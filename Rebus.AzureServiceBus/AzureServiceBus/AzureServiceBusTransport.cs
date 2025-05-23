﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rebus.AzureServiceBus.Messages;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Internals;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Transport;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleAnonymousFunction
#pragma warning disable 1998

namespace Rebus.AzureServiceBus;

/// <summary>
/// Implementation of <see cref="ITransport"/> that uses Azure Service Bus queues to send/receive messages.
/// </summary>
public class AzureServiceBusTransport : ITransport, IInitializable, IDisposable, ISubscriptionStorage
{
    /// <summary>
    /// Outgoing messages are stashed in a concurrent queue under this key
    /// </summary>
    const string OutgoingMessagesKey = "new-azure-service-bus-transport";

    /// <summary>
    /// Subscriber "addresses" are prefixed with this bad boy so we can recognize them and publish to a topic client instead
    /// </summary>
    const string MagicSubscriptionPrefix = "TOPIC:";

    /// <summary>
    /// External timeout manager address set to this magic address will be routed to the destination address specified by the <see cref="Headers.DeferredRecipient"/> header
    /// </summary>
    public const string MagicDeferredMessagesAddress = "___deferred___";

    const string TransportmessageItemKey = "transportMessage";

    static readonly ServiceBusRetryOptions DefaultRetryStrategy = new()
    {
        Mode = ServiceBusRetryMode.Exponential,
        Delay = TimeSpan.FromMilliseconds(100),
        MaxDelay = TimeSpan.FromSeconds(10),
        MaxRetries = 10
    };

    readonly ExceptionIgnorant _subscriptionExceptionIgnorant = new ExceptionIgnorant(maxAttemps: 10).Ignore<ServiceBusException>(ex => ex.IsTransient);
    readonly ConcurrentStack<IDisposable> _disposables = new();
    readonly ConcurrentDictionary<string, MessageLockRenewer> _messageLockRenewers = new();
    readonly ConcurrentDictionary<string, string[]> _cachedSubscriberAddresses = new();
    readonly ConcurrentDictionary<string, Lazy<ServiceBusSender>> _messageSenders = new();
    readonly ConcurrentDictionary<string, Lazy<Task<ServiceBusSender>>> _topicClients = new();
    readonly CancellationToken _cancellationToken;
    readonly IAsyncTask _messageLockRenewalTask;
    readonly ServiceBusAdministrationClient _managementClient;
    readonly ConnectionStringParser _connectionStringParser;
    readonly INameFormatter _nameFormatter;
    readonly IMessageConverter _messageConverter;
    readonly string _subscriptionName;
    readonly ILog _log;
    readonly ServiceBusClient _client;

    bool _prefetchingEnabled;
    int _prefetchCount;

    ServiceBusReceiver _messageReceiver;

    /// <summary>
    /// Constructs the transport, connecting to the service bus pointed to by the connection string.
    /// </summary>
    public AzureServiceBusTransport(string connectionString, string queueName, IRebusLoggerFactory rebusLoggerFactory,
        IAsyncTaskFactory asyncTaskFactory, INameFormatter nameFormatter, IMessageConverter messageConverter,
        CancellationToken cancellationToken = default, TokenCredential tokenCredential = null)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        if (asyncTaskFactory == null) throw new ArgumentNullException(nameof(asyncTaskFactory));
        if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

        _nameFormatter = nameFormatter;
        _messageConverter = messageConverter;

        if (queueName != null)
        {
            // this never happens
            if (queueName.StartsWith(MagicSubscriptionPrefix))
            {
                throw new ArgumentException($"Sorry, but the queue name '{queueName}' cannot be used because it conflicts with Rebus' internally used 'magic subscription prefix': '{MagicSubscriptionPrefix}'. ");
            }

            Address = _nameFormatter.FormatQueueName(queueName);
            _subscriptionName = _nameFormatter.FormatSubscriptionName(queueName);
        }

        _cancellationToken = cancellationToken;
        _log = rebusLoggerFactory.GetLogger<AzureServiceBusTransport>();

        _connectionStringParser = new ConnectionStringParser(connectionString);

        var clientOptions = new ServiceBusClientOptions
        {
            TransportType = _connectionStringParser.Transport,
            RetryOptions = DefaultRetryStrategy,
        };

        if (tokenCredential != null)
        {
            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);
            _managementClient = new ServiceBusAdministrationClient(connectionStringProperties.FullyQualifiedNamespace, tokenCredential);
            _client = new ServiceBusClient(connectionStringProperties.FullyQualifiedNamespace, tokenCredential, clientOptions);
        }
        else
        {
            var connectionStringWithoutEntityPath = _connectionStringParser.GetConnectionStringWithoutEntityPath();

            _client = new ServiceBusClient(connectionStringWithoutEntityPath, clientOptions);
            _managementClient = new ServiceBusAdministrationClient(connectionStringWithoutEntityPath);
        }

        _messageLockRenewalTask = asyncTaskFactory.Create("Peek Lock Renewal", RenewPeekLocks, prettyInsignificant: true, intervalSeconds: 10);
    }

    /// <summary>
    /// Gets "subscriber addresses" by getting one single magic "queue name", which is then
    /// interpreted as a publish operation to a topic when the time comes to send to that "queue"
    /// </summary>
    public async Task<IReadOnlyList<string>> GetSubscriberAddresses(string topic)
    {
        return _cachedSubscriberAddresses.GetOrAdd(topic, _ => new[] { $"{MagicSubscriptionPrefix}{topic}" });
    }

    /// <summary>
    /// Registers this endpoint as a subscriber by creating a subscription for the given topic, setting up
    /// auto-forwarding from that subscription to this endpoint's input queue
    /// </summary>
    public async Task RegisterSubscriber(string topic, string subscriberAddress)
    {
        if (DoNotConfigureTopicEnabled)
        {
            return;
        }
        
        VerifyIsOwnInputQueueAddress(subscriberAddress);

        topic = _nameFormatter.FormatTopicName(topic);

        _log.Debug("Registering subscription for topic {topicName}", topic);

        await _subscriptionExceptionIgnorant.Execute(async () =>
        {
            var topicProperties = await EnsureTopicExists(topic).ConfigureAwait(false);
            var messageSender = GetMessageSender(Address);

            var inputQueuePath = messageSender.GetQueuePath();
            var topicName = topicProperties.Name;

            var subscription = await GetOrCreateSubscription(topicName, _subscriptionName).ConfigureAwait(false);

            bool ForwardToMatches() => subscription.ForwardTo == inputQueuePath;

            // if it looks fine, just skip it
            if (ForwardToMatches()) return;

            subscription.ForwardTo = inputQueuePath;

            await _managementClient.UpdateSubscriptionAsync(subscription, _cancellationToken).ConfigureAwait(false);

            _log.Info("Subscription {subscriptionName} for topic {topicName} successfully registered", _subscriptionName, topic);
        }, _cancellationToken);
    }

    /// <summary>
    /// Unregisters this endpoint as a subscriber by deleting the subscription for the given topic
    /// </summary>
    public async Task UnregisterSubscriber(string topic, string subscriberAddress)
    {
        if (DoNotConfigureTopicEnabled)
        {
            return;
        }
        
        VerifyIsOwnInputQueueAddress(subscriberAddress);

        topic = _nameFormatter.FormatTopicName(topic);

        _log.Debug("Unregistering subscription for topic {topicName}", topic);

        await _subscriptionExceptionIgnorant.Execute(async () =>
        {
            var topicProperties = await EnsureTopicExists(topic).ConfigureAwait(false);
            var topicName = topicProperties.Name;

            try
            {
                await _managementClient.DeleteSubscriptionAsync(topicName, _subscriptionName, _cancellationToken).ConfigureAwait(false);

                _log.Info("Subscription {subscriptionName} for topic {topicName} successfully unregistered",
                    _subscriptionName, topic);
            }
            catch (ServiceBusException)
            {
                // it's alright man
            }
        }, _cancellationToken);
    }

    async Task<SubscriptionProperties> GetOrCreateSubscription(string topicPath, string subscriptionName)
    {
        if (await _managementClient.SubscriptionExistsAsync(topicPath, subscriptionName, _cancellationToken).ConfigureAwait(false))
        {
            return await _managementClient.GetSubscriptionAsync(topicPath, subscriptionName, _cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var options = new CreateSubscriptionOptions(topicPath, subscriptionName)
            {
                ForwardTo = GetMessageSender(Address).GetQueuePath()
            };

            return await _managementClient.CreateSubscriptionAsync(options, _cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException)
        {
            // most likely a race between two competing consumers - we should be able to get it now
            return await _managementClient.GetSubscriptionAsync(topicPath, subscriptionName, _cancellationToken).ConfigureAwait(false);
        }
    }

    void VerifyIsOwnInputQueueAddress(string subscriberAddress)
    {
        if (subscriberAddress == Address) return;

        var message = $"Cannot register subscriptions endpoint with input queue '{subscriberAddress}' in endpoint with input" +
                      $" queue '{Address}'! The Azure Service Bus transport functions as a centralized subscription" +
                      " storage, which means that all subscribers are capable of managing their own subscriptions";

        throw new ArgumentException(message);
    }

    async Task<TopicProperties> EnsureTopicExists(string normalizedTopic)
    {
        if (await _managementClient.TopicExistsAsync(normalizedTopic, _cancellationToken).ConfigureAwait(false))
        {
            return await _managementClient.GetTopicAsync(normalizedTopic, _cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await _managementClient.CreateTopicAsync(normalizedTopic, _cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException)
        {
            // most likely a race between two clients trying to create the same topic - we should be able to get it now
            return await _managementClient.GetTopicAsync(normalizedTopic, _cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new ArgumentException($"Could not create topic '{normalizedTopic}'", exception);
        }
    }

    /// <summary>
    /// Creates a queue with the given address
    /// </summary>
    public void CreateQueue(string address)
    {
        address = _nameFormatter.FormatQueueName(address);

        InnerCreateQueue(address);
    }

    void InnerCreateQueue(string normalizedAddress)
    {
        CreateQueueOptions GetInputQueueDescription()
        {
            var queueOptions = new CreateQueueOptions(normalizedAddress);

            // if it's the input queue, do this:
            if (normalizedAddress == Address)
            {
                // must be set when the queue is first created
                queueOptions.EnablePartitioning = PartitioningEnabled;

                if (LockDuration.HasValue)
                {
                    queueOptions.LockDuration = LockDuration.Value;
                }

                if (DefaultMessageTimeToLive.HasValue)
                {
                    queueOptions.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
                }

                if (DuplicateDetectionHistoryTimeWindow.HasValue)
                {
                    queueOptions.RequiresDuplicateDetection = true;
                    queueOptions.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;
                }

                if (AutoDeleteOnIdle.HasValue)
                {
                    queueOptions.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
                }

                queueOptions.MaxDeliveryCount = 100;
            }

            return queueOptions;
        }

        // one-way client does not create any queues
        if (Address == null)
        {
            return;
        }

        if (DoNotCreateQueuesEnabled)
        {
            _log.Info("Transport configured to not create queue - skipping existence check and potential creation for {queueName}", normalizedAddress);
            return;
        }

        AsyncHelpers.RunSync(async () =>
        {
            if (await _managementClient.QueueExistsAsync(normalizedAddress, _cancellationToken).ConfigureAwait(false)) return;

            try
            {
                _log.Info("Creating ASB queue {queueName}", normalizedAddress);

                var queueDescription = GetInputQueueDescription();

                await _managementClient.CreateQueueAsync(queueDescription, _cancellationToken).ConfigureAwait(false);
            }
            catch (ServiceBusException)
            {
                // it's alright man
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not create Azure Service Bus queue '{normalizedAddress}'", exception);
            }
        });
    }

    void CheckInputQueueConfiguration(string address)
    {
        if (DoNotCheckQueueConfigurationEnabled)
        {
            _log.Info("Transport configured to not check queue configuration - skipping existence check for {queueName}", address);
            return;
        }

        AsyncHelpers.RunSync(async () =>
        {
            var queueDescription = await GetQueueDescription(address).ConfigureAwait(false);

            if (queueDescription.EnablePartitioning != PartitioningEnabled)
            {
                _log.Warn("The queue {queueName} has EnablePartitioning={enablePartitioning}, but the transport has PartitioningEnabled={partitioningEnabled}. As this setting cannot be changed after the queue is created, please either make sure the Rebus transport settings are consistent with the queue settings, or delete the queue and let Rebus create it again with the new settings.",
                    address, queueDescription.EnablePartitioning, PartitioningEnabled);
            }

            if (DuplicateDetectionHistoryTimeWindow.HasValue)
            {
                var duplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;

                if (!queueDescription.RequiresDuplicateDetection ||
                    queueDescription.DuplicateDetectionHistoryTimeWindow != duplicateDetectionHistoryTimeWindow)
                {
                    _log.Warn("The queue {queueName} has RequiresDuplicateDetection={requiresDuplicateDetection}, but the transport has DuplicateDetectionHistoryTimeWindow={duplicateDetectionHistoryTimeWindow}. As this setting cannot be changed after the queue is created, please either make sure the Rebus transport settings are consistent with the queue settings, or delete the queue and let Rebus create it again with the new settings.",
                        address, queueDescription.RequiresDuplicateDetection, duplicateDetectionHistoryTimeWindow);
                }
            }
            else
            {
                if (queueDescription.RequiresDuplicateDetection)
                {
                    _log.Warn("The queue {queueName} has RequiresDuplicateDetection={requiresDuplicateDetection}, but the transport has DuplicateDetectionHistoryTimeWindow=null. As this setting cannot be changed after the queue is created, please either make sure the Rebus transport settings are consistent with the queue settings, or delete the queue and let Rebus create it again with the new settings.",
                        address, queueDescription.RequiresDuplicateDetection);
                }
            }

            var updates = new List<string>();

            if (DefaultMessageTimeToLive.HasValue)
            {
                var defaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
                if (queueDescription.DefaultMessageTimeToLive != defaultMessageTimeToLive)
                {
                    queueDescription.DefaultMessageTimeToLive = defaultMessageTimeToLive;
                    updates.Add($"DefaultMessageTimeToLive = {defaultMessageTimeToLive}");
                }
            }

            if (LockDuration.HasValue)
            {
                var lockDuration = LockDuration.Value;
                if (queueDescription.LockDuration != lockDuration)
                {
                    queueDescription.LockDuration = lockDuration;
                    updates.Add($"LockDuration = {lockDuration}");
                }
            }

            if (AutoDeleteOnIdle.HasValue)
            {
                var autoDeleteOnIdle = AutoDeleteOnIdle.Value;
                if (queueDescription.AutoDeleteOnIdle != autoDeleteOnIdle)
                {
                    queueDescription.AutoDeleteOnIdle = autoDeleteOnIdle;
                    updates.Add($"AutoDeleteOnIdle = {autoDeleteOnIdle}");
                }
            }

            if (!updates.Any()) return;

            if (DoNotCreateQueuesEnabled)
            {
                _log.Warn("Detected changes in the settings for the queue {queueName}: {updates} - but the transport is configured to NOT create queues, so no settings will be changed", address, updates);
                return;
            }

            _log.Info("Updating ASB queue {queueName}: {updates}", address, updates);
            await _managementClient.UpdateQueueAsync(queueDescription, _cancellationToken);
        });
    }

    async Task<QueueProperties> GetQueueDescription(string address)
    {
        try
        {
            return await _managementClient.GetQueueAsync(address, _cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, $"Could not get queue description for queue {address}");
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Sends the given message to the queue with the given <paramref name="destinationAddress" />
    /// </summary>
    public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
    {
        var actualDestinationAddress = GetActualDestinationAddress(destinationAddress, message);
        var outgoingMessages = GetOutgoingMessages(context);

        outgoingMessages.Enqueue(new OutgoingMessage(actualDestinationAddress, message));
    }

    string GetActualDestinationAddress(string destinationAddress, TransportMessage message)
    {
        if (message.Headers.ContainsKey(Headers.DeferredUntil))
        {
            if (destinationAddress == MagicDeferredMessagesAddress)
            {
                try
                {
                    return message.Headers.GetValue(Headers.DeferredRecipient);
                }
                catch (Exception exception)
                {
                    throw new ArgumentException($"The destination address was set to '{MagicDeferredMessagesAddress}', but no '{Headers.DeferredRecipient}' header could be found on the message", exception);
                }
            }

            if (message.Headers.TryGetValue(Headers.DeferredRecipient, out var deferredRecipient))
            {
                return _nameFormatter.FormatQueueName(deferredRecipient);
            }
        }

        if (!destinationAddress.StartsWith(MagicSubscriptionPrefix))
        {
            return _nameFormatter.FormatQueueName(destinationAddress);
        }

        return destinationAddress;
    }

    ConcurrentQueue<OutgoingMessage> GetOutgoingMessages(ITransactionContext context)
    {
        ConcurrentQueue<OutgoingMessage> CreateNewOutgoingMessagesQueue()
        {
            var messagesToSend = new ConcurrentQueue<OutgoingMessage>();

            async Task SendOutgoingMessages(ITransactionContext ctx)
            {
                var messagesByDestinationQueue = messagesToSend.GroupBy(m => m.DestinationAddress);

                async Task SendOutgoingMessagesToDestination(IGrouping<string, OutgoingMessage> group)
                {
                    var destinationQueue = group.Key;
                    var messages = group;

                    if (destinationQueue.StartsWith(MagicSubscriptionPrefix))
                    {
                        var topicName = _nameFormatter.FormatTopicName(destinationQueue.Substring(MagicSubscriptionPrefix.Length));
                        var topicClient = await GetTopicClient(topicName);
                        var serviceBusMessageBatches = await GetBatches(messages.Select(m => _messageConverter.ToServiceBus(m.TransportMessage)), topicClient);

                        using (serviceBusMessageBatches.AsDisposable(b => b.DisposeCollection()))
                        {
                            foreach (var batch in serviceBusMessageBatches)
                            {
                                try
                                {
                                    await topicClient
                                        .SendMessagesAsync(batch, CancellationToken.None) //< don't cancel when sending outgoing messages
                                        .ConfigureAwait(false);
                                }
                                catch (Exception exception)
                                {
                                    throw new RebusApplicationException(exception, $"Could not publish to topic '{topicName}'");
                                }
                            }
                        }
                    }
                    else
                    {
                        var messageSender = GetMessageSender(destinationQueue);
                        var serviceBusMessageBatches = await GetBatches(messages.Select(m => _messageConverter.ToServiceBus(m.TransportMessage)), messageSender);

                        using (serviceBusMessageBatches.AsDisposable(b => b.DisposeCollection()))
                        {
                            foreach (var batch in serviceBusMessageBatches)
                            {
                                try
                                {
                                    await messageSender
                                        .SendMessagesAsync(batch, CancellationToken.None) //< don't cancel when sending outgoing messages
                                        .ConfigureAwait(false);
                                }
                                catch (Exception exception)
                                {
                                    throw new RebusApplicationException(exception, $"Could not send to queue '{destinationQueue}'");
                                }
                            }
                        }
                    }
                }

                await Task.WhenAll(messagesByDestinationQueue.Select(SendOutgoingMessagesToDestination)).ConfigureAwait(false);
            }

            context.OnCommit(SendOutgoingMessages);

            return messagesToSend;
        }

        return context.GetOrAdd(OutgoingMessagesKey, CreateNewOutgoingMessagesQueue);
    }

    async Task<IReadOnlyList<ServiceBusMessageBatch>> GetBatches(IEnumerable<ServiceBusMessage> messages, ServiceBusSender sender)
    {
        async ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync()
        {
            try
            {
                return await sender.CreateMessageBatchAsync(CancellationToken.None); //< don't cancel when sending outgoing messages
            }
            catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                throw new RebusApplicationException(exception, $"Message batch creation failed, because the messaging entity with path '{sender.EntityPath}' does not exist");
            }
        }

        var batches = new List<ServiceBusMessageBatch>();
        var currentBatch = await CreateMessageBatchAsync();

        foreach (var message in messages)
        {
            if (currentBatch.TryAddMessage(message)) continue;

            batches.Add(currentBatch);
            currentBatch = await CreateMessageBatchAsync();

            if (currentBatch.TryAddMessage(message)) continue;

            throw new ArgumentException($"The message {message} could not be added to a brand new message batch (batch max size is {currentBatch.MaxSizeInBytes} bytes)");
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// Receives the next message from the input queue. Returns null if no message was available
    /// </summary>
    public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
    {
        var receivedMessage = await ReceiveInternal().ConfigureAwait(false);

        if (receivedMessage == null) return null;

        var message = receivedMessage.Message;
        var messageReceiver = receivedMessage.MessageReceiver;

        var items = context.Items;

        // add the message and its receiver to the context
        items["asb-message"] = message;
        items["asb-message-receiver"] = messageReceiver;

        if (string.IsNullOrWhiteSpace(message.LockToken))
        {
            throw new RebusApplicationException($"OMG that's weird - message with ID {message.MessageId} does not have a lock token!");
        }

        var messageId = message.MessageId;

        if (AutomaticallyRenewPeekLock && !_prefetchingEnabled)
        {
            _messageLockRenewers.TryAdd(message.MessageId, new MessageLockRenewer(message, messageReceiver));
        }

        context.OnAck(async ctx =>
        {
            _messageLockRenewers.TryRemove(messageId, out _);

            // only ACK the message if it's still in the context - this way, carefully crafted
            // user code can take over responsibility for the message by removing it from the transaction context
            if (ctx.Items.TryGetValue("asb-message", out var messageObject) && messageObject is ServiceBusReceivedMessage asbMessage)
            {
                var lockToken = asbMessage.LockToken;

                try
                {
                    await messageReceiver
                        .CompleteMessageAsync(
                            message: asbMessage,
                            cancellationToken: CancellationToken.None //< pass none here to avoid canceling the call immediately when we're shutting down
                        )
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception,
                        $"Could not complete message with ID {messageId} and lock token {lockToken}");
                }
            }
        });

        context.OnNack(async ctx =>
        {
            _messageLockRenewers.TryRemove(messageId, out _);

            // only NACK the message if it's still in the context - this way, carefully crafted
            // user code can take over responsibility for the message by removing it from the transaction context
            if (!ctx.Items.TryGetValue("asb-message", out var messageObject) || messageObject is not ServiceBusReceivedMessage asbMessage) return;

            var lockToken = asbMessage.LockToken;

            try
            {
                var transportMessage = (TransportMessage)ctx.Items[TransportmessageItemKey];
                var propertiesToModify = transportMessage.Headers.ToDictionary(k => k.Key, v => (object)v.Value);

                await messageReceiver.AbandonMessageAsync(
                        message: message,
                        propertiesToModify: propertiesToModify,
                        cancellationToken: CancellationToken
                            .None //< pass none here to avoid canceling the call immediately when we're shutting down
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception,
                    $"Could not abandon message with ID {messageId} and lock token {lockToken}");
            }
        });

        context.OnDisposed(ctx => _messageLockRenewers.TryRemove(messageId, out _));

        var transportMessage = _messageConverter.ToTransport(message);
        context.Items[TransportmessageItemKey] = transportMessage;

        if (NativeMessageDeliveryCountEnabled)
        {
            transportMessage.Headers[Headers.DeliveryCount] = message.DeliveryCount.ToString();
        }
        
        return transportMessage;
    }

    async Task<ReceivedMessage> ReceiveInternal()
    {
        try
        {
            var message = await _messageReceiver.ReceiveMessageAsync(ReceiveOperationTimeout, _cancellationToken);

            return message == null ? null : new ReceivedMessage(message, _messageReceiver);
        }
        catch (ServiceBusException exception)
        {
            throw new RebusApplicationException(exception, $"Could not receive next message from Azure Service Bus queue '{Address}'");
        }
    }

    /// <summary>
    /// Gets the input queue name for this transport
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Initializes the transport by ensuring that the input queue has been created
    /// </summary>
    /// <inheritdoc />
    public void Initialize()
    {
        _disposables.Push(_messageLockRenewalTask);

        if (Address == null)
        {
            _log.Info("Initializing one-way Azure Service Bus transport");
            return;
        }

        _log.Info("Initializing Azure Service Bus transport with queue {queueName}", Address);

        InnerCreateQueue(Address);

        CheckInputQueueConfiguration(Address);

        var receiverOptions = new ServiceBusReceiverOptions
        {
            PrefetchCount = _prefetchCount,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        };

        _messageReceiver = _client.CreateReceiver(Address, receiverOptions);

        _disposables.Push(_messageReceiver.AsDisposable(m => AsyncHelpers.RunSync(async () =>
        {
            try
            {
                await m.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // don't care
            }
        })));

        if (AutomaticallyRenewPeekLock)
        {
            _messageLockRenewalTask.Start();
        }
    }

    /// <summary>
    /// Always returns true because Azure Service Bus topics and subscriptions are global
    /// </summary>
    public bool IsCentralized => true;

    /// <summary>
    /// Enables automatic peek lock renewal - only recommended if you truly need to handle messages for a very long time
    /// </summary>
    public bool AutomaticallyRenewPeekLock { get; set; }

    /// <summary>
    /// Gets/sets whether partitioning should be enabled on new queues. Only takes effect for queues created
    /// after the property has been enabled
    /// </summary>
    public bool PartitioningEnabled { get; set; }

    /// <summary>
    /// Gets/sets whether to skip creating queues
    /// </summary>
    public bool DoNotCreateQueuesEnabled { get; set; }

    /// <summary>
    /// Gets/sets whether to skip checking queues configuration
    /// </summary>
    public bool DoNotCheckQueueConfigurationEnabled { get; set; }
    
    /// <summary>
    /// Gets/sets whether to skip checking topics configuration
    /// </summary>
    public bool DoNotConfigureTopicEnabled { get; set; }

    /// <summary>
    /// Gets/sets the default message TTL. Must be set before calling <see cref="Initialize"/>, because that is the time when the queue is (re)configured
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// Gets/sets message peek lock duration
    /// </summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>
    /// Gets/sets auto-delete-on-idle duration
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>
    /// Gets/sets the duplicate detection window
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }

    /// <summary>
    /// Gets/sets the receive timeout.
    /// </summary>
    public TimeSpan ReceiveOperationTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets/sets whether native message delivery count is used
    /// </summary>
    public bool NativeMessageDeliveryCountEnabled { get; set; }

    /// <summary>
    /// Purges the input queue by receiving all messages as quickly as possible
    /// </summary>
    public void PurgeInputQueue()
    {
        var queueName = Address;

        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new InvalidOperationException("Cannot 'purge input queue' because there's no input queue name – it's most likely because this is a one-way client, and hence there is no input queue");
        }

        PurgeQueue(queueName);
    }

    /// <summary>
    /// Configures the transport to prefetch the specified number of messages into an in-mem queue for processing, disabling automatic peek lock renewal
    /// </summary>
    public void PrefetchMessages(int prefetchCount)
    {
        if (prefetchCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(prefetchCount), prefetchCount, "Must prefetch zero or more messages");
        }

        _prefetchingEnabled = prefetchCount > 0;
        _prefetchCount = prefetchCount;
    }

    /// <summary>
    /// Disposes all resources associated with this particular transport instance
    /// </summary>
    public void Dispose()
    {
        var disposables = new List<IDisposable>();

        while (_disposables.TryPop(out var disposable))
        {
            disposables.Add(disposable);
        }

        Parallel.ForEach(disposables, d => d.Dispose());
    }

    void PurgeQueue(string queueName)
    {
        try
        {
            AsyncHelpers.RunSync(() => ManagementExtensions.PurgeQueue(_connectionStringParser.GetConnectionString(), queueName, _cancellationToken));
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
            // we're on our way out - let cancellation bubble out
            throw;
        }
        catch (Exception exception)
        {
            throw new ArgumentException($"Could not purge queue '{queueName}'", exception);
        }
    }

    ServiceBusSender GetMessageSender(string queue) => _messageSenders.GetOrAdd(queue, _ => new(() =>
    {
        var messageSender = _client.CreateSender(queue);

        _disposables.Push(messageSender.AsDisposable(t => AsyncHelpers.RunSync(async () =>
        {
            try
            {
                await t.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // don't care
            }
        })));

        return messageSender;

    })).Value;

    async Task<ServiceBusSender> GetTopicClient(string topic) => await _topicClients.GetOrAdd(topic, _ => new(async () =>
    {
        await EnsureTopicExists(topic);

        var topicClient = _client.CreateSender(topic);

        _disposables.Push(topicClient.AsDisposable(t => AsyncHelpers.RunSync(async () =>
        {
            try
            {
                await t.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // don't care
            }
        })));

        return topicClient;
    })).Value;

    async Task RenewPeekLocks()
    {
        var mustBeRenewed = _messageLockRenewers
            .Where(r => r.Value.IsDue)
            .Select(kvp => kvp.Value)
            .ToList();

        if (!mustBeRenewed.Any()) return;

        _log.Debug("Found {count} peek locks to be renewed", mustBeRenewed.Count);

        await Task.WhenAll(mustBeRenewed.Select(async r =>
        {
            try
            {
                await r.Renew().ConfigureAwait(false);

                _log.Debug("Successfully renewed peek lock for message with ID {messageId}", r.MessageId);
            }
            catch (Exception exception)
            {
                // if an exception occurs, check if the peek lock renewer is still in the dictionary of active
                // peek lock renewers - if it isn't, then the message must have been completed/abandoned in the meantime,
                // and then it's not an error that the peek lock could not be renewed
                if (!_messageLockRenewers.ContainsKey(r.MessageId)) return;

                _log.Warn(exception, "Error when renewing peek lock for message with ID {messageId}", r.MessageId);

                // peek lock renewal will be automatically retried, because it's still due for renewal
            }
        }));
    }
}

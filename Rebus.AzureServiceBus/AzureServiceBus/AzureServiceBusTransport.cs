using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Internals;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Subscriptions;
using Rebus.Transport;
using Message = Microsoft.Azure.ServiceBus.Message;
// ReSharper disable RedundantArgumentDefaultValue
#pragma warning disable 1998

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Azure Service Bus queues to send/receive messages.
    /// </summary>
    public class AzureServiceBusTransport : ITransport, IInitializable, IDisposable, ISubscriptionStorage
    {
        readonly string _connectionString;

        /// <summary>
        /// Outgoing messages are stashed in a concurrent queue under this key
        /// </summary>
        const string OutgoingMessagesKey = "new-azure-service-bus-transport";

        /// <summary>
        /// Subscriber "addresses" are prefixed with this bad boy so we can recognize it and publish to a topic client instead
        /// </summary>
        const string MagicSubscriptionPrefix = "subscription/";

        /// <summary>
        /// Defines the maxiumum number of outgoing messages to batch together when sending/publishing
        /// </summary>
        const int DefaultOutgoingBatchSize = 50;

        static readonly RetryExponential DefaultRetryStrategy = new RetryExponential(
            minimumBackoff: TimeSpan.FromMilliseconds(100),
            maximumBackoff: TimeSpan.FromSeconds(10),
            maximumRetryCount: 10
        );

        readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();
        readonly ConcurrentDictionary<string, MessageSender> _messageSenders = new ConcurrentDictionary<string, MessageSender>();
        readonly ConcurrentDictionary<string, TopicClient> _topicClients = new ConcurrentDictionary<string, TopicClient>();
        readonly Func<string, MessageSender> _getMessageSender;
        readonly Func<string, TopicClient> _getTopicClient;
        readonly ManagementClient _managementClient;
        readonly Action _purgeInputQueue;
        readonly ILog _log;

        MessageReceiver _messageReceiver;

        /// <summary>
        /// Constructs the transport, connecting to the service bus pointed to by the connection string.
        /// </summary>
        public AzureServiceBusTransport(string connectionString, string queueName, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            Address = queueName?.ToLowerInvariant();

            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _log = rebusLoggerFactory.GetLogger<AzureServiceBusTransport>();
            _managementClient = new ManagementClient(connectionString);

            _getTopicClient = topic => _topicClients.GetOrAdd(topic, _ =>
            {
                var topicClient = new TopicClient(
                    connectionString,
                    topic,
                    retryPolicy: DefaultRetryStrategy
                );
                _disposables.Push(topicClient.AsDisposable(t => AsyncHelpers.RunSync(t.CloseAsync)));
                return topicClient;
            });

            _getMessageSender = queue => _messageSenders.GetOrAdd(queue, _ =>
            {
                var messageSender = new MessageSender(
                    connectionString,
                    queue,
                    retryPolicy: DefaultRetryStrategy
                );
                _disposables.Push(messageSender.AsDisposable(t => AsyncHelpers.RunSync(t.CloseAsync)));
                return messageSender;
            });

            _purgeInputQueue = () =>
            {
                try
                {
                    AsyncHelpers.RunSync(() => ManagementExtensions.PurgeQueue(connectionString, queueName));
                }
                catch (Exception exception)
                {
                    throw new ArgumentException($"Could not purge queue '{queueName}'", exception);
                }
            };
        }

        //static readonly TimeSpan[] RetryWaitTimes =
        //{
        //    TimeSpan.FromSeconds(0.1),
        //    TimeSpan.FromSeconds(0.1),
        //    TimeSpan.FromSeconds(0.1),
        //    TimeSpan.FromSeconds(0.2),
        //    TimeSpan.FromSeconds(0.2),
        //    TimeSpan.FromSeconds(0.2),
        //    TimeSpan.FromSeconds(0.5),
        //    TimeSpan.FromSeconds(1),
        //    TimeSpan.FromSeconds(1),
        //    TimeSpan.FromSeconds(1),
        //    TimeSpan.FromSeconds(5),
        //    TimeSpan.FromSeconds(5),
        //    TimeSpan.FromSeconds(10),
        //};

        //readonly ConcurrentDictionary<string, TopicDescription> _topics = new ConcurrentDictionary<string, TopicDescription>(StringComparer.InvariantCultureIgnoreCase);
        //readonly ConcurrentDictionary<string, TopicClient> _topicClients = new ConcurrentDictionary<string, TopicClient>(StringComparer.InvariantCultureIgnoreCase);
        //readonly ConcurrentDictionary<string, QueueClient> _queueClients = new ConcurrentDictionary<string, QueueClient>(StringComparer.InvariantCultureIgnoreCase);
        //readonly NamespaceManager _namespaceManager;
        //readonly string _connectionString;
        //readonly IAsyncTaskFactory _asyncTaskFactory;
        //readonly string _inputQueueAddress;
        //readonly ILog _log;

        //readonly TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        //readonly AsyncBottleneck _bottleneck = new AsyncBottleneck(10);
        //readonly Ignorant _ignorant = new Ignorant();

        //readonly ConcurrentQueue<BrokeredMessage> _prefetchQueue = new ConcurrentQueue<BrokeredMessage>();
        //readonly TimeSpan? _receiveTimeout;

        //bool _prefetchingEnabled;
        //int _numberOfMessagesToPrefetch;
        //bool _disposed;

        ///// <summary>
        ///// Constructs the transport, connecting to the service bus pointed to by the connection string.
        ///// </summary>
        //public AzureServiceBusTransport(string connectionString, string inputQueueAddress, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory)
        //{
        //    if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
        //    if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        //    if (asyncTaskFactory == null) throw new ArgumentNullException(nameof(asyncTaskFactory));

        //    _log = rebusLoggerFactory.GetLogger<AzureServiceBusTransport>();

        //    _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
        //    _connectionString = connectionString;
        //    _asyncTaskFactory = asyncTaskFactory;

        //    if (inputQueueAddress != null)
        //    {
        //        _inputQueueAddress = inputQueueAddress.ToLowerInvariant();
        //    }

        //    // if a timeout has been specified, we respect that - otherwise, we pick a sensible default:
        //    _receiveTimeout = _connectionString.Contains("OperationTimeout")
        //        ? default(TimeSpan?)
        //        : TimeSpan.FromSeconds(5);
        //}

        ///// <summary>
        ///// Initializes the transport by ensuring that the input queue has been created
        ///// </summary>
        //public void Initialize()
        //{
        //    if (_inputQueueAddress != null)
        //    {
        //        _log.Info("Initializing Azure Service Bus transport with queue '{0}'", _inputQueueAddress);
        //        CreateQueue(_inputQueueAddress);
        //        return;
        //    }

        //    _log.Info("Initializing one-way Azure Service Bus transport");
        //}

        ///// <summary>
        ///// Purges the input queue by deleting it and creating it again
        ///// </summary>
        //public void PurgeInputQueue()
        //{
        //    _log.Info("Purging queue '{0}'", _inputQueueAddress);
        //    _namespaceManager.DeleteQueue(_inputQueueAddress);

        //    CreateQueue(_inputQueueAddress);
        //}

        ///// <summary>
        ///// Configures the transport to prefetch the specified number of messages into an in-mem queue for processing, disabling automatic peek lock renewal
        ///// </summary>
        //public void PrefetchMessages(int numberOfMessagesToPrefetch)
        //{
        //    if (numberOfMessagesToPrefetch < 0)
        //    {
        //        throw new ArgumentOutOfRangeException(nameof(numberOfMessagesToPrefetch), numberOfMessagesToPrefetch, "Must prefetch zero or more messages");
        //    }

        //    _prefetchingEnabled = numberOfMessagesToPrefetch > 0;
        //    _numberOfMessagesToPrefetch = numberOfMessagesToPrefetch;
        //}

        ///// <summary>
        ///// Enables automatic peek lock renewal - only recommended if you truly need to handle messages for a very long time
        ///// </summary>
        //public bool AutomaticallyRenewPeekLock { get; set; }

        ///// <summary>
        ///// Creates a queue with the given address
        ///// </summary>
        //public void CreateQueue(string address)
        //{
        //    if (DoNotCreateQueuesEnabled)
        //    {
        //        _log.Info("Transport configured to not create queue - skipping existencecheck and potential creation");
        //        return;
        //    }

        //    if (_namespaceManager.QueueExists(address)) return;

        //    var now = DateTime.Now;
        //    var queueDescription = new QueueDescription(address)
        //    {
        //        MaxSizeInMegabytes = 1024,
        //        MaxDeliveryCount = 100,
        //        LockDuration = _peekLockDuration,
        //        EnablePartitioning = PartitioningEnabled,
        //        UserMetadata = $"Created by Rebus {now:yyyy-MM-dd} - {now:HH:mm:ss}",
        //    };

        //    try
        //    {
        //        _log.Info("Queue '{0}' does not exist - will create it now", address);
        //        _namespaceManager.CreateQueue(queueDescription);
        //        _log.Info("Created!");
        //    }
        //    catch (MessagingEntityAlreadyExistsException)
        //    {
        //        // fair enough...
        //        _log.Info("MessagingEntityAlreadyExistsException - carrying on");
        //    }
        //}

        ///// <summary>
        ///// Gets/sets whether partitioning should be enabled on new queues. Only takes effect for queues created
        ///// after the property has been enabled
        ///// </summary>
        //public bool PartitioningEnabled { get; set; }

        ///// <summary>
        ///// Sends the given message to the queue with the given <paramref name="destinationAddress"/>
        ///// </summary>
        //public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        //{
        //    GetOutgoingMessages(context)
        //        .GetOrAdd(destinationAddress, _ => new ConcurrentQueue<TransportMessage>())
        //        .Enqueue(message);
        //}


        ///// <summary>
        ///// Should return a new <see cref="Retrier"/>, fully configured to correctly "accept" the right exceptions
        ///// </summary>
        //static Retrier GetRetrier()
        //{
        //    return new Retrier(RetryWaitTimes)
        //        .On<MessagingException>(e => e.IsTransient)
        //        .On<MessagingCommunicationException>(e => e.IsTransient)
        //        .On<ServerBusyException>(e => e.IsTransient);
        //}

        ///// <summary>
        ///// Receives the next message from the input queue. Returns null if no message was available
        ///// </summary>
        //public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        //{
        //    if (_inputQueueAddress == null)
        //    {
        //        throw new InvalidOperationException("This Azure Service Bus transport does not have an input queue, hence it is not possible to reveive anything");
        //    }

        //    using (await _bottleneck.Enter(cancellationToken).ConfigureAwait(false))
        //    {
        //        var brokeredMessage = await ReceiveBrokeredMessage().ConfigureAwait(false);

        //        if (brokeredMessage == null) return null;

        //        var headers = brokeredMessage.Properties
        //            .Where(kvp => kvp.Value is string)
        //            .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value);

        //        var messageId = headers.GetValueOrNull(Headers.MessageId);
        //        var now = DateTime.UtcNow;
        //        var leaseDuration = brokeredMessage.LockedUntilUtc - now;
        //        var lockRenewalInterval = TimeSpan.FromMinutes(0.5 * leaseDuration.TotalMinutes);

        //        var renewalTask = GetRenewalTaskOrFakeDisposable(messageId, brokeredMessage, lockRenewalInterval);

        //        context.OnAborted(() =>
        //        {
        //            renewalTask.Dispose();

        //            try
        //            {
        //                brokeredMessage.Abandon();
        //            }
        //            catch (Exception exception)
        //            {
        //                // if it fails, it'll be back on the queue anyway....
        //                _log.Warn("Could not abandon message: {0}", exception);
        //            }
        //        });

        //        context.OnCommitted(async () =>
        //        {
        //            renewalTask.Dispose();
        //        });

        //        context.OnCompleted(async () =>
        //        {
        //            try
        //            {
        //                await brokeredMessage.CompleteAsync().ConfigureAwait(false);
        //            }
        //            catch (MessageLockLostException exception)
        //            {
        //                var elapsed = DateTime.UtcNow - now;

        //                throw new RebusApplicationException(exception, $"The message lock for message with ID {messageId} was lost - tried to complete after {elapsed.TotalSeconds:0.0} s");
        //            }
        //        });

        //        context.OnDisposed(() =>
        //        {
        //            renewalTask.Dispose();

        //            brokeredMessage.Dispose();
        //        });

        //        using (var memoryStream = new MemoryStream())
        //        {
        //            await brokeredMessage.GetBody<Stream>().CopyToAsync(memoryStream).ConfigureAwait(false);
        //            return new TransportMessage(headers, memoryStream.ToArray());
        //        }
        //    }
        //}

        //ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>> GetOutgoingMessages(ITransactionContext context)
        //{
        //    return context.GetOrAdd(OutgoingMessagesKey, () =>
        //    {
        //        var destinations = new ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>>();

        //        context.OnCommitted(async () =>
        //        {
        //            // send outgoing messages
        //            foreach (var destinationAndMessages in destinations)
        //            {
        //                var destinationAddress = destinationAndMessages.Key;
        //                var messages = destinationAndMessages.Value;

        //                var sendTasks = messages
        //                    .Select(async message =>
        //                    {
        //                        await GetRetrier().Execute(async () =>
        //                        {
        //                            using (var brokeredMessageToSend = MsgHelpers.CreateBrokeredMessage(message))
        //                            {
        //                                try
        //                                {
        //                                    await Send(destinationAddress, brokeredMessageToSend).ConfigureAwait(false);
        //                                }
        //                                catch (MessagingEntityNotFoundException exception)
        //                                {
        //                                    // do NOT rethrow as MessagingEntityNotFoundException because it has its own ToString that swallows most of the info!!
        //                                    throw new MessagingException($"Could not send to '{destinationAddress}'!", false, exception);
        //                                }
        //                            }
        //                        }).ConfigureAwait(false);
        //                    })
        //                    .ToArray();

        //                await Task.WhenAll(sendTasks).ConfigureAwait(false);
        //            }
        //        });

        //        return destinations;
        //    });
        //}

        //async Task Send(string destinationAddress, BrokeredMessage brokeredMessageToSend)
        //{
        //    if (destinationAddress.StartsWith(MagicSubscriptionPrefix))
        //    {
        //        var topic = destinationAddress.Substring(MagicSubscriptionPrefix.Length);

        //        await GetTopicClient(topic).SendAsync(brokeredMessageToSend).ConfigureAwait(false);
        //    }
        //    else
        //    {
        //        await GetQueueClient(destinationAddress).SendAsync(brokeredMessageToSend).ConfigureAwait(false);
        //    }
        //}

        //TopicClient GetTopicClient(string topic)
        //{
        //    return _topicClients.GetOrAdd(topic, t =>
        //    {
        //        _log.Debug("Initializing new topic client for {0}", topic);

        //        var topicDescription = EnsureTopicExists(topic);

        //        var fromConnectionString = TopicClient.CreateFromConnectionString(_connectionString, topicDescription.Path);

        //        return fromConnectionString;
        //    });
        //}


        //IDisposable GetRenewalTaskOrFakeDisposable(string messageId, BrokeredMessage brokeredMessage, TimeSpan lockRenewalInterval)
        //{
        //    if (!AutomaticallyRenewPeekLock)
        //    {
        //        return new FakeDisposable();
        //    }

        //    if (_prefetchingEnabled)
        //    {
        //        return new FakeDisposable();
        //    }

        //    var renewalTask = _asyncTaskFactory
        //        .Create($"RenewPeekLock-{messageId}",
        //            async () =>
        //            {
        //                await RenewPeekLock(messageId, brokeredMessage).ConfigureAwait(false);
        //            },
        //            intervalSeconds: (int)lockRenewalInterval.TotalSeconds,
        //            prettyInsignificant: true);

        //    renewalTask.Start();

        //    return renewalTask;
        //}

        //async Task RenewPeekLock(string messageId, BrokeredMessage brokeredMessage)
        //{
        //    _log.Info("Renewing peek lock for message with ID {0}", messageId);

        //    try
        //    {
        //        await brokeredMessage.RenewLockAsync().ConfigureAwait(false);
        //    }
        //    catch (MessageLockLostException)
        //    {
        //        // if we get this, it is because the message has been handled
        //    }
        //}

        //class FakeDisposable : IDisposable
        //{
        //    public void Dispose()
        //    {
        //    }
        //}

        //async Task<BrokeredMessage> ReceiveBrokeredMessage()
        //{
        //    var queueAddress = _inputQueueAddress;

        //    if (_prefetchingEnabled)
        //    {
        //        BrokeredMessage nextMessage;

        //        if (_prefetchQueue.TryDequeue(out nextMessage))
        //        {
        //            return nextMessage;
        //        }

        //        var client = GetQueueClient(queueAddress);

        //        // Timeout can be specified in ASB ConnectionString Endpoint=sb:://...;OperationTimeout=00:00:10
        //        var brokeredMessages = _receiveTimeout.HasValue
        //            ? (await client.ReceiveBatchAsync(_numberOfMessagesToPrefetch, _receiveTimeout.Value).ConfigureAwait(false)).ToList()
        //            : (await client.ReceiveBatchAsync(_numberOfMessagesToPrefetch).ConfigureAwait(false)).ToList();

        //        _ignorant.Reset();

        //        if (!brokeredMessages.Any()) return null;

        //        foreach (var receivedMessage in brokeredMessages)
        //        {
        //            _prefetchQueue.Enqueue(receivedMessage);
        //        }

        //        _prefetchQueue.TryDequeue(out nextMessage);

        //        return nextMessage; //< just accept null at this point if there was nothing
        //    }

        //    try
        //    {
        //        // Timeout can be specified in ASB ConnectionString Endpoint=sb:://...;OperationTimeout=00:00:10
        //        var brokeredMessage = _receiveTimeout.HasValue
        //            ? await GetQueueClient(queueAddress).ReceiveAsync(_receiveTimeout.Value).ConfigureAwait(false)
        //            : await GetQueueClient(queueAddress).ReceiveAsync().ConfigureAwait(false);

        //        _ignorant.Reset();

        //        return brokeredMessage;
        //    }
        //    catch (Exception exception)
        //    {
        //        if (_ignorant.IsToBeIgnored(exception)) return null;

        //        QueueClient possiblyFaultyQueueClient;

        //        if (_queueClients.TryRemove(queueAddress, out possiblyFaultyQueueClient))
        //        {
        //            CloseQueueClient(possiblyFaultyQueueClient);
        //        }

        //        throw;
        //    }
        //}

        //static void CloseQueueClient(QueueClient queueClientToClose)
        //{
        //    try
        //    {
        //        queueClientToClose.Close();
        //    }
        //    catch (Exception)
        //    {
        //        // ignored because we don't care!
        //    }
        //}

        //QueueClient GetQueueClient(string queueAddress)
        //{
        //    var queueClient = _queueClients.GetOrAdd(queueAddress, address =>
        //    {
        //        _log.Debug("Initializing new queue client for {0}", address);

        //        var newQueueClient = QueueClient.CreateFromConnectionString(_connectionString, address, ReceiveMode.PeekLock);

        //        return newQueueClient;
        //    });

        //    return queueClient;
        //}

        ///// <summary>
        ///// Gets the address of the input queue for the transport
        ///// </summary>
        //public string Address => _inputQueueAddress;

        ///// <summary>
        ///// Releases prefetched messages and cached queue clients
        ///// </summary>
        //public void Dispose()
        //{
        //    if (_disposed) return;

        //    try
        //    {
        //        DisposePrefetchedMessages();

        //        _queueClients.Values.ForEach(CloseQueueClient);
        //    }
        //    finally
        //    {
        //        _disposed = true;
        //    }
        //}

        //void DisposePrefetchedMessages()
        //{
        //    BrokeredMessage brokeredMessage;
        //    while (_prefetchQueue.TryDequeue(out brokeredMessage))
        //    {
        //        using (brokeredMessage)
        //        {
        //            try
        //            {
        //                brokeredMessage.Abandon();
        //            }
        //            catch (Exception exception)
        //            {
        //                _log.Warn("Could not abandon brokered message with ID {0}: {1}", brokeredMessage.MessageId, exception);
        //            }
        //        }
        //    }
        //}

        readonly ConcurrentDictionary<string, string[]> _cachedSubscriberAddresses = new ConcurrentDictionary<string, string[]>();

        /// <summary>
        /// Gets "subscriber addresses" by getting one single magic "queue name", which is then
        /// interpreted as a publish operation to a topic when the time comes to send to that "queue"
        /// </summary>
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            return _cachedSubscriberAddresses.GetOrAdd(topic, _ =>
            {
                var normalizedTopic = topic.ToValidAzureServiceBusEntityName();

                return new[] {$"{MagicSubscriptionPrefix}{normalizedTopic}"};
            });
        }

        /// <summary>
        /// Registers this endpoint as a subscriber by creating a subscription for the given topic, setting up
        /// auto-forwarding from that subscription to this endpoint's input queue
        /// </summary>
        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            VerifyIsOwnInputQueueAddress(subscriberAddress);

            var normalizedTopic = topic.ToValidAzureServiceBusEntityName();
            var topicDescription = await EnsureTopicExists(normalizedTopic);
            var messageSender = _getMessageSender(Address);

            var inputQueuePath = messageSender.Path;
            var topicPath = topicDescription.Path;
            var subscriptionName = GetSubscriptionName();

            var subscription = await GetOrCreateSubscription(topicPath, subscriptionName).ConfigureAwait(false);

            subscription.ForwardTo = inputQueuePath;

            await _managementClient.UpdateSubscriptionAsync(subscription);
        }

        /// <summary>
        /// Unregisters this endpoint as a subscriber by deleting the subscription for the given topic
        /// </summary>
        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            VerifyIsOwnInputQueueAddress(subscriberAddress);

            var normalizedTopic = topic.ToValidAzureServiceBusEntityName();
            var topicDescription = await EnsureTopicExists(normalizedTopic);
            var topicPath = topicDescription.Path;
            var subscriptionName = GetSubscriptionName();

            try
            {
                await _managementClient.DeleteSubscriptionAsync(topicPath, subscriptionName).ConfigureAwait(false);
            }
            catch (MessagingEntityNotFoundException)
            {
                // it's alright man
            }
        }

        async Task<SubscriptionDescription> GetOrCreateSubscription(string topicPath, string subscriptionName)
        {
            try
            {
                return await _managementClient.CreateSubscriptionAsync(topicPath, subscriptionName);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                return await _managementClient.GetSubscriptionAsync(topicPath, subscriptionName);
            }
        }

        string GetSubscriptionName()
        {
            var idx = Address.LastIndexOf("/", StringComparison.Ordinal) + 1;

            return Address.Substring(idx).ToValidAzureServiceBusEntityName();
        }

        void VerifyIsOwnInputQueueAddress(string subscriberAddress)
        {
            if (subscriberAddress == Address) return;

            var message = $"Cannot register subscriptions endpoint with input queue '{subscriberAddress}' in endpoint with input" +
                          $" queue '{Address}'! The Azure Service Bus transport functions as a centralized subscription" +
                          " storage, which means that all subscribers are capable of managing their own subscriptions";

            throw new ArgumentException(message);
        }

        async Task<TopicDescription> EnsureTopicExists(string normalizedTopic)
        {
            try
            {
                return await _managementClient.CreateTopicAsync(normalizedTopic);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                return await _managementClient.GetTopicAsync(normalizedTopic);
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
            if (DoNotCreateQueuesEnabled)
            {
                _log.Info("Transport configured to not create queue - skipping existence check and potential creation for {queueName}", address);
                return;
            }

            if (AsyncHelpers.ReturnSync(() => _managementClient.QueueExistsAsync(address))) return;

            try
            {
                _log.Info("Creating ASB queue {queueName}", address);

                AsyncHelpers.RunSync(() => _managementClient.CreateQueueIfNotExistsAsync(address));
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not create ASB queue '{address}'", exception);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Sends the given message to the queue with the given <paramref name="destinationAddress" />
        /// </summary>
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var outgoingMessages = GetOutgoingMessages(context);

            outgoingMessages.Enqueue(new OutgoingMessage(destinationAddress, message));
        }

        static Message GetMessage(OutgoingMessage outgoingMessage)
        {
            var transportMessage = outgoingMessage.TransportMessage;
            var message = new Message(transportMessage.Body);

            foreach (var kvp in transportMessage.Headers)
            {
                message.UserProperties[kvp.Key] = kvp.Value;
            }

            return message;
        }

        ConcurrentQueue<OutgoingMessage> GetOutgoingMessages(ITransactionContext context)
        {
            return context.GetOrAdd(OutgoingMessagesKey, () =>
            {
                var messagesToSend = new ConcurrentQueue<OutgoingMessage>();

                context.OnCommitted(async () =>
                {
                    var messagesByDestinationQueue = messagesToSend.GroupBy(m => m.DestinationAddress);

                    await Task.WhenAll(messagesByDestinationQueue.Select(async group =>
                    {
                        var destinationQueue = group.Key;
                        var messages = group;

                        if (destinationQueue.StartsWith(MagicSubscriptionPrefix))
                        {
                            var topicName = destinationQueue.Substring(MagicSubscriptionPrefix.Length);

                            foreach (var batch in messages.Batch(DefaultOutgoingBatchSize))
                            {
                                var list = batch.Select(GetMessage).ToList();

                                await _getTopicClient(topicName).SendAsync(list);
                            }
                        }
                        else
                        {
                            foreach (var batch in messages.Batch(DefaultOutgoingBatchSize))
                            {
                                var list = batch.Select(GetMessage).ToList();

                                await _getMessageSender(destinationQueue).SendAsync(list);
                            }
                        }

                    }));
                });

                return messagesToSend;
            });
        }

        class OutgoingMessage
        {
            public string DestinationAddress { get; }
            public TransportMessage TransportMessage { get; }

            public OutgoingMessage(string destinationAddress, TransportMessage transportMessage)
            {
                DestinationAddress = destinationAddress;
                TransportMessage = transportMessage;
            }
        }

        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            var message = await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(2));

            if (message == null) return null;

            var lockToken = message.SystemProperties.LockToken;

            context.OnCompleted(async () =>
            {
                try
                {
                    await _messageReceiver.CompleteAsync(lockToken);
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception,
                        $"Could not complete message with ID {message.MessageId} and lock token {lockToken}");
                }
            });

            context.OnAborted(() =>
            {
                try
                {
                    AsyncHelpers.RunSync(() => _messageReceiver.AbandonAsync(lockToken));
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception,
                        $"Could not abandon message with ID {message.MessageId} and lock token {lockToken}");
                }
            });

            var userProperties = message.UserProperties;
            var headers = userProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            var body = message.Body;

            return new TransportMessage(headers, body);
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
            if (Address != null)
            {
                _log.Info("Initializing Azure Service Bus transport with queue {queueName}", Address);
                CreateQueue(Address);

                _messageReceiver = new MessageReceiver(
                    _connectionString,
                    Address,
                    receiveMode: ReceiveMode.PeekLock,
                    retryPolicy: DefaultRetryStrategy
                );

                _disposables.Push(_messageReceiver.AsDisposable(m => AsyncHelpers.RunSync(m.CloseAsync)));

                return;
            }

            _log.Info("Initializing one-way Azure Service Bus transport");
        }

        /// <summary>
        /// Always returns true because Azure Service Bus topics and subscriptions are global
        /// </summary>
        public bool IsCentralized => true;

        public bool AutomaticallyRenewPeekLock { get; set; }

        public bool PartitioningEnabled { get; set; }

        ///// <summary>
        ///// Gets/sets whether to skip creating queues
        ///// </summary>
        public bool DoNotCreateQueuesEnabled { get; set; }

        public void PurgeInputQueue() => _purgeInputQueue();

        public void PrefetchMessages(int prefetchCount)
        {
        }

        /// <summary>
        /// Disposes all resources associated with this particular transport instance
        /// </summary>
        public void Dispose()
        {
            while (_disposables.TryPop(out var disposable))
            {
                disposable.Dispose();
            }
        }
    }
}

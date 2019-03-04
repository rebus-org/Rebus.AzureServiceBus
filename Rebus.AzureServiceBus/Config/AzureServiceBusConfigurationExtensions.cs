using System;
using System.Threading;
using Rebus.AzureServiceBus;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Timeouts;
using Rebus.Topic;
using Rebus.Transport;

// ReSharper disable once CheckNamespace
namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for the Azure Service Bus transport
    /// </summary>
    public static class AzureServiceBusConfigurationExtensions
    {
        const string AsbSubStorageText = "The Azure Service Bus transport was inserted as the subscriptions storage because it has native support for pub/sub messaging";
        const string AsbTimeoutManagerText = "A disabled timeout manager was installed as part of the Azure Service Bus configuration, becuase the transport has native support for deferred messages";

        /// <summary>
        /// Configures Rebus to use Azure Service Bus to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static AzureServiceBusTransportClientSettings UseAzureServiceBusAsOneWayClient(this StandardConfigurer<ITransport> configurer, string connectionString)
        {
            var settingsBuilder = new AzureServiceBusTransportClientSettings();

            configurer
                .OtherService<AzureServiceBusTransport>()
                .Register(c =>
                {
                    var cancellationToken = c.Get<CancellationToken>();
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                    var nameFormatter = c.Get<INameFormatter>();

                    return new AzureServiceBusTransport(
                        connectionString: connectionString,
                        queueName: null,
                        rebusLoggerFactory: rebusLoggerFactory,
                        asyncTaskFactory: asyncTaskFactory,
                        nameFormatter: nameFormatter,
                        cancellationToken: cancellationToken
                    );
                });

            RegisterServices(configurer, () => settingsBuilder.LegacyNamingEnabled);

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);

            return settingsBuilder;
        }

        /// <summary>
        /// Configures Rebus to use Azure Service Bus queues to transport messages, connecting to the service bus instance pointed to by the connection string
        /// (or the connection string with the specified name from the current app.config)
        /// </summary>
        public static AzureServiceBusTransportSettings UseAzureServiceBus(this StandardConfigurer<ITransport> configurer, string connectionString, string inputQueueAddress)
        {
            var settingsBuilder = new AzureServiceBusTransportSettings();

            // register the actual transport as itself
            configurer
                .OtherService<AzureServiceBusTransport>()
                .Register(c =>
                {
                    var nameFormatter = c.Get<INameFormatter>();
                    var cancellationToken = c.Get<CancellationToken>();
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var asyncTaskFactory = c.Get<IAsyncTaskFactory>();

                    var transport = new AzureServiceBusTransport(
                        connectionString: connectionString,
                        queueName: inputQueueAddress,
                        rebusLoggerFactory: rebusLoggerFactory,
                        asyncTaskFactory: asyncTaskFactory,
                        nameFormatter: nameFormatter,
                        cancellationToken: cancellationToken
                    );

                    if (settingsBuilder.PrefetchingEnabled)
                    {
                        transport.PrefetchMessages(settingsBuilder.NumberOfMessagesToPrefetch);
                    }

                    transport.AutomaticallyRenewPeekLock = settingsBuilder.AutomaticPeekLockRenewalEnabled;
                    transport.PartitioningEnabled = settingsBuilder.PartitioningEnabled;
                    transport.DoNotCreateQueuesEnabled = settingsBuilder.DoNotCreateQueuesEnabled;
                    transport.DefaultMessageTimeToLive = settingsBuilder.DefaultMessageTimeToLive;
                    transport.DoNotCheckQueueConfigurationEnabled = settingsBuilder.DoNotCheckQueueConfigurationEnabled;
                    transport.LockDuration = settingsBuilder.LockDuration;
                    transport.AutoDeleteOnIdle = settingsBuilder.AutoDeleteOnIdle;
                    transport.DuplicateDetectionHistoryTimeWindow = settingsBuilder.DuplicateDetectionHistoryTimeWindow;
                    
                    return transport;
                });

            RegisterServices(configurer, () => settingsBuilder.LegacyNamingEnabled);

            // remove deferred messages step
            configurer.OtherService<IPipeline>().Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();

                return new PipelineStepRemover(pipeline)
                    .RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
            });

            return settingsBuilder;
        }

        static void RegisterServices(StandardConfigurer<ITransport> configurer, Func<bool> legacyNamingEnabled)
        {
            // map ITransport to transport implementation
            configurer.Register(c => c.Get<AzureServiceBusTransport>());

            // map subscription storage to transport
            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<AzureServiceBusTransport>(), description: AsbSubStorageText);

            // disable timeout manager
            configurer.OtherService<ITimeoutManager>().Register(c => new DisabledTimeoutManager(), description: AsbTimeoutManagerText);

            configurer.OtherService<INameFormatter>().Register(c =>
            {
                // lazy-evaluated setting because the builder needs a chance to be built upon before getting its settings
                var useLegacyNaming = legacyNamingEnabled();

                if (useLegacyNaming) return new LegacyNameFormatter();
                else return new DefaultNameFormatter();
            });

            configurer.OtherService<DefaultAzureServiceBusTopicNameConvention>().Register(c =>
            {
                // lazy-evaluated setting because the builder needs a chance to be built upon before getting its settings
                var useLegacyNaming = legacyNamingEnabled();

                return new DefaultAzureServiceBusTopicNameConvention(useLegacyNaming: useLegacyNaming);
            });

            configurer.OtherService<ITopicNameConvention>().Register(c => c.Get<DefaultAzureServiceBusTopicNameConvention>());
        }
    }
}
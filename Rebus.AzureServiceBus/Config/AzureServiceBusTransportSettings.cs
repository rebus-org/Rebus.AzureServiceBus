using System;
using Rebus.Exceptions;

namespace Rebus.Config
{
    /// <summary>
    /// Allows for configuring additional options for the Azure Service Bus transport
    /// </summary>
    public class AzureServiceBusTransportSettings
    {
        internal bool PrefetchingEnabled { get; set; }
        internal int NumberOfMessagesToPrefetch { get; set; }
        internal bool PartitioningEnabled { get; set; }
        internal bool DoNotCreateQueuesEnabled { get; set; }
        internal bool AutomaticPeekLockRenewalEnabled { get; set; }
        internal TimeSpan? MessageTimeToLive { get; set; }
        internal TimeSpan? MessagePeekLockDuration { get; set; }

        /// <summary>
        /// Enables partitioning whereby Azure Service Bus will be able to distribute messages between message stores
        /// and this way increase throughput. Enabling partitioning only has an effect on newly created queues.
        /// </summary>
        public AzureServiceBusTransportSettings EnablePartitioning()
        {
            PartitioningEnabled = true;
            return this;
        }

        /// <summary>
        /// Configures the default TTL on the input queue. This is the longest messages get to stay in the input queue.
        /// If a shorter TTL is set on the message when sending it, that TTL is used instead.
        /// </summary>
        public AzureServiceBusTransportSettings SetDefaultMessageTimeToLive(TimeSpan messageTimeToLive)
        {
            if (messageTimeToLive < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentException($"Default message TTL {messageTimeToLive} cannot be used - it must be at least one second");
            }
            MessageTimeToLive = messageTimeToLive;
            return this;
        }

        /// <summary>
        /// Configures the message peek lock duration for received messages. 
        /// </summary>
        public AzureServiceBusTransportSettings SetMessagePeekLockDuration(TimeSpan messagePeekLockDuration)
        {
            if (messagePeekLockDuration < TimeSpan.FromSeconds(5) || messagePeekLockDuration > TimeSpan.FromMinutes(5))
            {
                throw new ArgumentException($"Message peek lock duration {messagePeekLockDuration} cannot be used - it must be at least 5 seconds and at most 5 minutes");
            }
            MessagePeekLockDuration = messagePeekLockDuration;
            return this;
        }

        /// <summary>
        /// Enables prefetching whereby a batch of messages will be prefetched instead of only one at a time.
        /// By enabling prefetching, the automatic peek lock renewal will be disabled, because it is assumed
        /// that prefetching will be enabled only in cases where messages can be processed fairly quickly.
        /// </summary>
        public AzureServiceBusTransportSettings EnablePrefetching(int numberOfMessagesToPrefetch)
        {
            if (numberOfMessagesToPrefetch < 1)
            {
                throw new ArgumentOutOfRangeException($"Cannot set prefetching to {numberOfMessagesToPrefetch} messages - must be at least 1");
            }

            PrefetchingEnabled = true;
            NumberOfMessagesToPrefetch = numberOfMessagesToPrefetch;
            return this;
        }

        /// <summary>
        /// Enables automatic peek lock renewal. Only enable this if you intend on handling messages for a long long time, and
        /// DON'T intend on handling messages quickly - it will have an impact on message receive, so only enable it if you
        /// need it. You should usually strive after keeping message processing times low, much lower than the 5 minute lease
        /// you get with Azure Service Bus.
        /// </summary>
        public AzureServiceBusTransportSettings AutomaticallyRenewPeekLock()
        {
            AutomaticPeekLockRenewalEnabled = true;
            return this;
        }

        /// <summary>
        /// Skips queue creation. Can be used when the connection string does not have manage rights to the queue object, e.g.
        /// when a read-only shared-access signature is used to access an input queue. Please note that the signature MUST
        /// have write access to the configured error queue, unless Azure Service Bus' own dead-lettering is activated on the 
        /// input queue (which is probably the preferred approach with this option)
        /// </summary>
        public AzureServiceBusTransportSettings DoNotCreateQueues()
        {
            DoNotCreateQueuesEnabled = true;
            return this;
        }
    }
}
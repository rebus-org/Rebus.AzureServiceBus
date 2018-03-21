using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusTransport : ITransport, ISubscriptionStorage, IDisposable, IInitializable
    {
        public AzureServiceBusTransport(string connectionString, string inputQueueName, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory)
        {
            
        }

        public void Initialize()
        {
        }

        public void CreateQueue(string address)
        {
        }

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public string Address { get; }

        public bool AutomaticallyRenewPeekLock { get; set; }

        public bool PartitioningEnabled { get; set; }

        public bool DoNotCreateQueuesEnabled { get; set; }

        public void PrefetchMessages(int numberOfMessagesToPrefetch)
        {
            
        }

        public Task<string[]> GetSubscriberAddresses(string topic)
        {
            throw new System.NotImplementedException();
        }

        public Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            throw new System.NotImplementedException();
        }

        public Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool IsCentralized { get; }

        public void Dispose()
        {
        }

        public void PurgeInputQueue()
        {
            
        }
    }
}
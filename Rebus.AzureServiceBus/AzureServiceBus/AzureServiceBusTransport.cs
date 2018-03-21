using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Azure Service Bus to do its thing
    /// </summary>
    public class AzureServiceBusTransport : ITransport, ISubscriptionStorage, IDisposable, IInitializable
    {
        readonly QueueClient _client;
        readonly ILog _log;

        public AzureServiceBusTransport(string connectionString, string inputQueueName, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory)
        {
            Address = inputQueueName;

            if (!IsOneWayClient)
            {
                _client = new QueueClient(connectionString, inputQueueName);
            }
            else
            {
                var connectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);

                _client = new QueueClient(connectionStringBuilder);
            }

            _log = rebusLoggerFactory.GetLogger<AzureServiceBusTransport>();

        }

        public void Initialize()
        {
            if (IsOneWayClient) return;

            _client.RegisterMessageHandler(
                async (message, token) =>
                {
                    Console.WriteLine($"Got message: {message}");
                }, async args =>
                {

                });
        }

        bool IsOneWayClient => string.IsNullOrWhiteSpace(Address);

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
            _client.PrefetchCount = numberOfMessagesToPrefetch;
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

        public void PurgeInputQueue()
        {

        }

        public void Dispose() => AsyncHelpers.RunSync(() => _client.CloseAsync());
    }
}
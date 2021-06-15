using System;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rebus.Internals;

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    public class QueueDeleter : IDisposable
    {
        readonly string _queueName;

        public QueueDeleter(string queueName)
        {
            _queueName = queueName;
        }

        public void Dispose()
        {
            var managementClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);

            AsyncHelpers.RunSync(async () =>
            {
                try
                {
                    var topicDescription = await managementClient.GetQueueAsync(_queueName);

                    await managementClient.DeleteQueueAsync(topicDescription.Value.Name);

                    Console.WriteLine($"Deleted queue '{_queueName}'");
                }
                catch (ServiceBusException)
                {
                    // it's ok
                }
            });
        }
    }
}
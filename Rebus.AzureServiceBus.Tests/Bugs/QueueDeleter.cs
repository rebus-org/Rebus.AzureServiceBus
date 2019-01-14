using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
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
            var managementClient = new ManagementClient(AsbTestConfig.ConnectionString);

            AsyncHelpers.RunSync(async () =>
            {
                try
                {
                    var topicDescription = await managementClient.GetQueueAsync(_queueName);

                    await managementClient.DeleteQueueAsync(topicDescription.Path);

                    Console.WriteLine($"Deleted queue '{_queueName}'");
                }
                catch (MessagingEntityNotFoundException)
                {
                    // it's ok
                }
            });
        }
    }
}
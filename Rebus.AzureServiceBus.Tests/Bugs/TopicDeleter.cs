using System;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rebus.Internals;

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    public class TopicDeleter : IDisposable
    {
        readonly string _topicName;

        public TopicDeleter(string topicName)
        {
            _topicName = topicName;
        }

        public void Dispose()
        {
            var managementClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);

            AsyncHelpers.RunSync(async () =>
            {
                try
                {
                    var topicDescription = await managementClient.GetTopicAsync(_topicName);

                    await managementClient.DeleteTopicAsync(topicDescription.Value.Name);

                    Console.WriteLine($"Deleted topic '{_topicName}'");
                }
                catch (ServiceBusException)
                {
                    // it's ok
                }
            });
        }
    }
}
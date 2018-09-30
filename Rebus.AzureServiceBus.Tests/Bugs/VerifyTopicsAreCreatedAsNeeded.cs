using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    [TestFixture]
    public class VerifyTopicsAreCreatedAsNeeded : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {

            _activator = new BuiltinHandlerActivator();
            
            Using(_activator);
            
            Configure.With(_activator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, TestConfig.GetName("topictest")))
                .Start();
        }

        [Test]
        public async Task CanUseTopicWithRandomName()
        {
            await DeleteAllTopics();

            var topicName = Guid.NewGuid().ToString("N");

            // try to ensure we remove the topic afterwards
            Using(new TopicDeleter(topicName));

            var eventWasReceived = new ManualResetEvent(false);

            _activator.Handle<string>(async str => eventWasReceived.Set());

            var bus = _activator.Bus;

            await bus.Advanced.Topics.Subscribe(topicName);

            await bus.Advanced.Topics.Publish(topicName, "hej med dig min veeeeen!");

            eventWasReceived.WaitOrDie(timeout: TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task CanPublishToTopicThatDoesNotExist()
        {
            await DeleteAllTopics();

            var topicName = Guid.NewGuid().ToString("N");

            // try to ensure we remove the topic afterwards
            Using(new TopicDeleter(topicName));

            var bus = _activator.Bus;

            // must not throw!
            await bus.Advanced.Topics.Publish(topicName, "hej med dig min veeeeen!");
        }

        static async Task DeleteAllTopics()
        {
            var managementClient = new ManagementClient(AsbTestConfig.ConnectionString);

            while (true)
            {
                var topics = await managementClient.GetTopicsAsync();

                if (!topics.Any()) return;

                await Task.WhenAll(topics.Select(async topic =>
                {
                    Console.WriteLine($"Deleting topic '{topic.Path}'");
                    await managementClient.DeleteTopicAsync(topic.Path);
                }));
            }
        }
    }
}
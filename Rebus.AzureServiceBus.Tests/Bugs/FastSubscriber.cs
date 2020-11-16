using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    [TestFixture]
    public class FastSubscriber : FixtureBase
    {
        [TestCase(10)]
        public async Task TakeTime(int topicCount)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            var bus = Configure.With(activator)
                .Logging(l => l.Console(LogLevel.Warn))
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "my-input-queue"))
                .Start();

            var stopwatch = Stopwatch.StartNew();

            Task.WaitAll(Enumerable.Range(0, topicCount)
                .Select(async n =>
                {
                    var topicName = $"topic-{n}";

                    Using(new TopicDeleter(topicName));

                    await bus.Advanced.Topics.Subscribe(topicName);
                })
                .ToArray());

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($"Subscribing to {topicCount} topics took {elapsedSeconds:0.0}s - that's {elapsedSeconds / topicCount:0.0} s/subscription");
        }

        [Test]
        [Ignore("run manually")]
        public async Task DeleteAllTopics()
        {
            var managementClient = new ManagementClient(AsbTestConfig.ConnectionString);

            while (true)
            {
                var topics = await managementClient.GetTopicsAsync();

                if (!topics.Any()) break;

                foreach (var topic in topics)
                {
                    Console.Write($"Deleting '{topic.Path}'... ");
                    await managementClient.DeleteTopicAsync(topic.Path);
                    Console.WriteLine("OK");
                }
            }
        }
    }
}
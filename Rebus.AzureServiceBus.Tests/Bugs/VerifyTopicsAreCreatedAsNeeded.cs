using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
public class VerifyTopicsAreCreatedAsNeeded : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _busStarter = Configure.With(_activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, TestConfig.GetName("topictest")))
            .Create();
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

        _busStarter.Start();

        var bus = _activator.Bus;

        await bus.Advanced.Topics.Subscribe(topicName);

        await bus.Advanced.Topics.Publish(topicName, "hej med dig min veeeeen!");

        eventWasReceived.WaitOrDie(timeout: TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CanPublishToTopicThatDoesNotExist()
    {
        _busStarter.Start();

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
        var managementClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);

        await foreach (var topic in managementClient.GetTopicsAsync())
        {
            Console.WriteLine($"Deleting topic '{topic.Name}'");
            await managementClient.DeleteTopicAsync(topic.Name);
        }
    }
}
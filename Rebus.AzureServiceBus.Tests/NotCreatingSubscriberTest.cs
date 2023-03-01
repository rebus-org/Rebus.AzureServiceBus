using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class NotCreatingSubscriberTest : FixtureBase
{
    [Test]
    public async Task ShouldNotRegisterSubscriberWhenConfiguredNotTo()
    {
        var connectionString = AsbTestConfig.ConnectionString;
        var managementClient = new ServiceBusAdministrationClient(connectionString);

        var topic = Guid.NewGuid().ToString("N");
        var queue = Guid.NewGuid().ToString("N");

        Using(new TopicDeleter(topic));
        Using(new QueueDeleter(queue));
        
        var bus = Configure.With(Using(new BuiltinHandlerActivator()))
            .Logging(l => l.ColoredConsole())
            .Transport(t => t
                .UseAzureServiceBus(connectionString, queue)
                .DoNotCreateSubscriptions())
            .Start();

        await bus.Advanced.Topics.Subscribe(topic);
        
        var exists = await managementClient.SubscriptionExistsAsync(topic, queue);
        Assert.IsFalse(exists.Value);
    }
    
    [Test]
    public async Task ShouldNotRemoveSubscriberWhenConfiguredNotTo()
    {
        var connectionString = AsbTestConfig.ConnectionString;
        var managementClient = new ServiceBusAdministrationClient(connectionString);

        var topic = Guid.NewGuid().ToString("N");
        var queue = Guid.NewGuid().ToString("N");

        await managementClient.CreateTopicAsync(topic);
        Using(new TopicDeleter(topic));

        await managementClient.CreateSubscriptionAsync(topic, queue);
        Using(new QueueDeleter(queue));
        
        var bus = Configure.With(Using(new BuiltinHandlerActivator()))
            .Logging(l => l.ColoredConsole())
            .Transport(t => t
                .UseAzureServiceBus(connectionString, queue)
                .DoNotCreateSubscriptions())
            .Start();

        await bus.Advanced.Topics.Unsubscribe(topic);
        
        var exists = await managementClient.SubscriptionExistsAsync(topic, queue);
        Assert.IsTrue(exists.Value);
    }
}
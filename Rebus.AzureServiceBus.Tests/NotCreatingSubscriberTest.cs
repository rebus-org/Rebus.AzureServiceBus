using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
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

        await managementClient.CreateTopicAsync(topic);
        Using(new TopicDeleter(topic));
        
        await managementClient.CreateQueueAsync(queue);
        Using(new QueueDeleter(queue));
        
        var arm = new ArmClient(new DefaultAzureCredential());
        var azure = await arm.GetSubscriptions().GetAsync("9391c94a-1a42-4f34-a835-ce73e4661f34");
        var rg = await azure.Value.GetResourceGroupAsync("rg-servicebus");
        var sb = await rg.Value.GetServiceBusNamespaceAsync("sbmanuel");
        var t = await sb.Value.GetServiceBusTopicAsync(topic);
         await t.Value.GetServiceBusSubscriptions()
            .CreateOrUpdateAsync(WaitUntil.Completed, queue, new ServiceBusSubscriptionData
        {
            ForwardTo = queue
        });

        var before = await managementClient.GetSubscriptionAsync(topic, queue);
        
        var bus = Configure.With(Using(new BuiltinHandlerActivator()))
            .Logging(l => l.ColoredConsole())
            .Transport(t => t
                .UseAzureServiceBus(connectionString, queue)
                .DoNotCreateQueues()
                .DoNotCheckQueueConfiguration())
            .Start();

        await bus.Advanced.Topics.Subscribe(topic);
        var after = await managementClient.GetSubscriptionAsync(topic, queue);
        Assert.AreEqual(before.Value.ForwardTo, after.Value.ForwardTo);
    }
}
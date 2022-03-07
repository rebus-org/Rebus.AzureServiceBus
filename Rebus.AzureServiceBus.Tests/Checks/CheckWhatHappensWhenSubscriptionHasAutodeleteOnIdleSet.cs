using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Internals;
using Rebus.Tests.Contracts;

// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
public class CheckWhatHappensWhenSubscriptionHasAutodeleteOnIdleSet : FixtureBase
{
    [Test]
    [Description("Emulates the topology created by Rebus when creating a topic with a single subscriber and checks that AutoDeleteOnIdle can be set")]
    [Ignore("Ignored because it fails. Seems like it's not possible to combine forwarding with auto-delete-on-idle on a subscription - it will simply ignore the auto-delete timeout.")]
    public async Task EstablishSubscriptionWithAutoDeleteOnIdle()
    {
        var topicName = Guid.NewGuid().ToString("N");
        var queueName = Guid.NewGuid().ToString("N");

        // clean up afterwards
        Using(new TopicDeleter(topicName));
        Using(new QueueDeleter(queueName));

        var adminClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);

        await adminClient.CreateTopicAsync(topicName);
        await adminClient.CreateQueueAsync(queueName);

        var options = new CreateSubscriptionOptions(topicName: topicName, subscriptionName: queueName)
        {
            AutoDeleteOnIdle = TimeSpan.FromMinutes(5),
            ForwardTo = queueName
        };

        var subscription = await adminClient.CreateSubscriptionAsync(options);

        var props = subscription.Value;

        var serviceBusClient = new ServiceBusClient(AsbTestConfig.ConnectionString);

        Assert.That(props.ForwardTo, Is.EqualTo(serviceBusClient.CreateSender(queueName).GetQueuePath()));
        Assert.That(props.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }
}
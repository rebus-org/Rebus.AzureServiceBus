using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
public class TestNativeDeliveryCount : FixtureBase
{
    [Test]
    public async Task ItIsThisEasy()
    {
        var queueName = TestConfig.GetName("deadlettering");
        var deliveryCounts = new ConcurrentQueue<int>();

        Using(new QueueDeleter(queueName));

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<TestMessage>(async (_, context, _) =>
        {
            if (context.Headers.TryGetValue(Headers.DeliveryCount, out var str) &&
                int.TryParse(str, out var deliveryCount))
            {
                deliveryCounts.Enqueue(deliveryCount);
            }

            throw new ApplicationException("just pretend something is wrong");
        });

        var bus = Configure.With(activator)
            .Transport(t =>
            {
                t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName)
                    .UseNativeMessageDeliveryCount();
            })
            .Start();

        await bus.SendLocal(new TestMessage());

        await deliveryCounts.WaitUntil(c => c.Count == 5);

        Assert.That(deliveryCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    class TestMessage { }
}
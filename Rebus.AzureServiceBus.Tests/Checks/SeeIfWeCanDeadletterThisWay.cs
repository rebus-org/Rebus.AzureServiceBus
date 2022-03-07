using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
public class UseAzureServiceBusNativeDeadlettering : FixtureBase
{
    [Test]
    public async Task ItIsThisEasy()
    {
        var queueName = TestConfig.GetName("deadlettering");

        Using(new QueueDeleter(queueName));

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<TestMessage>(async _ => throw new FailFastException("failing fast"));

        var bus = Configure.With(activator)
            .Transport(t =>
            {
                t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName);
                t.UseNativeDeadlettering();
            })
            .Options(o => o.SimpleRetryStrategy(maxDeliveryAttempts: 1))
            .Start();

        await bus.SendLocal(new TestMessage());

        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    class TestMessage { }
}
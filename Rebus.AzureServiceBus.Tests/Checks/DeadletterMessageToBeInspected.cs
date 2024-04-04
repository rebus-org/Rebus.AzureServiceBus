using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Tests.Contracts;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
public class DeadletterMessageToBeInspected : FixtureBase
{
    [Test]
    public async Task ItIsThisEasy()
    {
        var queueName = TestConfig.GetName("deadlettering");

        Using(new QueueDeleter(queueName));

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<TestMessage>(async _ => throw new FailFastException("failing fast"));

        var bus = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        await bus.SendLocal(new TestMessage());
    }

    class TestMessage { }
}
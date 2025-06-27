using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
[Ignore("No, we can't. Topics and queues exist in the same namespace, so it's just not possible.")]
public class CanPublishToTopicWithSameNameAsQueue : FixtureBase
{
    [Test]
    public async Task CanDoIt()
    {
        using var activator = new BuiltinHandlerActivator();

        var bus = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "thenameisthesame"))
            .Start();

        await bus.Advanced.Topics.Publish("thenameisthesame", new TextCarrier("text"));
    }

    record TextCarrier(string Text);
}
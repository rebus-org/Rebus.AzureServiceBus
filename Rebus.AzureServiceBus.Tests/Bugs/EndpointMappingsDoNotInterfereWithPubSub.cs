using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
#pragma warning disable CS1998

// ReSharper disable AccessToDisposedClosure

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
[Description("Tried to reproduce an issue, but was unsuccessful.")]
public class EndpointMappingsDoNotInterfereWithPubSub : FixtureBase
{
    [Test]
    public async Task ItWorksAsItShould()
    {
        using var gotTheEvent = new ManualResetEvent(initialState: false);

        var publisher = CreateBus("publisher");
        var subscriber = CreateBus("subscriber", messageHandler: _ => gotTheEvent.Set());

        await subscriber.Subscribe<MyEvent>();

        await publisher.Publish(new MyEvent());

        gotTheEvent.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    IBus CreateBus(string queueName, Action<MyEvent> messageHandler = null)
    {
        var activator = Using(new BuiltinHandlerActivator());

        if (messageHandler != null)
        {
            activator.Handle<MyEvent>(async msg => messageHandler(msg));
        }

        return Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Routing(r => r.TypeBased().Map<MyEvent>("somethingsomething completely invalid full of invalid characters like :/\\😈 and such"))
            .Start();
    }

    record MyEvent;
}
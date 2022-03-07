using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleStringLiteral

// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
public class OneWayClientCanDeferMessageOntoAnotherQueue : FixtureBase
{
    [Test]
    public async Task ItWorksAsItShould()
    {
        var connectionString = AsbTestConfig.ConnectionString;
        var queueName = TestConfig.GetName("some-target-queue");
        var gotTheDeferredMessage = new ManualResetEvent(initialState: false);

        // start bus instance, which is supposed to receive the deferred message
        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<DeferredMessage>(async msg => gotTheDeferredMessage.Set());

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(connectionString, queueName))
            .Start();

        // create one-way client
        var sender = Configure.With(Using(new BuiltinHandlerActivator()))
            .Transport(t => t.UseAzureServiceBusAsOneWayClient(connectionString))
            .Routing(r => r.TypeBased().Map<DeferredMessage>(queueName))
            .Start();

        await sender.Defer(TimeSpan.FromSeconds(1), new DeferredMessage());

        gotTheDeferredMessage.WaitOrDie(timeout: TimeSpan.FromSeconds(50), 
            errorMessage: "Did not receive the message within 5 s timeout");
    }

    class DeferredMessage { }
}
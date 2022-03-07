using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
public class VerifyDeferredMessagesWorkAsExpected : FixtureBase
{
    [Test]
    public async Task CanDeferWithRouting()
    {
        using var messageReceived = new ManualResetEvent(initialState: false);
        using var senderActivator = new BuiltinHandlerActivator();
        using var receiverActivator = new BuiltinHandlerActivator();

        receiverActivator.Handle<RoutedEvent>(async _ => messageReceived.Set());

        var senderQueueName = TestConfig.GetName("deferral-routing-sender");
        var receiverQueueName = TestConfig.GetName("deferral-routing-receiver");

        var bus = Configure.With(senderActivator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, senderQueueName))
            .Routing(t => t.TypeBased().Map<RoutedEvent>(receiverQueueName))
            .Start();

        Configure.With(receiverActivator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, receiverQueueName))
            .Start();

        //await bus.DeferLocal(TimeSpan.FromSeconds(0.1), new RoutedEvent());
        await bus.Defer(TimeSpan.FromSeconds(0.1), new RoutedEvent());

        messageReceived.WaitOrDie(timeout: TimeSpan.FromSeconds(3), errorMessage: "Did not receive RoutedEvent within 3 s of waiting");
    }

    class RoutedEvent { }
}
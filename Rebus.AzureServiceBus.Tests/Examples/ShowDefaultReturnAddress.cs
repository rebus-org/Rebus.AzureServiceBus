using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.AzureServiceBus.Tests.Examples;

[TestFixture]
public class ShowDefaultReturnAddress : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        using var gotTheFinalReply = new ManualResetEvent(initialState: false);

        var receiverQueueName = Guid.NewGuid().ToString("N");
        var finalDestinationQueueName = Guid.NewGuid().ToString("N");

        Using(new QueueDeleter(receiverQueueName));
        Using(new QueueDeleter(finalDestinationQueueName));

        using var receiver = new BuiltinHandlerActivator();

        receiver.Handle<InitialMessage>(async (b, _) => await b.Reply(new FinalReply()));

        using var finalDestination = new BuiltinHandlerActivator();

        finalDestination.Handle<FinalReply>(async _ => gotTheFinalReply.Set());

        Configure.With(receiver)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, receiverQueueName))
            .Start();

        Configure.With(finalDestination)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, finalDestinationQueueName))
            .Start();

        using var bus = Configure.OneWayClient()
            .Transport(t => t.UseAzureServiceBusAsOneWayClient(AsbTestConfig.ConnectionString))
            .Routing(r => r.TypeBased().Map<InitialMessage>(receiverQueueName))
            .Options(o => o.SetDefaultReturnAddress(finalDestinationQueueName))
            .Start();

        await bus.Send(new InitialMessage());

        gotTheFinalReply.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    record InitialMessage;

    record FinalReply;
}
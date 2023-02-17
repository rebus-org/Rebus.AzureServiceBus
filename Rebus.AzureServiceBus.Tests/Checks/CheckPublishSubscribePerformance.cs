using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable CS1998

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
[Description("Simple check just to get some kind of idea of some numbers")]
public class CheckPublishSubscribePerformance : FixtureBase
{
    [Test]
    public async Task CheckEndToEndLatency()
    {
        var receiveTimes = new ConcurrentQueue<ReceiveInfo>();

        async Task RegisterReceiveTime(IBus bus, IMessageContext messageContext, string msg)
        {
            var actualReceiveTime = DateTimeOffset.Now;
            var messageSendTime = messageContext.Headers.GetValue(Headers.SentTime).ToDateTimeOffset();
            var receiveInfo = new ReceiveInfo(actualReceiveTime, messageSendTime);
            receiveTimes.Enqueue(receiveInfo);
        }

        var publisher = GetBus("sender");
        var subscriber = GetBus("receiver", handlers: activator => activator.Handle<string>(RegisterReceiveTime));
        await subscriber.Subscribe<string>();

        var actualSendTime = DateTimeOffset.Now;
        await publisher.Publish("HEJ MED DIG MIN VEN! 🙂");

        await receiveTimes.WaitUntil(q => q.Count == 1);

        if (!receiveTimes.TryDequeue(out var receiveInfo))
        {
            throw new AssertionException("Did not get receive info within timeout");
        }

        Console.WriteLine($@"

Actual send time: {actualSendTime}
Receive info
       Sent time: {receiveInfo.MessageSendTime}
    Receive time: {receiveInfo.ReceiveTime}

");
    }

    record ReceiveInfo(DateTimeOffset ReceiveTime, DateTimeOffset MessageSendTime);

    IBus GetBus(string queueName, Action<BuiltinHandlerActivator> handlers = null)
    {
        var activator = Using(new BuiltinHandlerActivator());

        handlers?.Invoke(activator);

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        return activator.Bus;
    }
}
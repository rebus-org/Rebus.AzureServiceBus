using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
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
    protected override void SetUp()
    {
        base.SetUp();

        Using(new QueueDeleter("publisher"));
        Using(new QueueDeleter("subscriber"));
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100)]
    [Repeat(10)]
    public async Task CheckEndToEndLatency(int count)
    {
        var receiveTimes = new ConcurrentQueue<ReceiveInfo>();

        async Task RegisterReceiveTime(IBus bus, IMessageContext messageContext, TimedEvent evt)
        {
            var actualReceiveTime = DateTimeOffset.Now;
            var receiveInfo = new ReceiveInfo(actualReceiveTime, evt.Time);
            receiveTimes.Enqueue(receiveInfo);
        }

        var publisher = GetBus("publisher");
        var subscriber = GetBus("subscriber", handlers: activator => activator.Handle<TimedEvent>(RegisterReceiveTime));

        await subscriber.Subscribe<TimedEvent>();

        count.Times(() => publisher.Advanced.SyncBus.Publish(new TimedEvent(DateTimeOffset.Now)));

        await receiveTimes.WaitUntil(q => q.Count == count, timeoutSeconds: 20 + count*2);

        var latencies = receiveTimes.Select(a => a.Latency().TotalSeconds).ToList();

        var average = latencies.Average();
        var median = latencies.Median();
        var min = latencies.Min();
        var max = latencies.Max();

        Console.WriteLine($"AVG: {average:0.0} s, MED: {median:0.0} s, MIN: {min:0.0} s, MAX: {max:0.0} s");
    }

    record TimedEvent(DateTimeOffset Time);

    record ReceiveInfo(DateTimeOffset ReceiveTime, DateTimeOffset SendTime)
    {
        public TimeSpan Latency() => ReceiveTime - SendTime;
    }

    IBus GetBus(string queueName, Action<BuiltinHandlerActivator> handlers = null)
    {
        var activator = Using(new BuiltinHandlerActivator());

        handlers?.Invoke(activator);

        Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel.Warn))
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        return activator.Bus;
    }
}
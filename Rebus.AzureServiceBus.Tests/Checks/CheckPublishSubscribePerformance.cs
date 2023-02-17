using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Internals;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
// ReSharper disable MethodSupportsCancellation
#pragma warning disable CS4014

#pragma warning disable CS1998

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
[Description("Simple check just to get some kind of idea of some numbers")]
public class CheckPublishSubscribePerformance : FixtureBase
{
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100, Explicit = true)]
    [Repeat(5)]
    public async Task CheckEndToEndLatency(int count)
    {
        var receiveTimes = new ConcurrentQueue<ReceiveInfo>();

        async Task RegisterReceiveTime(IBus bus, IMessageContext messageContext, TimedEvent evt)
        {
            var actualReceiveTime = DateTimeOffset.Now;
            var receiveInfo = new ReceiveInfo(actualReceiveTime, evt.Time);
            receiveTimes.Enqueue(receiveInfo);
        }

        Using(new QueueDeleter("publisher"));
        var publisher = GetBus("publisher");

        Using(new QueueDeleter("subscriber"));
        var subscriber = GetBus("subscriber", handlers: activator => activator.Handle<TimedEvent>(RegisterReceiveTime));

        await subscriber.Subscribe<TimedEvent>();

        await Parallel.ForEachAsync(Enumerable.Range(0, count),
            async (_, _) => await publisher.Publish(new TimedEvent(DateTimeOffset.Now)));

        await receiveTimes.WaitUntil(q => q.Count == count, timeoutSeconds: 30 + count * 5);

        var latencies = receiveTimes.Select(a => a.Latency().TotalSeconds).ToList();

        var average = latencies.Average();
        var median = latencies.Median();
        var min = latencies.Min();
        var max = latencies.Max();

        Console.WriteLine($"AVG: {average:0.0} s, MED: {median:0.0} s, MIN: {min:0.0} s, MAX: {max:0.0} s");
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100, Explicit = true)]
    [Repeat(5)]
    public async Task CheckEndToEndLatency_NoRebus_ServiceBusProcessor(int count)
    {
        var receiveTimes = new ConcurrentQueue<ReceiveInfo>();

        async Task RegisterReceiveTime(TimedEvent evt)
        {
            var actualReceiveTime = DateTimeOffset.Now;
            var receiveInfo = new ReceiveInfo(actualReceiveTime, evt.Time);
            receiveTimes.Enqueue(receiveInfo);
        }

        var client = new ServiceBusClient(AsbTestConfig.ConnectionString);
        var admin = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);

        Using(new QueueDeleter("subscriber"));
        await admin.CreateQueueIfNotExistsAsync("subscriber");

        Using(new TopicDeleter(nameof(TimedEvent)));
        await admin.CreateTopicAsync(nameof(TimedEvent));

        var options = new CreateSubscriptionOptions(nameof(TimedEvent), $"{nameof(TimedEvent)}_subscriber")
        {
            ForwardTo = "subscriber"
        };

        await admin.CreateSubscriptionAsync(options);

        var publisher = client.CreateSender(nameof(TimedEvent));
        var subscriber = client.CreateProcessor("subscriber");

        subscriber.ProcessMessageAsync += async args =>
        {
            var message = args.Message;
            try
            {
                var bytes = message.Body.ToArray();
                var json = Encoding.UTF8.GetString(bytes);
                var timedEvent = JsonConvert.DeserializeObject<TimedEvent>(json);

                await RegisterReceiveTime(timedEvent);

                await args.CompleteMessageAsync(message);
            }
            catch
            {
                await args.AbandonMessageAsync(message);
                throw;
            }
        };
        subscriber.ProcessErrorAsync += async _ => { };

        try
        {
            await subscriber.StartProcessingAsync();

            await Parallel.ForEachAsync(Enumerable.Range(0, count),
                async (_, token) =>
                    await publisher.SendMessageAsync(
                        new ServiceBusMessage(JsonConvert.SerializeObject(new TimedEvent(DateTimeOffset.Now))), token));

            await receiveTimes.WaitUntil(q => q.Count == count, timeoutSeconds: 30 + count * 5);

            var latencies = receiveTimes.Select(a => a.Latency().TotalSeconds).ToList();

            var average = latencies.Average();
            var median = latencies.Median();
            var min = latencies.Min();
            var max = latencies.Max();

            Console.WriteLine($"AVG: {average:0.0} s, MED: {median:0.0} s, MIN: {min:0.0} s, MAX: {max:0.0} s");
        }
        finally
        {
            await subscriber.StopProcessingAsync();
        }
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100, Explicit = true)]
    [Repeat(5)]
    public async Task CheckEndToEndLatency_NoRebus_ServiceBusReceiver(int count)
    {
        var receiveTimes = new ConcurrentQueue<ReceiveInfo>();

        async Task RegisterReceiveTime(TimedEvent evt)
        {
            var actualReceiveTime = DateTimeOffset.Now;
            var receiveInfo = new ReceiveInfo(actualReceiveTime, evt.Time);
            receiveTimes.Enqueue(receiveInfo);
        }

        var client = new ServiceBusClient(AsbTestConfig.ConnectionString);
        var admin = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);

        Using(new QueueDeleter("subscriber"));
        await admin.CreateQueueIfNotExistsAsync("subscriber");

        Using(new TopicDeleter(nameof(TimedEvent)));
        await admin.CreateTopicAsync(nameof(TimedEvent));

        var options = new CreateSubscriptionOptions(nameof(TimedEvent), $"{nameof(TimedEvent)}_subscriber")
        {
            ForwardTo = "subscriber"
        };

        await admin.CreateSubscriptionAsync(options);

        var publisher = client.CreateSender(nameof(TimedEvent));
        var subscriber = client.CreateReceiver("subscriber");

        using var stopReceiver = new CancellationTokenSource();
        var cancellationToken = stopReceiver.Token;

        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await subscriber.ReceiveMessageAsync(TimeSpan.FromSeconds(50), cancellationToken);

                    try
                    {
                        var bytes = message.Body.ToArray();
                        var json = Encoding.UTF8.GetString(bytes);
                        var timedEvent = JsonConvert.DeserializeObject<TimedEvent>(json);

                        await RegisterReceiveTime(timedEvent);

                        await subscriber.CompleteMessageAsync(message);
                    }
                    catch
                    {
                        await subscriber.AbandonMessageAsync(message);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // we're exiting
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Unhandled exception in receiver loop: {exception}");
                }
            }
        }, cancellationToken);

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, count),
                async (_, token) =>
                    await publisher.SendMessageAsync(
                        new ServiceBusMessage(JsonConvert.SerializeObject(new TimedEvent(DateTimeOffset.Now))), token));

            await receiveTimes.WaitUntil(q => q.Count == count, timeoutSeconds: 30 + count * 5);

            var latencies = receiveTimes.Select(a => a.Latency().TotalSeconds).ToList();

            var average = latencies.Average();
            var median = latencies.Median();
            var min = latencies.Min();
            var max = latencies.Max();

            Console.WriteLine($"AVG: {average:0.0} s, MED: {median:0.0} s, MIN: {min:0.0} s, MAX: {max:0.0} s");
        }
        finally
        {
            stopReceiver.Cancel();
        }
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
using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleOther

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class AzureServiceBusPeekLockRenewalTest_ShorterDuration : FixtureBase
{
    static readonly string ConnectionString = AsbTestConfig.ConnectionString;
    static readonly string QueueName = TestConfig.GetName("input");

    readonly ConsoleLoggerFactory _consoleLoggerFactory = new ConsoleLoggerFactory(false);

    BuiltinHandlerActivator _activator;
    AzureServiceBusTransport _transport;
    IBusStarter _busStarter;
    IBus _bus;

    protected override void SetUp()
    {
        _transport = new AzureServiceBusTransport(ConnectionString, QueueName, _consoleLoggerFactory, new TplAsyncTaskFactory(_consoleLoggerFactory), new DefaultNameFormatter());

        Using(_transport);

        _transport.Initialize();
        _transport.PurgeInputQueue();

        _activator = new BuiltinHandlerActivator();

        _busStarter = Configure.With(_activator)
            .Logging(l => l.Use(new ListLoggerFactory(outputToConsole: true, detailed: true)))
            .Transport(t =>
            {
                t.UseAzureServiceBus(ConnectionString, QueueName)
                    .SetMessagePeekLockDuration(messagePeekLockDuration: TimeSpan.FromMinutes(1))
                    .AutomaticallyRenewPeekLock();
            })
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Create();

        _bus = _busStarter.Bus;

        Using(_bus);
    }

    [Test]
    public async Task ItWorks()
    {
        var gotMessage = new ManualResetEvent(false);

        _activator.Handle<string>(async (bus, context, message) =>
        {
            Console.WriteLine($"Got message with ID {context.Headers.GetValue(Headers.MessageId)} - waiting 2 minutes....");
            await Task.Delay(TimeSpan.FromMinutes(2));
            Console.WriteLine("done waiting");

            gotMessage.Set();
        });

        _busStarter.Start();

        await _bus.SendLocal("hej med dig min ven!");

        gotMessage.WaitOrDie(TimeSpan.FromMinutes(2.1));

        // shut down bus
        _bus.Dispose();

        // see if queue is empty
        using var scope = new RebusTransactionScope();

        var message = await _transport.Receive(scope.TransactionContext, CancellationToken.None);

        await scope.CompleteAsync();

        if (message != null)
        {
            throw new AssertionException(
                $"Did not expect to receive a message - got one with ID {message.Headers.GetValue(Headers.MessageId)}");
        }
    }
}
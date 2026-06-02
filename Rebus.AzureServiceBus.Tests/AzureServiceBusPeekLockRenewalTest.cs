using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.AzureServiceBus.Tests.Bugs;
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

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class AzureServiceBusPeekLockRenewalTest : FixtureBase
{
    static readonly string ConnectionString = AsbTestConfig.ConnectionString;

    readonly ConsoleLoggerFactory _consoleLoggerFactory = new(false);

    BuiltinHandlerActivator _activator;
    AzureServiceBusTransport _transport;
    IBus _bus;
    IBusStarter _busStarter;
    string _queueName;

    protected override void SetUp()
    {
        _queueName = TestConfig.GetName("input");

        Using(new QueueDeleter(_queueName));

        _transport = new AzureServiceBusTransport(ConnectionString, _queueName, _consoleLoggerFactory, new TplAsyncTaskFactory(_consoleLoggerFactory), new DefaultNameFormatter(), new Messages.DefaultMessageConverter());

        Using(_transport);

        _transport.Initialize();
        _transport.PurgeInputQueue();

        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _busStarter = Configure.With(_activator)
            .Logging(l => l.Use(new ListLoggerFactory(outputToConsole: true, detailed: true)))
            .Transport(t => t.UseAzureServiceBus(ConnectionString, _queueName).AutomaticallyRenewPeekLock())
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Create();

        _bus = _busStarter.Bus;
    }

    [Test, Explicit("Can be used to check silencing behavior when receive errors occur")]
    public void ReceiveExceptions()
    {
        Using(_transport);

        Thread.Sleep(TimeSpan.FromMinutes(10));
    }

    [Test]
    public async Task ItWorks()
    {
        var gotMessage = new ManualResetEvent(false);

        _activator.Handle<string>(async (bus, context, message) =>
        {
            Console.WriteLine($"Got message with ID {context.Headers.GetValue(Headers.MessageId)} - waiting 6 minutes....");

            // longer than the longest asb peek lock in the world...
            //await Task.Delay(TimeSpan.FromSeconds(3));
            await Task.Delay(TimeSpan.FromMinutes(6));

            Console.WriteLine("done waiting");

            gotMessage.Set();
        });

        _busStarter.Start();

        await _bus.SendLocal("hej med dig min ven!");

        gotMessage.WaitOrDie(TimeSpan.FromMinutes(6.5));

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
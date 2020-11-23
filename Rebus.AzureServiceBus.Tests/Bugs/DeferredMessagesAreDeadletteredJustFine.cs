using System;
using System.Collections.Generic;
using System.Linq;
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
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Threading.TaskParallelLibrary;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    [TestFixture]
    public class DeferredMessagesAreDeadletteredJustFine : FixtureBase
    {
        AzureServiceBusTransport _deadletters;
        ListLoggerFactory _loggerFactory;

        protected override void SetUp()
        {
            _loggerFactory = new ListLoggerFactory(outputToConsole: false);

            _deadletters = GetAsbTransport("error");
            Using(_deadletters);
            _deadletters.Initialize();

            _deadletters.PurgeInputQueue();
        }

        [Test]
        public async Task WhatTheClassSays()
        {
            var queueName = TestConfig.GetName("defer-deadletter");

            PurgeQueue(queueName);

            var deliveryAttempts = 0;

            Using(new QueueDeleter(queueName));

            using var activator = new BuiltinHandlerActivator();

            activator.Handle<Poison>(async _ =>
            {
                deliveryAttempts++;
                throw new ArgumentException("can't take it");
            });

            var bus = Configure.With(activator)
                .Logging(l => l.Use(_loggerFactory))
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
                .Start();

            var knownMessageId = Guid.NewGuid().ToString();

            await bus.DeferLocal(TimeSpan.FromSeconds(1), new Poison(), new Dictionary<string, string>{["custom-id"] = knownMessageId});

            await Task.Delay(TimeSpan.FromSeconds(1));

            var transportMessage = await _deadletters.WaitForNextMessage(timeoutSeconds: 5);

            Assert.That(transportMessage.Headers.GetValue("custom-id"), Is.EqualTo(knownMessageId));
            Assert.That(deliveryAttempts, Is.EqualTo(5), "Expected exactly five delivery attempts followed by dead-lettering");

            var errorLogLines = _loggerFactory.Where(log => log.Level == LogLevel.Error).ToList();

            Assert.That(errorLogLines.Count, Is.EqualTo(1), "Expected one single 'message was dead-lettered' log line");
        }

        void PurgeQueue(string queueName)
        {
            using var transport = GetAsbTransport(queueName);
            transport.Initialize();
            transport.PurgeInputQueue();
        }

        AzureServiceBusTransport GetAsbTransport(string queueName) =>
            new AzureServiceBusTransport(
                AsbTestConfig.ConnectionString,
                queueName,
                _loggerFactory,
                new TplAsyncTaskFactory(_loggerFactory),
                new DefaultNameFormatter(),
                CancellationToken.None
            );

        class Poison { }
    }

    public class Sladrehank : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var messageId = transportMessage.GetMessageId();

            Console.WriteLine($"HANDLING MSG {messageId}");

            await next();
        }
    }
}
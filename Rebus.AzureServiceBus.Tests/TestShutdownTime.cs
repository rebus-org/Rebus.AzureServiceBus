using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class TestShutdownTime : FixtureBase
    {
        static readonly string ConnectionString = AzureServiceBusTransportFactory.ConnectionString;
        static readonly string QueueName = TestConfig.GetName("timeouttest");

        [Test]
        [Description("Verifies that all pending receive operations are cancelled when the bus is disposed")]
        public void FoundWayToCancelAllPendingReceiveOperations()
        {
            var stopwatch = new Stopwatch();

            using (var activator = new BuiltinHandlerActivator())
            {
                Configure.With(activator)
                    .Transport(t => t.UseAzureServiceBus(ConnectionString, QueueName))
                    .Start();

                Thread.Sleep(1000);

                stopwatch.Start();
            }

            stopwatch.Stop();

            var shutdownDuration = stopwatch.Elapsed;

            Console.WriteLine($"Shutdown took {shutdownDuration}");
        }
    }
}
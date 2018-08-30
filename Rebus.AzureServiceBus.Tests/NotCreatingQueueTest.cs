using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Injection;
using Rebus.Tests;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class NotCreatingQueueTest : FixtureBase
    {
        [Test]
        public async Task ShouldNotCreateInputQueueWhenConfiguredNotTo()
        {
            var connectionString = AzureServiceBusTransportFactory.ConnectionString;
            var managementClient = new ManagementClient(connectionString);
            var queueName = Guid.NewGuid().ToString("N");

            Assert.IsFalse(await managementClient.QueueExistsAsync(queueName));

            var activator = Using(new BuiltinHandlerActivator());

            var exception = Assert.Throws<ResolutionException>(() =>
            {
                Configure.With(activator)
                    .Logging(l => l.ColoredConsole())
                    .Transport(t =>
                    {
                        t.UseAzureServiceBus(connectionString, queueName)
                            .DoNotCreateQueues();
                    })
                    .Start();
            });

            Console.WriteLine(exception);

            var exceptionMessage = exception.ToString();

            Assert.That(exceptionMessage, Contains.Substring(queueName), "The exception message did not contain the queue name");
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
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

            Configure.With(activator)
                .Logging(l => l.ColoredConsole())
                .Transport(t =>
                {
                    t.UseAzureServiceBus(connectionString, queueName)
                        .DoNotCreateQueues();
                })
                .Start();

            Assert.IsFalse(await managementClient.QueueExistsAsync(queueName));
        }
    }
}
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Extensions;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class SpikeTest : FixtureBase
    {
        static readonly string ConnectionString = StandardAzureServiceBusTransportFactory.ConnectionString;
        ManagementClient _managementClient;

        protected override void SetUp()
        {
            _managementClient = new ManagementClient(ConnectionString);
        }

        [Test]
        public async Task CanDeleteQueueThatDoesNotExist()
        {
            var queueName = TestConfig.GetName("delete");
            await _managementClient.DeleteQueueIfExistsAsync(queueName);
            Assert.That(await _managementClient.QueueExistsAsync(queueName), Is.False, $"The queue {queueName} still exists");
        }

        [Test]
        public async Task CanCreateQueue()
        {
            var queueName = TestConfig.GetName("create");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            Assert.That(await _managementClient.QueueExistsAsync(queueName), Is.True, 
                $"The queue {queueName} does not exist, even after several attempts at creating it");
        }
    }
}
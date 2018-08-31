using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Internals;
using Rebus.Tests.Contracts;
// ReSharper disable RedundantArgumentDefaultValue

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class SpikeTest : FixtureBase
    {
        static readonly string ConnectionString = AzureServiceBusTransportFactory.ConnectionString;
        ManagementClient _managementClient;

        protected override void SetUp()
        {
            _managementClient = new ManagementClient(ConnectionString);
        }

        [Test]
        [Description("Test doesn't work if the CreateQueueIfNotExistsAsync method is defective")]
        public async Task CanDeleteQueueThatDoesNotExist()
        {
            var queueName = TestConfig.GetName("delete");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            
            await _managementClient.DeleteQueueIfExistsAsync(queueName);
            
            Assert.That(await _managementClient.QueueExistsAsync(queueName), Is.False, $"The queue {queueName} still exists");
        }

        [Test]
        [Description("Test doesn't work if the DeleteQueueIfExistsAsync method is defective")]
        public async Task CanCreateQueue()
        {
            var queueName = TestConfig.GetName("create");
            await _managementClient.DeleteQueueIfExistsAsync(queueName);

            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);

            Assert.That(await _managementClient.QueueExistsAsync(queueName), Is.True, $"The queue {queueName} does not exist, even after several attempts at creating it");
        }

        [Test]
        public async Task CanPurgeQueue()
        {
            var queueName = TestConfig.GetName("send");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);

            var retryPolicy = new RetryExponential(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), 10);
            var queueClient = new QueueClient(ConnectionString, queueName, receiveMode: ReceiveMode.PeekLock, retryPolicy: retryPolicy);

            await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes("Hej med dig min ven!")));
            await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes("Hej med dig min ven!")));
            await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes("Hej med dig min ven!")));
            await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes("Hej med dig min ven!")));

            await ManagementExtensions.PurgeQueue(ConnectionString, queueName);

            var client = new QueueClient(ConnectionString, queueName);
            var somethingWasReceived = new ManualResetEvent(false);
            client.RegisterMessageHandler(async (_, __) => somethingWasReceived.Set(), async _ => somethingWasReceived.Set());

            Assert.That(somethingWasReceived.WaitOne(TimeSpan.FromSeconds(1)), Is.False, $"Looks like a message was received from the queue '{queueName}' even though it was purged :o");
        }

        [Test]
        public async Task CanSendAndReceiveMessage()
        {
            var queueName = TestConfig.GetName("send-receive");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);

            var retryPolicy = new RetryExponential(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), 10);
            var queueClient = new QueueClient(ConnectionString, queueName, receiveMode: ReceiveMode.PeekLock, retryPolicy: retryPolicy);

            await ManagementExtensions.PurgeQueue(ConnectionString, queueName);

            await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes("Hej med dig min ven! Det spiller!")));

            var messageReceiver = new MessageReceiver(ConnectionString, queueName, receiveMode: ReceiveMode.PeekLock);

            var message = await messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(2));

            Assert.That(message, Is.Not.Null);

            var text = Encoding.UTF8.GetString(message.Body);

            Assert.That(text, Is.EqualTo("Hej med dig min ven! Det spiller!"));
        }
    }
}
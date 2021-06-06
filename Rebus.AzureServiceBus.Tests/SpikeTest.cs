using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Internals;
using Rebus.Tests.Contracts;
// ReSharper disable RedundantArgumentDefaultValue
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class SpikeTest : FixtureBase
    {
        static readonly string ConnectionString = AsbTestConfig.ConnectionString;
        ServiceBusAdministrationClient _managementClient;

        protected override void SetUp()
        {
            _managementClient = new ServiceBusAdministrationClient(ConnectionString);
        }

        [Test]
        [Description("Test doesn't work if the CreateQueueIfNotExistsAsync method is defective")]
        public async Task CanDeleteQueueThatDoesNotExist()
        {
            var queueName = TestConfig.GetName("delete");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);
            
            await _managementClient.DeleteQueueIfExistsAsync(queueName);
            
            Assert.That((await _managementClient.QueueExistsAsync(queueName)).Value, Is.False, $"The queue {queueName} still exists");
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

            Assert.That((await _managementClient.QueueExistsAsync(queueName)).Value, Is.True, $"The queue {queueName} does not exist, even after several attempts at creating it");
        }

        [Test]
        public async Task CanPurgeQueue()
        {
            var queueName = TestConfig.GetName("send");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);

            var clientOptions = new ServiceBusClientOptions
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = ServiceBusRetryMode.Exponential,
                    Delay = TimeSpan.FromMilliseconds(100),
                    MaxDelay = TimeSpan.FromSeconds(5),
                    MaxRetries = 10
                }
            };
            var processorOptions = new ServiceBusProcessorOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };
            var client = new ServiceBusClient(ConnectionString, clientOptions);

            var queueSender = client.CreateSender(queueName);

            await queueSender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("Hej med dig min ven!")));
            await queueSender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("Hej med dig min ven!")));
            await queueSender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("Hej med dig min ven!")));
            await queueSender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("Hej med dig min ven!")));

            await ManagementExtensions.PurgeQueue(ConnectionString, queueName);

            var queueProcessor = client.CreateProcessor(queueName, processorOptions);
            var somethingWasReceived = new ManualResetEvent(false);
            queueProcessor.ProcessMessageAsync += _ => Task.FromResult(somethingWasReceived.Set());

            Assert.That(somethingWasReceived.WaitOne(TimeSpan.FromSeconds(1)), Is.False, $"Looks like a message was received from the queue '{queueName}' even though it was purged :o");
        }

        [Test]
        public async Task CanSendAndReceiveMessage()
        {
            var queueName = TestConfig.GetName("send-receive");
            await _managementClient.CreateQueueIfNotExistsAsync(queueName);

            var clientOptions = new ServiceBusClientOptions
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = ServiceBusRetryMode.Exponential,
                    Delay = TimeSpan.FromMilliseconds(100),
                    MaxDelay = TimeSpan.FromSeconds(5),
                    MaxRetries = 10
                }
            };
            var receiverOptions = new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };
            var client = new ServiceBusClient(ConnectionString, clientOptions);

            var queueSender = client.CreateSender(queueName);

            await ManagementExtensions.PurgeQueue(ConnectionString, queueName);

            await queueSender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("Hej med dig min ven! Det spiller!")));

            var messageReceiver = client.CreateReceiver(queueName, receiverOptions);

            var message = await messageReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));

            Assert.That(message, Is.Not.Null);

            var text = Encoding.UTF8.GetString(message.Body);

            Assert.That(text, Is.EqualTo("Hej med dig min ven! Det spiller!"));
        }
    }
}
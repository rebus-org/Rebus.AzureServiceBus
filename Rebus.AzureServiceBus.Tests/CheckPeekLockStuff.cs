using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Internals;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class CheckPeekLockStuff : FixtureBase
    {
        ServiceBusAdministrationClient _managementClient;
        ServiceBusSender _messageSender;
        ServiceBusReceiver _messageReceiver;
        string _queueName;

        protected override void SetUp()
        {
            var connectionString = AsbTestConfig.ConnectionString;
            
            _queueName = TestConfig.GetName("test-queue");
            
            _managementClient = new ServiceBusAdministrationClient(connectionString);
            var client = new ServiceBusClient(connectionString);
            _messageSender = client.CreateSender(_queueName);
            _messageReceiver = client.CreateReceiver(_queueName);

            AsyncHelpers.RunSync(async () => await _managementClient.DeleteQueueIfExistsAsync(_queueName));
        }

        [Test]
        public async Task CanGrabPeekLock()
        {
            await _managementClient.CreateQueueAsync(_queueName);

            var messageId = Guid.NewGuid().ToString();

            await _messageSender.SendMessageAsync(new ServiceBusMessage
            {
                MessageId = messageId,
                Body = new BinaryData(new byte[] {1, 2, 3}.AsMemory())
            });

            var message = await _messageReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));

            Assert.That(message, Is.Not.Null);
            Assert.That(message.MessageId, Is.EqualTo(messageId));
            Assert.That(await _messageReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2)), Is.Null);

            var lockedUntilUtc = message.LockedUntil;

            Console.WriteLine($"The message is locked until {lockedUntilUtc} (message ID = {message.MessageId}, lock token = {message.LockToken})");

            await _messageReceiver.CompleteMessageAsync(message);

            Assert.That(await _messageReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2)), Is.Null);

            while (DateTime.UtcNow < lockedUntilUtc.AddSeconds(5))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            var otherMessage = await _messageReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));

            Assert.That(otherMessage, Is.Null, () => $"Got message at time {DateTime.UtcNow} (message ID = {otherMessage.MessageId}, lock token = {otherMessage.LockToken})");
        }
    }
}
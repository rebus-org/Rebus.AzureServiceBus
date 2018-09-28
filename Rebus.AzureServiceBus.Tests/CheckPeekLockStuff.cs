using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Internals;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class CheckPeekLockStuff : FixtureBase
    {
        ManagementClient _managementClient;
        MessageSender _messageSender;
        MessageReceiver _messageReceiver;
        string _queueName;

        protected override void SetUp()
        {
            var connectionString = AsbTestConfig.ConnectionString;
            
            _queueName = TestConfig.GetName("test-queue");
            
            _managementClient = new ManagementClient(connectionString);
            _messageSender = new MessageSender(connectionString, _queueName);
            _messageReceiver = new MessageReceiver(connectionString, _queueName);

            AsyncHelpers.RunSync(async () => await _managementClient.DeleteQueueIfExistsAsync(_queueName));
        }

        [Test]
        public async Task CanGrabPeekLock()
        {
            await _managementClient.CreateQueueAsync(_queueName);

            var messageId = Guid.NewGuid().ToString();

            await _messageSender.SendAsync(new Message
            {
                MessageId = messageId,
                Body = new byte[] {1, 2, 3}
            });

            var message = await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(2));

            Assert.That(message, Is.Not.Null);
            Assert.That(message.MessageId, Is.EqualTo(messageId));
            Assert.That(await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(2)), Is.Null);

            var lockedUntilUtc = message.SystemProperties.LockedUntilUtc;

            Console.WriteLine($"The message is locked until {lockedUntilUtc} (message ID = {message.MessageId}, lock token = {message.SystemProperties.LockToken})");

            await _messageReceiver.CompleteAsync(message.SystemProperties.LockToken);

            Assert.That(await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(2)), Is.Null);

            while (DateTime.UtcNow < lockedUntilUtc.AddSeconds(5))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            var otherMessage = await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(2));

            Assert.That(otherMessage, Is.Null, () => $"Got message at time {DateTime.UtcNow} (message ID = {otherMessage.MessageId}, lock token = {otherMessage.SystemProperties.LockToken})");
        }
    }
}
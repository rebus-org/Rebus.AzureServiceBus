using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Internals;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
public class CheckTheServiceBusMessageInTheTimeAfterCompletingIt : FixtureBase
{
    [Test]
    public async Task WhatIsTheState()
    {
        void PrintMessage(ServiceBusReceivedMessage msg)
        {
            var amqp = msg.GetRawAmqpMessage();

            Console.WriteLine($@"
  Message ID: {msg.MessageId}
  Lock Token: {msg.LockToken}
Locked until: {msg.LockedUntil}
       State: {msg.State}
  Expires at: {msg.ExpiresAt}

");
        }

        const string queueName = "test-queue";

        Using(new QueueDeleter(queueName));

        var connectionString = AsbTestConfig.ConnectionString;
        var admin = new ServiceBusAdministrationClient(connectionString);
        
        await admin.CreateQueueIfNotExistsAsync(queueName);

        var serviceBusClient = new ServiceBusClient(connectionString);

        var sender = serviceBusClient.CreateSender(queueName);
        var receiver = serviceBusClient.CreateReceiver(queueName);

        await sender.SendMessageAsync(new ServiceBusMessage("HEJ"));

        var roundtripped = await receiver.ReceiveMessageAsync();

        PrintMessage(roundtripped);

        await receiver.CompleteMessageAsync(roundtripped);

        PrintMessage(roundtripped);
    }
}
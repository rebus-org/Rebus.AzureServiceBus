using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class WorksWithAutoDeleteOnIdle : FixtureBase
    {
        [Test]
        [Ignore("takes a long time to execute")]
        public async Task AutomaticallyKeepsQueueAlive()
        {
            var client = new ManagementClient(AsbTestConfig.ConnectionString);
            var activator = Using(new BuiltinHandlerActivator());
            var gotTheString = Using(new ManualResetEvent(false));

            activator.Handle<string>(async message => gotTheString.Set());

            var queueName = $"auto-delete-test-{Guid.NewGuid()}";

            var bus = Configure.With(activator)
                .Transport(transport => transport
                    .UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName)
                    .SetAutoDeleteOnIdle(TimeSpan.FromMinutes(5)))
                .Start();

            // verify that queue exists
            var queueDescription = await client.GetQueueAsync(queueName);
            Assert.That(queueDescription.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.FromMinutes(5)));

            await Task.Delay(TimeSpan.FromMinutes(8));

            // verify that the queue still exists
            try
            {
                await client.GetQueueAsync(queueName);
            }
            catch (MessagingEntityNotFoundException exception)
            {
                throw new MessagingEntityNotFoundException($"Could not get queue description for '{queueName}' after 8 minutes of waiting!", exception);
            }

            // verify we can get the message
            await bus.SendLocal("HEJ MED DIG MIN VEN!!!!!!");
            gotTheString.WaitOrDie(TimeSpan.FromSeconds(5), errorMessage: "Did not receive the message within 5 s after sending it - was the queue deleted??");

            // dispose all the things
            CleanUpDisposables();

            await Task.Delay(TimeSpan.FromMinutes(8));

            var notFoundException = Assert.ThrowsAsync<MessagingEntityNotFoundException>(async () =>
            {
                await client.GetQueueAsync(queueName);
            });

            Console.WriteLine(notFoundException);
        }
    }
}
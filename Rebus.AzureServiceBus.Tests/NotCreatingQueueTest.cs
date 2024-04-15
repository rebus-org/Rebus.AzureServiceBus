using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Injection;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class NotCreatingQueueTest : FixtureBase
{
    [Test]
    public async Task ShouldNotCreateInputQueueWhenConfiguredNotTo()
    {
        var connectionString = AsbTestConfig.ConnectionString;
        var managementClient = new ServiceBusAdministrationClient(connectionString);
            
        var queueName = Guid.NewGuid().ToString("N");

        var response = await managementClient.QueueExistsAsync(queueName);
        Assert.That(response.Value, Is.False);

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
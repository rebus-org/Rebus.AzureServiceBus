using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class FailsWhenSendingToNonExistentQueue : FixtureBase
{
    static readonly string ConnectionString = AsbTestConfig.ConnectionString;

    [Test]
    public void YesItDoes()
    {
        Using(new QueueDeleter("bimmelim"));

        using var activator = new BuiltinHandlerActivator();

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(ConnectionString, "bimmelim"))
            .Start();

        Func<Task> action = async () =>
            await activator.Bus.Advanced.Routing.Send("yunoexist", "hej med dig min ven!");
        var exception = Assert.ThrowsAsync<RebusApplicationException>(action);

        Console.WriteLine(exception);

        var notFoundException = (ServiceBusException) exception.InnerException;

        Console.WriteLine(notFoundException);
    }
}
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
public class VerifyHowThingsWorkWhenConnectionStringEndpointHasPath
{
    [Test]
    public async Task ItWorks()
    {
        var name = TestConfig.GetName("namespace");
        var conn = new ConnectionStringParser(AsbTestConfig.ConnectionString);
        var newConn = new ConnectionStringParser($"{conn.Endpoint.TrimEnd('/')}/{name}", conn.SharedAccessKeyName, conn.SharedAccessKey, conn.EntityPath);

        using var activator = new BuiltinHandlerActivator();
            
        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(newConn.GetConnectionString(), "test-queue"))
            .Start();

        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task QueueWithSlash()
    {
        var name = TestConfig.GetName("namespace");

        using var activator = new BuiltinHandlerActivator();
            
        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, $"{name}/test-queue"))
            .Start();

        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
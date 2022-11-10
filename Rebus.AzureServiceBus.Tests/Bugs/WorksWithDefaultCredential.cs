using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
[Explicit("Requires bring logged in as an appropriate Azure user in Visual Studio")]
public class WorksWithDefaultCredential : FixtureBase
{
    static string EndpointOnlyConnectionString => $"Endpoint={AsbTestConfig.GetEndpointUriFromConnectionString()}";

    [Test]
    public async Task CanStartBusWithDefaultCredential()
    {
        using var activator = new BuiltinHandlerActivator();

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(EndpointOnlyConnectionString, "test-queue", GetDefaultAzureCredential()))
            .Start();

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task CheckRaw()
    {
        var credential = GetDefaultAzureCredential();

        var client = new ServiceBusAdministrationClient(AsbTestConfig.GetHostnameFromConnectionString(), credential);

        var queues = client.GetQueuesAsync();

        await foreach (var queue in queues)
        {
            Console.WriteLine(queue.Name);
        }
    }

    static DefaultAzureCredential GetDefaultAzureCredential() => new(new DefaultAzureCredentialOptions
    {
        Diagnostics =
        {
            LoggedHeaderNames = { "x-ms-request-id" },
            LoggedQueryParameters = { "api-version" },
            IsLoggingContentEnabled = true,
            IsAccountIdentifierLoggingEnabled = true,
            IsDistributedTracingEnabled = true,
            IsLoggingEnabled = true,
            IsTelemetryEnabled = true,
        },

        ExcludeAzureCliCredential = true,
        ExcludeAzurePowerShellCredential = true,
        ExcludeEnvironmentCredential = true,
        ExcludeInteractiveBrowserCredential = true,
        ExcludeVisualStudioCodeCredential = true,
        ExcludeSharedTokenCacheCredential = true,
        ExcludeManagedIdentityCredential = true,
    });
}
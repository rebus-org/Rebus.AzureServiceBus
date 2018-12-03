using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Config;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    [Description("BEcause inheritance collides with the builder pattern, we use this test fixture to ensure parity between AzureServiceBusTransportClientSettings and AzureServiceBusTransportSettings")]
    public class ServerSettingsBuilderHasSameSettingsAsClientBuilder
    {
        [Test]
        public void AllClientConfigurationOptionsAreAvailableOnServerToo()
        {
            var clientBuilder = typeof(AzureServiceBusTransportClientSettings);
            var serverBuilder = typeof(AzureServiceBusTransportSettings);

            foreach (var method in clientBuilder.GetMethods())
            {
                var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                var correspondingMethod = serverBuilder.GetMethod(method.Name, parameterTypes)
                    ?? throw new ArgumentException($@"Could not find configuration method on {serverBuilder} matching this signature:

    {method.Name}({string.Join(", ", parameterTypes.Select(type => type.Name))})

All methods present on AzureServiceBusTransportClientSettings must be available on AzureServiceBusTransportSettings as well.");
            }
        }
    }
}
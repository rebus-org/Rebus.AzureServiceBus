using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class PublishNativeHeadersOnly : FixtureBase
{
    [Test]
    public async Task ShouldNotPublishRebusHeadersWhenConfiguredNotTo()
    {
        var activator = Using(new BuiltinHandlerActivator());

        string queue = "publish-native";
        var starter = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queue).UseLegacyNaming().UseNativeHeaders())
            .Create();

        starter.Start();
        await starter.Bus.Publish("hello");
    }
}
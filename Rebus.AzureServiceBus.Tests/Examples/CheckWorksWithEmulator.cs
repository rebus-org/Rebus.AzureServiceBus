using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

// ReSharper disable AsyncMethodWithoutAwait
// ReSharper disable AccessToDisposedClosure

namespace Rebus.AzureServiceBus.Tests.Examples;

[TestFixture]
[Explicit("Requires that an emulator instance is running locally in a Docker container. It must have the queue 'queue.1' in its config.json")]
public class CheckWorksWithEmulator : FixtureBase
{
    const string ConnectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    [Test]
    public async Task ItSureDoes()
    {
        using var activator = new BuiltinHandlerActivator();
        using var gotTheMessage = new ManualResetEvent(initialState: false);

        activator.Handle<string>(async _ => gotTheMessage.Set());

        var bus = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(ConnectionString, "queue.1"))
            .Start();

        await bus.SendLocal("goddaw do");

        gotTheMessage.WaitOrDie(timeout: TimeSpan.FromSeconds(3));
    }
}
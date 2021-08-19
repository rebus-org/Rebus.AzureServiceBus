using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    [TestFixture]
    public class WorkWithIntegratedAuth : FixtureBase
    {
        [Test]
        [Explicit("run manually")]
        public async Task SureDoes()
        {
            var connectionString = AsbTestConfig.ConnectionString;

            using var activator = new BuiltinHandlerActivator();
            using var gotTheMessage = new ManualResetEvent(initialState: false);

            activator.Handle<string>(async _ => gotTheMessage.Set());

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(connectionString, "integrationtest"))
                .Start();

            await activator.Bus.SendLocal("HEJ 🙂");

            gotTheMessage.WaitOrDie(timeout: TimeSpan.FromSeconds(5), "Did not receive the string within 5 s");
        }
    }
}
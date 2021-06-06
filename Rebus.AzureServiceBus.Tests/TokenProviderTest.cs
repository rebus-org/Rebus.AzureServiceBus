using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    [Ignore("Requires some manual setup")]
    public class TokenProviderTest : FixtureBase
    {
        const string QueueName = "token-provider-server";

        BuiltinHandlerActivator _server;

        protected override void SetUp()
        {
            _server = new BuiltinHandlerActivator();

            Using(_server);

            Configure.With(_server)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, QueueName))
                .Start();

        }

        [Test]
        public async Task CanInitializeClientWithTokenProvider()
        {
            var gotTheString = new ManualResetEvent(false);

            _server.Handle<string>(async _ => gotTheString.Set());

            var configurer = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t
                    .UseAzureServiceBus("<insert connection string with endpoint only>", "<insert input queue>", new StubTokenCredential("<insert client ID>", "<insert client secret>", "<insert tenant ID>"))
                    .DoNotCheckQueueConfiguration()
                    .DoNotCreateQueues()
                )
                .Routing(r => r.TypeBased().Map<string>(QueueName));

            using (var client = configurer.Start())
            {
                await client.Send("HEJ MED DIG MIN VEN");
            }

            gotTheString.WaitOrDie(TimeSpan.FromSeconds(5));
        }
    }
}

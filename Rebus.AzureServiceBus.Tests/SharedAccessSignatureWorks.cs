using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    //[Ignore("Requires some manual setup")]
    public class SharedAccessSignatureWorks : FixtureBase
    {
        /// <summary>
        /// Intentionally configured to be a const, because the connection string grants access to this queue explicitly
        /// </summary>
        const string QueueName = "sastest";
        
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
        public async Task CanInitializeOneWayClientWithSasToken()
        {
            var gotTheString = new ManualResetEvent(false);

            _server.Handle<string>(async _ => gotTheString.Set());

            var configurer = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.UseAzureServiceBusAsOneWayClient("<insert sas connection string here with SEND claim for the 'sastest' queue>"))
                .Routing(r => r.TypeBased().Map<string>(QueueName));

            using (var client = configurer.Start())
            {
                await client.Send("HEJ MED DIG MIN VEN");
            }

            gotTheString.WaitOrDie(TimeSpan.FromSeconds(5));
        }
    }
}
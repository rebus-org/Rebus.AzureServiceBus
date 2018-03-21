using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    [Description("This is just a test to verify that the simplest scenario can work")]
    public class VerySimpleTest : FixtureBase
    {
        BuiltinHandlerActivator _server;
        IBus _client;

        protected override void SetUp()
        {
            var connectionString = StandardAzureServiceBusTransportFactory.ConnectionString;

            _server = new BuiltinHandlerActivator();

            Using(_server);

            Configure.With(_server)
                .Transport(t => t.UseAzureServiceBus(connectionString, "server"))
                .Start();

            _client = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.UseAzureServiceBusAsOneWayClient(connectionString))
                .Routing(r => r.TypeBased().Map<string>("server"))
                .Start();

            Using(_client);
        }

        [Test]
        public async Task ItWorks()
        {
            var gotMessage = new ManualResetEvent(false);

            _server.Handle<string>(async str => gotMessage.Set());

            await _client.Send("HEJ MED DIG");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(5), "Did not get the expected HEJ MED DIG message within 5 s");
        }
    }
}
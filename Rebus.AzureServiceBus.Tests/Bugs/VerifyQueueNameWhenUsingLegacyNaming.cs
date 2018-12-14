using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    [TestFixture]
    public class VerifyQueueNameWhenUsingLegacyNaming : FixtureBase
    {
        [TestCase("tester_input", false)]
        [TestCase("tester.input", false)]
        [TestCase("tester/input", false)]
        [TestCase("tester_input", true)]
        [TestCase("tester.input", true)]
        [TestCase("tester/input", true)]
        public async Task ItWorks(string queueName, bool useLegacyMode)
        {
            Console.WriteLine($"Checking that stuff works with queue name '{queueName}' and legacy mode = {useLegacyMode}");

            var gotTheMessage = new ManualResetEvent(false);
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            activator.Handle<string>(async message => gotTheMessage.Set());

            Configure.With(activator)
                .Transport(t =>
                {
                    var settings = t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName);

                    if (useLegacyMode)
                    {
                        settings.UseLegacyNaming();
                    }
                })
                .Start();

            await activator.Bus.Subscribe<string>();

            await activator.Bus.Publish("HEJ MED DIG MIN VEEEEN");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(3));
        }
    }
}
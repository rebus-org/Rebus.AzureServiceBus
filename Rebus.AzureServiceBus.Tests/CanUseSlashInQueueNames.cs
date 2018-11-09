using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class CanUseSlashInQueueNames : FixtureBase
    {
        [Test]
        public async Task ItJustWorks()
        {
            using (var activator = new BuiltinHandlerActivator())
            {
                var counter = new SharedCounter(2);
                var queueName = $"department/subdepartment/{TestConfig.GetName("slash")}";

                activator.Handle<string>(async _ => counter.Decrement());

                var bus = Configure.With(activator)
                    .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
                    .Start();

                await bus.Subscribe<string>();

                await bus.Publish("this message was published");
                await bus.SendLocal("this message was sent");

                counter.WaitForResetEvent();
            }
        }
    }
}
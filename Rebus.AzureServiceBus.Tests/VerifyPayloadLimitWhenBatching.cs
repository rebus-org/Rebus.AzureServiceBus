using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class VerifyPayloadLimitWhenBatching : FixtureBase
{
    [Test]
    public async Task DoesNotHitTheLimit()
    {
        var queueName = TestConfig.GetName("payload-limit");

        Using(new QueueDeleter(queueName));

        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<MessageWithText>(async _ => { });
            
        var bus = Configure.With(activator)
            .Logging(l => l.Console(LogLevel.Info))
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        using (var scope = new RebusTransactionScope())
        {
            var strings = Enumerable.Range(0, 1000)
                .Select(n => new MessageWithText($"message {n}"));

            await Task.WhenAll(strings.Select(str => bus.SendLocal(str)));

            await scope.CompleteAsync();
        }
    }

    record MessageWithText(string Text);
}
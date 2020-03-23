using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs
{
    [TestFixture]
    [Description("Verifies that a NULL value present in the headers dictionary => no trouble")]
    public class DoesNotDieOnHeaderNullValue : FixtureBase
    {
        [Test]
        public async Task NoItDoesNot()
        {
            var queueName = TestConfig.GetName("not-null");

            Using(new QueueDeleter(queueName));

            var headerKey = Guid.NewGuid().ToString("N");
            var gotTheMessageAndTheMessageWasGood = new ManualResetEvent(initialState: false);
            var activator = Using(new BuiltinHandlerActivator());

            activator.Handle<string>(async (bus, context, message) =>
            {
                var headers = context.Headers;

                if (headers.TryGetValue(headerKey, out var value)
                    && value == null)
                {
                    gotTheMessageAndTheMessageWasGood.Set();
                }
            });

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
                .Start();

            var problematicHeadersBecauseOfNullValue = new Dictionary<string, string>{{headerKey, null}};

            await activator.Bus.SendLocal("HEJ MED DIG", problematicHeadersBecauseOfNullValue);

            gotTheMessageAndTheMessageWasGood.WaitOrDie(timeout: TimeSpan.FromSeconds(3));
        }
    }
}
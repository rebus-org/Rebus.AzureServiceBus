using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class LegacyNamingTest : FixtureBase
    {
        [Test]
        public async Task CanReceiveFromPublisherWithLegacyModeEnabled()
        {
            var publisher = Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseAzureServiceBusAsOneWayClient(AsbTestConfig.ConnectionString).UseLegacyNaming())
                .Start();

            var subscriber = new BuiltinHandlerActivator();
            var gotTheEvent = new ManualResetEvent(false);

            subscriber.Handle<JustAnEvent>(async e => gotTheEvent.Set());

            Configure.With(Using(subscriber))
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "subscriber"))
                .Start();

            // simulate legacy style subscription
            await subscriber.Bus.Advanced.Topics.Subscribe(NormalizeLegacyStyle(typeof(JustAnEvent).GetSimpleAssemblyQualifiedName()));

            await publisher.Publish(new JustAnEvent());

            gotTheEvent.WaitOrDie(TimeSpan.FromSeconds(3));
        }

        [Test]
        public async Task CanPublishToSubscriberWithLegacyModeEnabled()
        {
            var publisher = Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseAzureServiceBusAsOneWayClient(AsbTestConfig.ConnectionString))
                .Start();

            var subscriber = new BuiltinHandlerActivator();
            var gotTheEvent = new ManualResetEvent(false);

            subscriber.Handle<JustAnEvent>(async e => gotTheEvent.Set());

            Configure.With(Using(subscriber))
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "subscriber").UseLegacyNaming())
                .Start();

            await subscriber.Bus.Subscribe<JustAnEvent>();

            // simulate legacy style publish
            await publisher.Advanced.Topics.Publish(NormalizeLegacyStyle(typeof(JustAnEvent).GetSimpleAssemblyQualifiedName()), new JustAnEvent());

            gotTheEvent.WaitOrDie(TimeSpan.FromSeconds(3));
        }

        static string NormalizeLegacyStyle(string topic)
        {
            return new string(topic.Select(c =>
                {
                    if (!char.IsLetterOrDigit(c)) return '_';

                    return char.ToLowerInvariant(c);
                })
                .ToArray());
        }

        class JustAnEvent { }
    }
}
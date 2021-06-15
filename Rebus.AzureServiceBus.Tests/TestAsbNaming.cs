using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class TestAsbNaming : FixtureBase
    {
        ServiceBusAdministrationClient _managementClient;
        string _endpoint;

        protected override void SetUp()
        {
            _managementClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);
            _endpoint = new ConnectionStringParser(AsbTestConfig.ConnectionString).Endpoint;
        }

        [Test]
        public async Task DefaultNaming()
        {
            Using(new QueueDeleter("group/some.inputqueue"));
            Using(new TopicDeleter("group/some.interesting_topic"));

            var activator = Using(new BuiltinHandlerActivator());
            var gotString1 = new ManualResetEvent(false);
            activator.Handle<object>(async str => gotString1.Set());

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue"))
                .Start();

            await activator.Bus.Advanced.Topics.Subscribe("group/" + "some.interesting topic");

            await Task.Delay(500);

            await activator.Bus.Advanced.Topics.Publish("group/" + "some.interesting topic", new object { });

            gotString1.WaitOrDie(TimeSpan.FromSeconds(4));

            Assert.IsTrue(await _managementClient.QueueExistsAsync("group/some.inputqueue"));
            Assert.IsTrue(await _managementClient.TopicExistsAsync("group/some.interesting_topic"));
            var subscription = await _managementClient.GetSubscriptionAsync("group/some.interesting_topic", "group_some.inputqueue");
            Assert.AreEqual(_endpoint + "/group/some.inputqueue", subscription.Value.ForwardTo);
        }

        [Test]
        public async Task DefaultTopicNameConvention()
        {
            Using(new QueueDeleter("group/some.inputqueue"));
            Using(new TopicDeleter("system.private.corelib/system.object"));

            var activator = Using(new BuiltinHandlerActivator());
            var gotString1 = new ManualResetEvent(false);
            activator.Handle<object>(async str => gotString1.Set());

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue"))
                .Start();

            await activator.Bus.Subscribe<object>();

            await Task.Delay(1000);

            await activator.Bus.Publish(new object { });

            gotString1.WaitOrDie(TimeSpan.FromSeconds(4));

            Assert.IsTrue(await _managementClient.QueueExistsAsync("group/some.inputqueue"));
            Assert.IsTrue(await _managementClient.TopicExistsAsync("system.private.corelib/system.object"));
            var subscription = await _managementClient.GetSubscriptionAsync("system.private.corelib/system.object", "group_some.inputqueue");
            Assert.AreEqual(_endpoint + "/group/some.inputqueue", subscription.Value.ForwardTo);
        }

        [Test]
        public async Task LegacyV604Naming()
        {
            Using(new QueueDeleter("group/some_inputqueue"));
            Using(new TopicDeleter("group/some_interesting_topic"));

            var activator = Using(new BuiltinHandlerActivator());
            var gotString1 = new ManualResetEvent(false);
            activator.Handle<object>(async str => gotString1.Set());

            Configure.With(activator)
                .Transport(t => t
                    .UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue")
                    .UseLegacyNaming())
                .Start();

            await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

            await Task.Delay(500);

            await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new object { });

            gotString1.WaitOrDie(TimeSpan.FromSeconds(4));

            Assert.IsTrue(await _managementClient.QueueExistsAsync("group/some_inputqueue"));
            Assert.IsTrue(await _managementClient.TopicExistsAsync("group/some_interesting_topic"));
            var subscription = await _managementClient.GetSubscriptionAsync("group/some_interesting_topic", "some_inputqueue");
            Assert.AreEqual(_endpoint + "/group/some_inputqueue", subscription.Value.ForwardTo);
        }

        [Test]
        public async Task LegacyV604TopicNameConvention()
        {
            Using(new QueueDeleter("group/some_inputqueue"));
            Using(new TopicDeleter("system_object__mscorlib"));

            var activator = Using(new BuiltinHandlerActivator());
            var gotString1 = new ManualResetEvent(false);
            activator.Handle<object>(async str => gotString1.Set());

            Configure.With(activator)
                .Transport(t => t
                    .UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue")
                    .UseLegacyNaming())
                .Start();

            await activator.Bus.Subscribe<object>();

            await Task.Delay(500);

            await activator.Bus.Publish(new object { });

            gotString1.WaitOrDie(TimeSpan.FromSeconds(5));

            Assert.IsTrue(await _managementClient.QueueExistsAsync("group/some_inputqueue"));
            Assert.IsTrue(await _managementClient.TopicExistsAsync("system_object__mscorlib"));
            var subscription = await _managementClient.GetSubscriptionAsync("system_object__mscorlib", "some_inputqueue");
            Assert.AreEqual(_endpoint + "/group/some_inputqueue", subscription.Value.ForwardTo);
        }

        [Test]
        public async Task LegacyV3Naming()
        {
            Using(new QueueDeleter("group/some.inputqueue"));
            Using(new TopicDeleter("group/some_interesting_topic"));

            var activator = Using(new BuiltinHandlerActivator());
            var gotString1 = new ManualResetEvent(false);
            activator.Handle<object>(async str => gotString1.Set());

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue"))
                .Options(c =>
                {
                    c.Decorate<INameFormatter>(r => new LegacyV3NameFormatter());
                })
                .Start();

            await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

            await Task.Delay(500);

            await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new object { });

            gotString1.WaitOrDie(TimeSpan.FromSeconds(8));

            Assert.IsTrue(await _managementClient.QueueExistsAsync("group/some.inputqueue"));
            Assert.IsTrue(await _managementClient.TopicExistsAsync("group/some_interesting_topic"));
            var subscription = await _managementClient.GetSubscriptionAsync("group/some_interesting_topic", "some_inputqueue");
            Assert.AreEqual(_endpoint + "/group/some.inputqueue", subscription.Value.ForwardTo);
        }

        [Test]
        public async Task PrefixNaming()
        {
            Using(new QueueDeleter("prefix/group/some.inputqueue"));
            Using(new TopicDeleter("prefix/group/some_interesting_topic"));

            var activator = Using(new BuiltinHandlerActivator());
            var gotString1 = new ManualResetEvent(false);
            activator.Handle<object>(async str => gotString1.Set());

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue").UseLegacyNaming())
                .Options(c => c.Decorate<INameFormatter>(r => new PrefixNameFormatter("prefix/", new LegacyV3NameFormatter())))
                .Start();

            await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

            await Task.Delay(500);

            await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new object { });

            gotString1.WaitOrDie(TimeSpan.FromSeconds(4));

            Assert.IsTrue(await _managementClient.QueueExistsAsync("prefix/group/some.inputqueue"));
            Assert.IsTrue(await _managementClient.TopicExistsAsync("prefix/group/some_interesting_topic"));
            var subscription = await _managementClient.GetSubscriptionAsync("prefix/group/some_interesting_topic", "some_inputqueue");
            Assert.AreEqual(_endpoint + "/prefix/group/some.inputqueue", subscription.Value.ForwardTo);
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class TestAsbTopicsPubSub : FixtureBase
    {
	    [Test]
	    public async Task PubSubSeemsToWork()
	    {
		    var activator = Using(new BuiltinHandlerActivator());
		    Configure.With(activator)
			    .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "some.inputqueue")
                    .UseLegacyNaming()
                )
                .Options(c =>
                {
                    c.Decorate<INameFormatter>(r =>
                    {
                        return new PrefixNameFormatter("prefix/", new LegacyV3NameFormatter());
                    });
                })
			    .Start();

		    var gotString1 = new ManualResetEvent(false);

		    activator.Handle<object>(async str => gotString1.Set());

		    // await activator.Bus.Advanced.Topics.Subscribe("group/" + "some.interesting topic");
            await activator.Bus.Subscribe<object>();

		    await Task.Delay(500);

		    // await activator.Bus.Advanced.Topics.Publish("group/" + "some.interesting topic", new object{ });
            await activator.Bus.Publish(new object { });

		    gotString1.WaitOrDie(TimeSpan.FromSeconds(2));
	    }
    }

    //[TestFixture]
    //public class TestAsbTopicsPubSub : FixtureBase
    //{
    //    readonly string _inputQueueName1 = TestConfig.GetName("pubsub1");
    //    readonly string _inputQueueName2 = TestConfig.GetName("pubsub2");
    //    readonly string _inputQueueName3 = TestConfig.GetName("pubsub3");
    //    readonly string _connectionString = AsbTestConfig.ConnectionString;
        
    //    BuiltinHandlerActivator _bus1;
    //    BuiltinHandlerActivator _bus2;
    //    BuiltinHandlerActivator _bus3;

    //    protected override void SetUp()
    //    {
    //        Using(new TopicDeleter(new AzureServiceBusNameHelper().GetTopic(typeof(string))));

    //        _bus1 = StartBus(_inputQueueName1);
    //        _bus2 = StartBus(_inputQueueName2);
    //        _bus3 = StartBus(_inputQueueName3);
    //    }

    //    [Test]
    //    public async Task PubSubSeemsToWork()
    //    {
    //        var gotString1 = new ManualResetEvent(false);
    //        var gotString2 = new ManualResetEvent(false);

    //        _bus1.Handle<string>(async str => gotString1.Set());
    //        _bus2.Handle<string>(async str => gotString2.Set());

    //        await _bus1.Bus.Subscribe<string>();
    //        await _bus2.Bus.Subscribe<string>();

    //        await Task.Delay(500);

    //        await _bus3.Bus.Publish("hello there!!!!");

    //        gotString1.WaitOrDie(TimeSpan.FromSeconds(3));
    //        gotString2.WaitOrDie(TimeSpan.FromSeconds(3));
    //    }

    //    [Test]
    //    public async Task PubSubSeemsToWorkAlsoWithUnsubscribe()
    //    {
    //        var gotString1 = new ManualResetEvent(false);
    //        var subscriber2GotTheMessage = false;

    //        _bus1.Handle<string>(async str => gotString1.Set());

    //        _bus2.Handle<string>(async str =>
    //        {
    //            subscriber2GotTheMessage = true;
    //        });

    //        await _bus1.Bus.Subscribe<string>();
    //        await _bus2.Bus.Subscribe<string>();

    //        await Task.Delay(500);

    //        await _bus2.Bus.Unsubscribe<string>();

    //        await Task.Delay(500);

    //        await _bus3.Bus.Publish("hello there!!!!");

    //        gotString1.WaitOrDie(TimeSpan.FromSeconds(3));

    //        Assert.That(subscriber2GotTheMessage, Is.False, "Didn't expect subscriber 2 to get the string since it was unsubscribed");
    //    }

    //    BuiltinHandlerActivator StartBus(string inputQueue)
    //    {
    //        var bus = Using(new BuiltinHandlerActivator());

    //        Configure.With(bus)
    //            .Transport(t => t.UseAzureServiceBus(_connectionString, inputQueue))
    //            .Start();

    //        return bus;
    //    }
    //}
}
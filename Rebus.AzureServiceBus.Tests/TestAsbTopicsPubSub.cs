﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class TestAsbTopicsPubSub : FixtureBase
{
    readonly string _inputQueueName1 = TestConfig.GetName("pubsub1");
    readonly string _inputQueueName2 = TestConfig.GetName("pubsub2");
    readonly string _inputQueueName3 = TestConfig.GetName("pubsub3");
    readonly string _connectionString = AsbTestConfig.ConnectionString;

    BuiltinHandlerActivator _bus1;
    IBusStarter _bus1Starter;
    BuiltinHandlerActivator _bus2;
    IBusStarter _bus2Starter;
    BuiltinHandlerActivator _bus3;
    IBusStarter _bus3Starter;

    protected override void SetUp()
    {
        Using(new TopicDeleter(new DefaultAzureServiceBusTopicNameConvention().GetTopic(typeof(string))));

        (_bus1, _bus1Starter) = CreateBus(_inputQueueName1);
        (_bus2, _bus2Starter) = CreateBus(_inputQueueName2);
        (_bus3, _bus3Starter) = CreateBus(_inputQueueName3);
    }

    void StartBuses()
    {
        _bus1Starter.Start();
        _bus2Starter.Start();
        _bus3Starter.Start();
    }

    [Test]
    public async Task PubSubSeemsToWork()
    {
        var gotString1 = new ManualResetEvent(false);
        var gotString2 = new ManualResetEvent(false);

        _bus1.Handle<string>(async str => gotString1.Set());
        _bus2.Handle<string>(async str => gotString2.Set());

        StartBuses();

        await _bus1.Bus.Subscribe<string>();
        await _bus2.Bus.Subscribe<string>();

        await Task.Delay(500);

        await _bus3.Bus.Publish("hello there!!!!");

        gotString1.WaitOrDie(TimeSpan.FromSeconds(3));
        gotString2.WaitOrDie(TimeSpan.FromSeconds(3));
    }
    
    [Test]
    public async Task PubSubMultipleTypesSeemsToWork()
    {
        var gotString1 = new ManualResetEvent(false);
        var gotString2 = new ManualResetEvent(false);

        _bus2.Handle<string>(async str => gotString1.Set());
        _bus2.Handle<int>(async str => gotString2.Set());

        StartBuses();

        await _bus2.Bus.Subscribe<string>();
        await _bus2.Bus.Subscribe<int>();

        await Task.Delay(500);

        await _bus3.Bus.Publish("hello there!!!!");
        await _bus3.Bus.Publish(3);

        gotString1.WaitOrDie(TimeSpan.FromSeconds(3));
        gotString2.WaitOrDie(TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task PubSubSeemsToWorkAlsoWithUnsubscribe()
    {
        var gotString1 = new ManualResetEvent(false);
        var subscriber2GotTheMessage = false;

        _bus1.Handle<string>(async str => gotString1.Set());

        _bus2.Handle<string>(async str =>
        {
            subscriber2GotTheMessage = true;
        });

        StartBuses();

        await _bus1.Bus.Subscribe<string>();
        await _bus2.Bus.Subscribe<string>();

        await Task.Delay(500);

        await _bus2.Bus.Unsubscribe<string>();

        await Task.Delay(500);

        await _bus3.Bus.Publish("hello there!!!!");

        gotString1.WaitOrDie(TimeSpan.FromSeconds(3));

        Assert.That(subscriber2GotTheMessage, Is.False, "Didn't expect subscriber 2 to get the string since it was unsubscribed");
    }
    
    [Test]
    public async Task PubNoSubSeemsNoMessage()
    {
        var gotString1 = new ManualResetEvent(false);
        _bus1.Handle<int>(async str => gotString1.Set());
        StartBuses();

        await _bus1.Bus.Subscribe<string>();
        await Task.Delay(500);

        await _bus3.Bus.Publish(24);

        Assert.False(gotString1.WaitOne(TimeSpan.FromSeconds(3)));
    }

    (BuiltinHandlerActivator, IBusStarter) CreateBus(string inputQueue)
    {
        var activator = Using(new BuiltinHandlerActivator());

        var starter = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(_connectionString, inputQueue))
            .Create();

        return (activator, starter);
    }
}
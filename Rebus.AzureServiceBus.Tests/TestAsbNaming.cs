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

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class TestAsbNaming : FixtureBase
{
    static readonly TimeSpan WaitOrDieTimeout = TimeSpan.FromSeconds(12);
    
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
        activator.Handle<AsbNamingEvent>(async evt => gotString1.Set());

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue"))
            .Start();

        await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

        await Task.Delay(500);

        await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new AsbNamingEvent());

        gotString1.WaitOrDie(WaitOrDieTimeout);

        Assert.That((await _managementClient.QueueExistsAsync("group/some.inputqueue")).Value, Is.True);
        Assert.That((await _managementClient.TopicExistsAsync("group/some.interesting_topic")).Value, Is.True);
        var subscription = await _managementClient.GetSubscriptionAsync("group/some.interesting_topic", "group_some.inputqueue");

        var expected = $"{_endpoint}/group/some.inputqueue";
        var actual = subscription.Value.ForwardTo;

        AssertUris(expected, actual);
    }

    [Test]
    public async Task DefaultTopicNameConvention()
    {
        Using(new QueueDeleter("group/some.inputqueue"));
        Using(new TopicDeleter("rebus.azureservicebus.tests/rebus.azureservicebus.tests.asbnamingevent"));

        var activator = Using(new BuiltinHandlerActivator());
        var gotString1 = new ManualResetEvent(false);
        activator.Handle<AsbNamingEvent>(async evt => gotString1.Set());

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue"))
            .Start();

        await activator.Bus.Subscribe<AsbNamingEvent>();

        await Task.Delay(1000);

        await activator.Bus.Publish(new AsbNamingEvent());

        gotString1.WaitOrDie(WaitOrDieTimeout);

        Assert.That((await _managementClient.QueueExistsAsync("group/some.inputqueue")).Value, Is.True);
        Assert.That((await _managementClient.TopicExistsAsync("rebus.azureservicebus.tests/rebus.azureservicebus.tests.asbnamingevent")).Value, Is.True);
        var subscription = await _managementClient.GetSubscriptionAsync("rebus.azureservicebus.tests/rebus.azureservicebus.tests.asbnamingevent", "group_some.inputqueue");

        //Assert.AreEqual(_endpoint + "/group/some.inputqueue", subscription.Value.ForwardTo);

        var expected = $"{_endpoint}/group/some.inputqueue";
        var actual = subscription.Value.ForwardTo;

        AssertUris(expected, actual);
    }

    [Test]
    public async Task LegacyV604Naming()
    {
        Using(new QueueDeleter("group/some_inputqueue"));
        Using(new TopicDeleter("group/some_interesting_topic"));

        var activator = Using(new BuiltinHandlerActivator());
        var gotString1 = new ManualResetEvent(false);
        activator.Handle<AsbNamingEvent>(async evt => gotString1.Set());

        Configure.With(activator)
            .Transport(t => t
                .UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue")
                .UseLegacyNaming())
            .Start();

        await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

        await Task.Delay(500);

        await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new AsbNamingEvent());

        gotString1.WaitOrDie(WaitOrDieTimeout);

        Assert.That((await _managementClient.QueueExistsAsync("group/some_inputqueue")).Value, Is.True);
        Assert.That((await _managementClient.TopicExistsAsync("group/some_interesting_topic")).Value, Is.True);
        var subscription = await _managementClient.GetSubscriptionAsync("group/some_interesting_topic", "some_inputqueue");

        //Assert.AreEqual(_endpoint + "/group/some_inputqueue", subscription.Value.ForwardTo);
        var expected = $"{_endpoint}/group/some_inputqueue";
        var actual = subscription.Value.ForwardTo;

        AssertUris(expected, actual);
    }

    [Test]
    public async Task LegacyV604TopicNameConvention()
    {
        Using(new QueueDeleter("group/some_inputqueue"));
        Using(new TopicDeleter("rebus_azureservicebus_tests_asbnamingevent__rebus_azureservicebus_tests"));

        var activator = Using(new BuiltinHandlerActivator());
        var gotString1 = new ManualResetEvent(false);
        activator.Handle<AsbNamingEvent>(async evt => gotString1.Set());

        Configure.With(activator)
            .Transport(t => t
                .UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue")
                .UseLegacyNaming())
            .Start();

        await activator.Bus.Subscribe<AsbNamingEvent>();

        await Task.Delay(500);

        await activator.Bus.Publish(new AsbNamingEvent());

        gotString1.WaitOrDie(WaitOrDieTimeout);

        Assert.That((await _managementClient.QueueExistsAsync("group/some_inputqueue")).Value, Is.True);
        Assert.That((await _managementClient.TopicExistsAsync("rebus_azureservicebus_tests_asbnamingevent__rebus_azureservicebus_tests")).Value, Is.True);
        var subscription = await _managementClient.GetSubscriptionAsync("rebus_azureservicebus_tests_asbnamingevent__rebus_azureservicebus_tests", "some_inputqueue");

        //Assert.AreEqual(_endpoint + "/group/some_inputqueue", subscription.Value.ForwardTo);
        var expected = $"{_endpoint}/group/some_inputqueue";
        var actual = subscription.Value.ForwardTo;

        AssertUris(expected, actual);
    }

    [Test]
    public async Task LegacyV3Naming()
    {
        Using(new QueueDeleter("group/some.inputqueue"));
        Using(new TopicDeleter("group/some_interesting_topic"));

        var activator = Using(new BuiltinHandlerActivator());
        var gotString1 = new ManualResetEvent(false);
        activator.Handle<AsbNamingEvent>(async evt => gotString1.Set());

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue"))
            .Options(c =>
            {
                c.Decorate<INameFormatter>(r => new LegacyV3NameFormatter());
            })
            .Start();

        await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

        await Task.Delay(500);

        await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new AsbNamingEvent());

        gotString1.WaitOrDie(WaitOrDieTimeout);

        Assert.That((await _managementClient.QueueExistsAsync("group/some.inputqueue")).Value, Is.True);
        Assert.That((await _managementClient.TopicExistsAsync("group/some_interesting_topic")).Value, Is.True);
        var subscription = await _managementClient.GetSubscriptionAsync("group/some_interesting_topic", "some_inputqueue");

        //Assert.AreEqual(_endpoint + "/group/some.inputqueue", subscription.Value.ForwardTo);
        var expected = $"{_endpoint}/group/some.inputqueue";
        var actual = subscription.Value.ForwardTo;

        AssertUris(expected, actual);
    }

    [Test]
    public async Task PrefixNaming()
    {
        Using(new QueueDeleter("prefix/group/some.inputqueue"));
        Using(new TopicDeleter("prefix/group/some_interesting_topic"));

        var activator = Using(new BuiltinHandlerActivator());
        var gotString1 = new ManualResetEvent(false);
        activator.Handle<AsbNamingEvent>(async evt => gotString1.Set());

        Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "group/some.inputqueue").UseLegacyNaming())
            .Options(c => c.Decorate<INameFormatter>(r => new PrefixNameFormatter("prefix/", new LegacyV3NameFormatter())))
            .Start();

        await activator.Bus.Advanced.Topics.Subscribe("group/some.interesting topic");

        await Task.Delay(500);

        await activator.Bus.Advanced.Topics.Publish("group/some.interesting topic", new AsbNamingEvent());

        gotString1.WaitOrDie(WaitOrDieTimeout);

        Assert.That((await _managementClient.QueueExistsAsync("prefix/group/some.inputqueue")).Value, Is.True);
        Assert.That((await _managementClient.TopicExistsAsync("prefix/group/some_interesting_topic")).Value, Is.True);
        var subscription = await _managementClient.GetSubscriptionAsync("prefix/group/some_interesting_topic", "some_inputqueue");
            
            
        //Assert.AreEqual(_endpoint + "/prefix/group/some.inputqueue", subscription.Value.ForwardTo);
        var expected = $"{_endpoint}/prefix/group/some.inputqueue";
        var actual = subscription.Value.ForwardTo;

        AssertUris(expected, actual);
    }

    static void AssertUris(string expected, string actual)
    {
        var expectedForwardUri = new Uri(expected);
        var actualForwardUri = new Uri(actual);
        Assert.That(actualForwardUri.Host, Is.EqualTo(expectedForwardUri.Host));
        Assert.That(actualForwardUri.PathAndQuery, Is.EqualTo(expectedForwardUri.PathAndQuery));
    }
}

record AsbNamingEvent;

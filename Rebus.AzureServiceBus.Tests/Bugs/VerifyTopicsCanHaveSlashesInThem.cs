using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
public class VerifyTopicsCanHaveSlashesInThem : FixtureBase
{
    [Test]
    public async Task ItsTrueTheyCan()
    {
        var queueName = TestConfig.GetName("topics-with-slashes");
        const string topicNameWithSlash = "primitives/string";

        Using(new TopicDeleter(topicNameWithSlash));
        Using(new QueueDeleter(queueName));

        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var gotTheString = new ManualResetEvent(false);

        activator.Handle<string>(async message =>
        {
            gotTheString.Set();
        });

        var bus = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        await bus.Advanced.Topics.Subscribe(topicNameWithSlash);

        await bus.Advanced.Topics.Publish(topicNameWithSlash, "WHOA DET VIRKER!!");

        gotTheString.WaitOrDie(TimeSpan.FromSeconds(5));



        // make a final verification: the topic has a / in it

        var managementClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);
        var topicDescription = await managementClient.GetTopicAsync(topicNameWithSlash);

        Assert.That(topicDescription.Value.Name, Contains.Substring(topicNameWithSlash));
    }
}
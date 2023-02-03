using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Internals;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
[Description("Just an example that shows that you can indeed use / in topic names")]
public class CanIndeedHaveTopicNameWithForwardSlash : FixtureBase
{
    const string QueueName = "MyServiceBus/MyConsumer";
    const string TopicName = "MyServiceBus/MyServiceBus.Messages.MySomethingSomethingMessage";

    protected override void SetUp()
    {
        base.SetUp();

        Using(new TopicDeleter(TopicName));
        Using(new QueueDeleter(QueueName));
    }

    [Test]
    public async Task LookItWorks()
    {
        var admin = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);
        
        await admin.CreateTopicAsync(TopicName);
        await admin.CreateQueueIfNotExistsAsync(QueueName);
    }
}
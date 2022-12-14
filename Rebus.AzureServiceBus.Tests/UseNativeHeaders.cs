using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Hypothesist;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class UseNativeHeaders : FixtureBase
{
    [Test]
    public async Task ShouldNotPublishRebusHeadersWhenConfiguredNotTo()
    {
        var hypothesis = Hypothesis
            .For<IMessageContext>()
            .Any(x => !((ServiceBusReceivedMessage)x.TransactionContext.Items["asb-message"]).ApplicationProperties.ContainsKey(Headers.MessageId));

        var activator = Using(new BuiltinHandlerActivator()
            .Handle<string>((_, c, _) => hypothesis.Test(c)));

        var starter = Configure.With(activator)
            .Transport(t => t
                .UseNativeHeaders()
                .UseAzureServiceBus(AsbTestConfig.ConnectionString, "publish-native"))
            .Start();

        await starter.Subscribe<string>();
        await starter.Publish("hello");

        await hypothesis.Validate(TimeSpan.FromSeconds(2));
    }
}
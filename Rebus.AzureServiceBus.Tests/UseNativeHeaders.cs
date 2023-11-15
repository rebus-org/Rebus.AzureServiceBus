using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Messages;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class UseNativeHeaders : FixtureBase
{
    [Test]
    public async Task ShouldNotPublishRebusHeadersWhenConfiguredNotTo()
    {
        var queueName = $"publish-native-{Guid.NewGuid():N}";

        Using(new QueueDeleter(queueName));

        using var activator = new BuiltinHandlerActivator();
        using var gotTheMessage = new ManualResetEvent(initialState: false);

        ServiceBusReceivedMessage receivedMessage = null;

        activator.Handle<string>(async (_, c, _) =>
        {
            receivedMessage = c.TransactionContext.Items.GetOrDefault("asb-message") as ServiceBusReceivedMessage;
            gotTheMessage.Set();
        });

        var bus = Configure.With(activator)
            .Transport(t => t
                .UseNativeHeaders()
                .UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        await bus.SendLocal("hello");

        gotTheMessage.WaitOrDie(timeout: TimeSpan.FromSeconds(5));

        Assert.That(receivedMessage, Is.Not.Null);

        var headersAlreadyPresentOnNativeAsbMessage = new[]
        {
            Headers.MessageId,
            Headers.CorrelationId,
            Headers.ContentType,
            ExtraHeaders.SessionId,
        };

        Assert.That(receivedMessage.ApplicationProperties.Keys.Intersect(headersAlreadyPresentOnNativeAsbMessage).Count(), Is.Zero,
            $@"Did not expect the ApplicationProperties dictionary on the ASB transport message to contain any of the following headers: 

{string.Join(Environment.NewLine, headersAlreadyPresentOnNativeAsbMessage.Select(header => $"    {header}"))}

but the following were present:

{string.Join(Environment.NewLine, receivedMessage.ApplicationProperties.Select(p => $"    {p.Key}"))}");
    }
}
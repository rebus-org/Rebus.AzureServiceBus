using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.AzureServiceBus.Tests.Checks;

[TestFixture]
public class CanDeferLocal : FixtureBase
{
    [Test]
    public async Task WhyWouldThatBeProblematic()
    {
        var queueName = TestConfig.GetName("my-queue");

        using var gotTheMessage = new ManualResetEvent(initialState: false);

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<TheMessage>(async _ => gotTheMessage.Set());

        var bus = Configure.With(activator)
            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
            .Start();

        await bus.DeferLocal(TimeSpan.FromSeconds(1), new TheMessage());

        gotTheMessage.WaitOrDie(timeout: TimeSpan.FromSeconds(5));
    }

    record TheMessage;
}
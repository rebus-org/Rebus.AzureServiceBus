using NUnit.Framework;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class AzureServiceBusBasicSendReceive : BasicSendReceive<AzureServiceBusTransportFactory>
{
    protected override TransportBehavior Behavior => new(ReturnsNullWhenQueueIsEmpty: true);
}
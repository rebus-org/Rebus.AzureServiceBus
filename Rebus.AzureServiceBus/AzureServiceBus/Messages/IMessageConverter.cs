using Azure.Messaging.ServiceBus;
using Rebus.Messages;

namespace Rebus.AzureServiceBus.Messages;

public interface IMessageConverter
{
    TransportMessage ToTransport(ServiceBusReceivedMessage message);
    ServiceBusMessage ToServiceBus(TransportMessage transportMessage);
}
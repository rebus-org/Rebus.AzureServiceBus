using Azure.Messaging.ServiceBus;
using Rebus.Messages;

namespace Rebus.AzureServiceBus.Messages;

internal class RemoveHeaders : IMessageConverter
{
    private readonly IMessageConverter _messageConverter;

    public RemoveHeaders(IMessageConverter messageConverter) => 
        _messageConverter = messageConverter;

    TransportMessage IMessageConverter.ToTransport(ServiceBusReceivedMessage message) =>
        _messageConverter.ToTransport(message);

    ServiceBusMessage IMessageConverter.ToServiceBus(TransportMessage transportMessage)
    {
        var m = _messageConverter.ToServiceBus(transportMessage);
        m.ApplicationProperties.Remove(Headers.MessageId);
        m.ApplicationProperties.Remove(Headers.CorrelationId);
        m.ApplicationProperties.Remove(Headers.ContentType);
        m.ApplicationProperties.Remove(ExtraHeaders.SessionId);

        return m;
    }
}
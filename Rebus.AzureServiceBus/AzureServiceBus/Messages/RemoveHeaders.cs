using System;
using Azure.Messaging.ServiceBus;
using Rebus.Messages;

namespace Rebus.AzureServiceBus.Messages;

class RemoveHeaders : IMessageConverter
{
    readonly IMessageConverter _messageConverter;

    public RemoveHeaders(IMessageConverter messageConverter) => _messageConverter = messageConverter ?? throw new ArgumentNullException(nameof(messageConverter));

    TransportMessage IMessageConverter.ToTransport(ServiceBusReceivedMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return _messageConverter.ToTransport(message);
    }

    ServiceBusMessage IMessageConverter.ToServiceBus(TransportMessage transportMessage)
    {
        if (transportMessage == null) throw new ArgumentNullException(nameof(transportMessage));
        
        var message = _messageConverter.ToServiceBus(transportMessage);
        var properties = message.ApplicationProperties;
        
        properties.Remove(Headers.MessageId);
        properties.Remove(Headers.CorrelationId);
        properties.Remove(Headers.ContentType);
        properties.Remove(ExtraHeaders.SessionId);

        return message;
    }
}
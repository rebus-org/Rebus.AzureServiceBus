using System;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Internals;
using Rebus.Messages;

namespace Rebus.AzureServiceBus.Messages;

internal class DefaultMessageConverter : IMessageConverter
{
    public TransportMessage ToTransport(ServiceBusReceivedMessage message)
    {
        var applicationProperties = message.ApplicationProperties;
        var headers = applicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
        headers[Headers.TimeToBeReceived] = message.TimeToLive.ToString();
        headers[Headers.ContentType] = message.ContentType;
        headers[Headers.CorrelationId] = message.CorrelationId;
        headers[Headers.MessageId] = message.MessageId;
        headers[ExtraHeaders.SessionId] = message.SessionId;

        return new TransportMessage(headers, message.Body.ToMemory().ToArray());
    }

    public ServiceBusMessage ToServiceBus(TransportMessage transportMessage)
    {
        var message = new ServiceBusMessage(transportMessage.Body);
        var headers = transportMessage.Headers.Clone();

        if (headers.TryGetValue(Headers.TimeToBeReceived, out var timeToBeReceivedStr))
        {
            var timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            message.TimeToLive = timeToBeReceived;
            headers.Remove(Headers.TimeToBeReceived);
        }

        if (headers.TryGetValue(Headers.DeferredUntil, out var deferUntilTime))
        {
            var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();
            message.ScheduledEnqueueTime = deferUntilDateTimeOffset;
            headers.Remove(Headers.DeferredUntil);
        }

        if (headers.TryGetValue(Headers.ContentType, out var contentType))
        {
            message.ContentType = contentType;
        }

        if (headers.TryGetValue(Headers.CorrelationId, out var correlationId))
        {
            message.CorrelationId = correlationId;
        }

        if (headers.TryGetValue(Headers.MessageId, out var messageId))
        {
            message.MessageId = messageId;
        }
        
        if (headers.TryGetValue(ExtraHeaders.SessionId, out var sessionId))
        {
            message.SessionId = sessionId;
        }

        message.Subject = transportMessage.GetMessageLabel();

        if (headers.TryGetValue(Headers.ErrorDetails, out var errorDetails))
        {
            // this particular header has a tendency to grow out of hand
            headers[Headers.ErrorDetails] = errorDetails.TrimTo(32000);
        }

        foreach (var kvp in headers)
        {
            message.ApplicationProperties[kvp.Key] = kvp.Value;
        }

        return message;
    }
}


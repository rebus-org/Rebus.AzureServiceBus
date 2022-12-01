using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Rebus.AzureServiceBus;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Internals;

internal static class ServiceBusMessageConvert
{
    private const string SessionIdHeader = "SessionId";

    public static TransportMessage ToTransportMessage(this ServiceBusReceivedMessage message)
    {
        var applicationProperties = message.ApplicationProperties;
        var headers = applicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
        headers[Headers.TimeToBeReceived] = message.TimeToLive.ToString();
        headers[Headers.DeferredUntil] = message.ScheduledEnqueueTime.ToString();
        headers[Headers.ContentType] = message.ContentType;
        headers[Headers.CorrelationId] = message.CorrelationId;
        headers[Headers.MessageId] = message.MessageId;
        headers[SessionIdHeader] = message.SessionId;

        return new TransportMessage(headers, message.Body.ToMemory().ToArray());
    }

    private static ServiceBusMessage ToServiceBusMessage(this OutgoingMessage outgoingMessage)
    {
        var transportMessage = outgoingMessage.TransportMessage;
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
        
        if (headers.TryGetValue(SessionIdHeader, out var sessionId))
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

    public static IEnumerable<ServiceBusMessage> ToServiceBusMessages(this IEnumerable<OutgoingMessage> m, bool remove) => 
        m.Select(ToServiceBusMessage).RemoveHeaders(remove);

    private static IEnumerable<ServiceBusMessage> RemoveHeaders(this IEnumerable<ServiceBusMessage> messages, bool remove) =>
        messages.Select(m =>
        {
            if (remove)
            {
                m.RemoveHeaders();
            }
            
            return m;
        });

    private static void RemoveHeaders(this ServiceBusMessage m)
    {
        m.ApplicationProperties.Remove(Headers.MessageId);
        m.ApplicationProperties.Remove(Headers.CorrelationId);
        m.ApplicationProperties.Remove(Headers.ContentType);
        m.ApplicationProperties.Remove(SessionIdHeader);
    }
}


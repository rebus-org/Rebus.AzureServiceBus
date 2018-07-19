using System;
using System.IO;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.AzureServiceBus
{
    static class MsgHelpers
    {
        //public static BrokeredMessage CreateBrokeredMessage(TransportMessage message)
        //{
        //    var headers = message.Headers.Clone();
        //    var brokeredMessage = new BrokeredMessage(new MemoryStream(message.Body), true);

        //    if (headers.TryGetValue(Headers.TimeToBeReceived, out var timeToBeReceivedStr))
        //    {
        //        timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
        //        var timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
        //        brokeredMessage.TimeToLive = timeToBeReceived;
        //    }

        //    if (headers.TryGetValue(Headers.DeferredUntil, out var deferUntilTime))
        //    {
        //        var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();
        //        brokeredMessage.ScheduledEnqueueTimeUtc = deferUntilDateTimeOffset.UtcDateTime;
        //        headers.Remove(Headers.DeferredUntil);
        //    }

        //    if (headers.TryGetValue(Headers.ContentType, out var contentType))
        //    {
        //        brokeredMessage.ContentType = contentType;
        //    }

        //    if (headers.TryGetValue(Headers.CorrelationId, out var correlationId))
        //    {
        //        brokeredMessage.CorrelationId = correlationId;
        //    }

        //    if (headers.TryGetValue(Headers.MessageId, out var messageId))
        //    {
        //        brokeredMessage.MessageId = messageId;
        //    }

        //    brokeredMessage.Label = message.GetMessageLabel();

        //    foreach (var kvp in headers)
        //    {
        //        brokeredMessage.Properties[kvp.Key] = PossiblyLimitLength(kvp.Value);
        //    }

        //    return brokeredMessage;
        //}

        static string PossiblyLimitLength(string str)
        {
            const int maxLengthPrettySafe = 16300;

            if (str.Length < maxLengthPrettySafe) return str;

            var firstPart = str.Substring(0, 8000);
            var lastPart = str.Substring(str.Length - 8000);

            return $"{firstPart} (... cut out because length exceeded {maxLengthPrettySafe} characters ...) {lastPart}";
        }
    }
}
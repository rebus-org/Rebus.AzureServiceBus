using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Messaging.ServiceBus;

namespace Rebus.Internals
{
    static class MessageExtensions
    {
        /// <summary>
        /// There's no way to get the actual size, because ASB puts all kinds of crap in the message too.
        /// We do our best here, and then we put in a little bit of headspace
        /// </summary>
        public static long EstimateSize(this ServiceBusMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return 2 * (message.Body.ToMemory().Length + message.ApplicationProperties.Sum(GetSize));
        }

        private static long GetSize(KeyValuePair<string, object> kvp) => Encoding.UTF8.GetByteCount(kvp.Key) + Encoding.UTF8.GetByteCount(kvp.Value?.ToString() ?? "");
    }
}
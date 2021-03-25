using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.ServiceBus;

namespace Rebus.Internals
{
    static class MessageExtensions
    {
        /// <summary>
        /// There's no way to get the actual size, because ASB puts all kinds of crap in the message too.
        /// We do our best here, and then we put in a little bit of headspace
        /// </summary>
        public static long EstimateSize(this Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            long GetSize(KeyValuePair<string, object> kvp) => Encoding.UTF8.GetByteCount(kvp.Key) +
                                                              Encoding.UTF8.GetByteCount(kvp.Value?.ToString() ?? "");

            return 2 * (message.Size + message.UserProperties.Sum(GetSize));
        }
    }
}
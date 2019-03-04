using System;
using System.Linq;

namespace Rebus.AzureServiceBus.NameFormat
{
    /// <summary>
    /// Formats the names according to how it was done since at least v3, up to v6.0.3.
    /// </summary>
    public class LegacyV3NameFormatter : INameFormatter
    {
        /// <summary>
        /// Formats the queue name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatQueueName(string queueName)
        {
            var name = string.Concat(queueName.Select(c => (char.IsLetterOrDigit(c) || c == '/' || c == '.') ? c : '_'));

            return name.ToLowerInvariant();
        }

        /// <summary>
        /// Formats the subscription name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatSubscriptionName(string subscriptionName)
        {
            var idx = subscriptionName.LastIndexOf("/", StringComparison.Ordinal) + 1;
            subscriptionName = subscriptionName.Substring(idx);

            subscriptionName = string.Concat(subscriptionName.Select(c => (char.IsLetterOrDigit(c) || c == '/') ? c : '_'));

            subscriptionName = subscriptionName.ToLowerInvariant();

            return subscriptionName;
        }

        /// <summary>
        /// Formats the topic name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatTopicName(string topicName)
        {
            var name = string.Concat(topicName.Select(c => char.IsLetterOrDigit(c) || c == '/' ? c : '_'));

            return name.ToLowerInvariant();
        }
    }
}

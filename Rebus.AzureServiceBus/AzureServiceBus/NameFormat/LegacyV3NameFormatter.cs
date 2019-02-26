using System;
using System.Linq;

namespace Rebus.AzureServiceBus.NameFormat
{
    public class LegacyV3NameFormatter : INameFormatter
    {
        public string FormatQueueName(string queueName)
        {
            var name = string.Concat(queueName.Select(c =>
            {
                return (char.IsLetterOrDigit(c) || c == '/' || c == '.') ? c : '_';
            }));

            return name.ToLowerInvariant();
        }

        public string FormatSubscriptionName(string subscriptionName)
        {
            var idx = subscriptionName.LastIndexOf("/", StringComparison.Ordinal) + 1;
            subscriptionName = subscriptionName.Substring(idx);

            subscriptionName = string.Concat(subscriptionName.Select(c =>
            {
                return (char.IsLetterOrDigit(c) || c == '/') ? c : '_';
            }));

            subscriptionName = subscriptionName.ToLowerInvariant();

            return subscriptionName;
        }

        public string FormatTopicName(string topicName)
        {
            var name = string.Concat(topicName.Select(c =>
            {
                return (char.IsLetterOrDigit(c) || c == '/') ? c : '_';
            }));

            return name.ToLowerInvariant();
        }
    }
}

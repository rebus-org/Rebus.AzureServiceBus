using System;
using System.Linq;

namespace Rebus.AzureServiceBus.NameFormat
{
    /// <summary>
    /// A formatter that formats queue, topic and subscription names into the Legacy v6 format.
    /// </summary>
    public class LegacyNameFormatter : INameFormatter
    {
        readonly char[] _additionalValidCharacters;

        /// <summary>
        /// Creates the formatter.
        /// </summary>
        public LegacyNameFormatter()
        {
            _additionalValidCharacters = new[] { '_' };
        }

        private string ReplaceInvalidCharacters(string str)
        {
            var name = string.Concat(str.Select(c =>
            {
                if (c == '/') return '/';

                return IsValidCharacter(c) ? c : '_';
            }));

            return name.ToLowerInvariant();
        }

        bool IsValidCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || _additionalValidCharacters.Contains(c);
        }

        public string FormatQueueName(string queueName)
        {
            return ReplaceInvalidCharacters(queueName);
        }

        public string FormatTopicName(string topicName)
        {
            return ReplaceInvalidCharacters(topicName);
        }

        public string FormatSubscriptionName(string subscriptionName)
        {
            var idx = subscriptionName.LastIndexOf("/", StringComparison.Ordinal) + 1;
            subscriptionName = subscriptionName.Substring(idx);
            return ReplaceInvalidCharacters(subscriptionName);
        }
    }
}
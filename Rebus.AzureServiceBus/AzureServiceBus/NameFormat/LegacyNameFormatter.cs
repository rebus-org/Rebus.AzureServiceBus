using System;
using System.Linq;

namespace Rebus.AzureServiceBus.NameFormat
{
    /// <summary>
    /// Formats the names how it was done from v6.0.4 and higher.
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

        string ReplaceInvalidCharacters(string str)
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

        /// <summary>
        /// Formats the queue name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatQueueName(string queueName)
        {
            return ReplaceInvalidCharacters(queueName);
        }

        /// <summary>
        /// Formats the subscription name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatSubscriptionName(string subscriptionName)
        {
            var idx = subscriptionName.LastIndexOf("/", StringComparison.Ordinal) + 1;
            subscriptionName = subscriptionName.Substring(idx);
            return ReplaceInvalidCharacters(subscriptionName);
        }

        /// <summary>
        /// Formats the topic name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatTopicName(string topicName)
        {
            return ReplaceInvalidCharacters(topicName);
        }
    }
}
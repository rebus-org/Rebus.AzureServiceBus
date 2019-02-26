using System.Linq;

namespace Rebus.AzureServiceBus.NameFormat
{
    /// <summary>
    /// A formatter that formats queue, topic and subscription namest.
    /// </summary>
    public class DefaultNameFormatter : INameFormatter
    {
        readonly char[] _additionalValidCharacters;

        /// <summary>
        /// Creates the name helper, using legacy naming (more conservative escaping, lowercase throughout) if <paramref name="useLegacyNaming"/> is true
        /// </summary>
        public DefaultNameFormatter(bool useLegacyNaming = false)
        {
            _additionalValidCharacters = new[] { '.', '-', '_' };
        }

        private string ReplaceInvalidCharacters(string str)
        {
            var name = string.Concat(str.Select(c =>
            {
                if (c == '/') return '/';

                return IsValidCharacter(c) ? c : '_';
            }));

            return name;
        }

        bool IsValidCharacter(char c)
        {
            return char.IsLetterOrDigit(c)
                   || _additionalValidCharacters.Contains(c);
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
            // queue names can have multiple segments in them separated by / - subscription names cannot!
            return ReplaceInvalidCharacters(subscriptionName.Replace("/", "_"));
        }
    }
}
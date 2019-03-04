using System;
// ReSharper disable UnusedMember.Global

namespace Rebus.AzureServiceBus.NameFormat
{
    /// <summary>
    /// Formats all queue and topic names using a prefix.
    /// </summary>
    public class PrefixNameFormatter : INameFormatter
    {
        readonly INameFormatter _innerFormatter;
        readonly string _prefix;

        /// <summary>
        /// Creates the name formatter.
        /// </summary>
        public PrefixNameFormatter(string prefix) : this(prefix, new DefaultNameFormatter()) { }
                    
        /// <summary>
        /// Creates the name formatter using a specified inner name formatter.
        /// </summary>
        public PrefixNameFormatter(string prefix, INameFormatter innerFormatter)
        {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            _innerFormatter = innerFormatter ?? throw new ArgumentNullException(nameof(innerFormatter));
        }

        /// <summary>
        /// Formats the queue name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatQueueName(string queueName)
        {
            return _innerFormatter.FormatQueueName(_prefix + queueName);
        }

        /// <summary>
        /// Formats the subscription name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatSubscriptionName(string subscriptionName)
        {
            return _innerFormatter.FormatSubscriptionName(subscriptionName);
        }

        /// <summary>
        /// Formats the topic name into a usable name on ASB, normalizing if needed.
        /// </summary>
        public string FormatTopicName(string topicName)
        {
            return _innerFormatter.FormatTopicName(_prefix + topicName);
        }
    }
}

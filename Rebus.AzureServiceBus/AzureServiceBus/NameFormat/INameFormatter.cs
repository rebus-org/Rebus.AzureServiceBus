namespace Rebus.AzureServiceBus.NameFormat
{
    /// <summary>
    /// Formatter for queue, topic and subscription names on ASB.
    /// </summary>
    public interface INameFormatter
    {
        /// <summary>
        /// Formats the queue name into a usable name on ASB, normalizing if needed.
        /// </summary>
        string FormatQueueName(string queueName);

        /// <summary>
        /// Formats the subscription name into a usable name on ASB, normalizing if needed.
        /// </summary>
        string FormatSubscriptionName(string subscriptionName);

        /// <summary>
        /// Formats the topic name into a usable name on ASB, normalizing if needed.
        /// </summary>
        string FormatTopicName(string topicName);
    }
}

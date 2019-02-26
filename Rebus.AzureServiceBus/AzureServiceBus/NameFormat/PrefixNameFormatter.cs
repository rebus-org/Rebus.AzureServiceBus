namespace Rebus.AzureServiceBus.NameFormat
{
    public class PrefixNameFormatter : INameFormatter
    {
        private readonly string _prefix;
        private readonly INameFormatter _innerFormatter;

        public PrefixNameFormatter(string prefix, INameFormatter innerFormatter)
        {
            _prefix = prefix;
            _innerFormatter = innerFormatter;
        }

        public string FormatQueueName(string queueName)
        {
            return _innerFormatter.FormatQueueName(_prefix + queueName);
        }

        public string FormatSubscriptionName(string subscriptionName)
        {
            return _innerFormatter.FormatSubscriptionName(_prefix + subscriptionName);
        }

        public string FormatTopicName(string topicName)
        {
            return _innerFormatter.FormatTopicName(_prefix + topicName);
        }
    }
}

namespace Rebus.AzureServiceBus.Tests
{
    public class NamespaceManager
    {
        public static NamespaceManager CreateFromConnectionString(string connectionString)
        {
            return new NamespaceManager();
        }

        public bool QueueExists(string queueName)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteQueue(string queueName)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteTopic(string topic)
        {
            throw new System.NotImplementedException();
        }
    }
}
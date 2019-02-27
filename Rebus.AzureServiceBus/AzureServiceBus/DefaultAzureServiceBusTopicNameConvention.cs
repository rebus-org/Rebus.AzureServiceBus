using System;
using Rebus.Extensions;
using Rebus.Topic;

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Helper responsible for implementing how various names turn out
    /// </summary>
    public class DefaultAzureServiceBusTopicNameConvention : ITopicNameConvention
    {
        readonly bool _useLegacyNaming;

        /// <summary>
        /// Creates the name helper, using legacy topic naming if <paramref name="useLegacyNaming"/> is true.
        /// </summary>
        public DefaultAzureServiceBusTopicNameConvention(bool useLegacyNaming = false)
        {
            _useLegacyNaming = useLegacyNaming;
        }

        /// <summary>
        /// Gets a topic name from the given <paramref name="eventType"/>
        /// </summary>
        public string GetTopic(Type eventType)
        {
            string topicName = null;

            if (!_useLegacyNaming)
            {
                var assemblyName = eventType.Assembly.GetName().Name;
                var typeName = eventType.FullName;

                topicName = $"{assemblyName}/{typeName}";
            } 
            else 
            {
                var simpleAssemblyQualifiedName = eventType.GetSimpleAssemblyQualifiedName();

                topicName = simpleAssemblyQualifiedName;
            }

            return topicName;
        }
    }
}
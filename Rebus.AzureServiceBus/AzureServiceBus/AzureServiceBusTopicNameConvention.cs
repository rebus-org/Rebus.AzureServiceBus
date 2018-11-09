using System;
using Rebus.Extensions;
using Rebus.Topic;

namespace Rebus.AzureServiceBus
{
    class AzureServiceBusTopicNameConvention : ITopicNameConvention
    {
        readonly AzureServiceBusEntityNameHelper _azureServiceBusEntityNameHelper = new AzureServiceBusEntityNameHelper();
        
        public string GetTopic(Type eventType)
        {
            var simpleAssemblyQualifiedName = eventType.GetSimpleAssemblyQualifiedName();

            return _azureServiceBusEntityNameHelper.ReplaceInvalidCharacters(simpleAssemblyQualifiedName);
        }
    }
}
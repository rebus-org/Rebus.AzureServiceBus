using System;
using System.Collections.Generic;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class StandardAzureServiceBusTransportFactory : ITransportFactory
    {
        readonly Dictionary<string, AzureServiceBusTransport> _queuesToDelete = new Dictionary<string, AzureServiceBusTransport>();

        public ITransport CreateOneWayClient()
        {
            return Create(null);
        }

        public ITransport Create(string inputQueueAddress)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var asyncTaskFactory = new TplAsyncTaskFactory(consoleLoggerFactory);

            if (inputQueueAddress == null)
            {
                var transport = new AzureServiceBusTransport(AsbTestConfig.ConnectionString, null, consoleLoggerFactory, asyncTaskFactory);

                transport.Initialize();

                return transport;
            }

            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {
                var transport = new AzureServiceBusTransport(AsbTestConfig.ConnectionString, inputQueueAddress, consoleLoggerFactory, asyncTaskFactory);

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }

        public void CleanUp()
        {
            _queuesToDelete.Keys.ForEach(DeleteQueue);
        }

        public static void DeleteQueue(string queueName)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(AsbTestConfig.ConnectionString);

            if (!namespaceManager.QueueExists(queueName)) return;

            Console.Write("Deleting ASB queue {0}...", queueName);

            try
            {
                namespaceManager.DeleteQueue(queueName);
                Console.WriteLine("OK!");
            }
            catch (MessagingEntityNotFoundException)
            {
                Console.WriteLine("OK (was not there)");   
            }        }

        public static void DeleteTopic(string topic)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(AsbTestConfig.ConnectionString);

            try
            {
                Console.Write("Deleting topic '{0}' ...", topic);
                namespaceManager.DeleteTopic(topic);
                Console.WriteLine("OK!");
            }
            catch (MessagingEntityNotFoundException)
            {
                Console.WriteLine("OK! (wasn't even there)");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Rebus.AzureServiceBus.Tests.Extensions;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Internals;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class AzureServiceBusTransportFactory : ITransportFactory
    {
        public static string ConnectionString => ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asb_connection_string.txt"))
                                                 ?? ConnectionStringFromEnvironmentVariable("rebus2_asb_connection_string")
                                                 ?? Throw("Could not find Azure Service Bus connection string!");

        static string Throw(string message)
        {
            throw new ApplicationException(message);
        }

        static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using Azure Service Bus connection string from env variable {0}", environmentVariableName);

            return value;
        }

        static string ConnectionStringFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Azure Service Bus connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

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
                var transport = new AzureServiceBusTransport(ConnectionString, inputQueueAddress, consoleLoggerFactory);

                transport.Initialize();

                return transport;
            }

            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {
                var transport = new AzureServiceBusTransport(ConnectionString, inputQueueAddress, consoleLoggerFactory);

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
            var managementClient = new ManagementClient(ConnectionString);

            AsyncHelpers.RunSync(async () =>
            {
                try
                {
                    Console.Write("Deleting ASB queue {0}...", queueName);
                    await managementClient.DeleteQueueAsync(queueName);
                    Console.WriteLine("OK!");
                }
                catch (MessagingEntityNotFoundException)
                {
                    Console.WriteLine("OK (was not there)");
                }
            });
        }

        public static void DeleteTopic(string topic)
        {
            var managementClient = new ManagementClient(ConnectionString);

            AsyncHelpers.RunSync(async () =>
            {

                try
                {
                    Console.Write("Deleting topic '{0}' ...", topic);
                    await managementClient.DeleteTopicAsync(topic);
                    Console.WriteLine("OK!");
                }
                catch (MessagingEntityNotFoundException)
                {
                    Console.WriteLine("OK! (wasn't even there)");
                }
            });
        }
    }
}

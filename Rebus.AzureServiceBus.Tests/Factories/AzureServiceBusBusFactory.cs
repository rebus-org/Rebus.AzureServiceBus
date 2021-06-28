using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class AzureServiceBusBusFactory : IBusFactory
    {
        readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            var queueName = TestConfig.GetName(inputQueueAddress);

            PurgeQueue(queueName);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(10);
                    o.SetMaxParallelism(10);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        static void PurgeQueue(string queueName)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var asyncTaskFactory = new TplAsyncTaskFactory(consoleLoggerFactory);
            var connectionString = AsbTestConfig.ConnectionString;

            using (var transport = new AzureServiceBusTransport(connectionString, queueName, consoleLoggerFactory, asyncTaskFactory, new DefaultNameFormatter()))
            {
                transport.PurgeInputQueue();
            }
        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}
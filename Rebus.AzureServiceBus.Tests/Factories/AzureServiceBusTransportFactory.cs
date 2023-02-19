using System;
using System.Collections.Concurrent;
using Rebus.AzureServiceBus.Messages;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.AzureServiceBus.Tests.Bugs;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests.Factories;

public class AzureServiceBusTransportFactory : ITransportFactory
{
    readonly ConcurrentStack<IDisposable> _disposables = new();

    public ITransport CreateOneWayClient() => Create(null);

    public ITransport Create(string inputQueueAddress)
    {
        var consoleLoggerFactory = new ConsoleLoggerFactory(false);
        var asyncTaskFactory = new TplAsyncTaskFactory(consoleLoggerFactory);

        if (inputQueueAddress == null)
        {
            var onewayClientTransport = new AzureServiceBusTransport(AsbTestConfig.ConnectionString, null, consoleLoggerFactory, asyncTaskFactory, new DefaultNameFormatter(), new DefaultMessageConverter());

            onewayClientTransport.Initialize();

            _disposables.Push(onewayClientTransport);

            return onewayClientTransport;
        }

        _disposables.Push(new QueueDeleter(inputQueueAddress));

        var transport = new AzureServiceBusTransport(AsbTestConfig.ConnectionString, inputQueueAddress, consoleLoggerFactory, asyncTaskFactory, new DefaultNameFormatter(), new DefaultMessageConverter());

        transport.ReceiveOperationTimeout = TimeSpan.FromSeconds(5);

        transport.PurgeInputQueue();
        transport.Initialize();

        _disposables.Push(transport);

        return transport;
    }

    public void CleanUp()
    {
        while (_disposables.TryPop(out var disposable))
        {
            disposable.Dispose();
        }
    }
}
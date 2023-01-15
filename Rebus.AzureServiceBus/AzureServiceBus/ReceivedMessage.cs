using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Rebus.AzureServiceBus;

interface IReceivedMessage
{
    public ServiceBusReceivedMessage Message { get; }
    public Task CompleteAsync();
    public Task AbandonAsync(Dictionary<string, object> propertiesToModify);
    public Task DeadLetterAsync(Dictionary<string, object> propertiesToModify, string reason, string description);
}

public class ReceivedMessage : IReceivedMessage
{
    private readonly ProcessMessageEventArgs _processMessageEvent;
    private readonly CancellationToken _token;
    public ServiceBusReceivedMessage Message { get; }

    public ReceivedMessage(ProcessMessageEventArgs processMessageEvent)
    {
        _processMessageEvent = processMessageEvent;
        _token = _processMessageEvent.CancellationToken;
        Message = _processMessageEvent.Message;
    }

    public Task CompleteAsync()
    {
        return _processMessageEvent.CompleteMessageAsync(Message, _token);
    }

    public Task AbandonAsync(Dictionary<string, object> propertiesToModify)
    {
        return _processMessageEvent.AbandonMessageAsync(Message, propertiesToModify, _token);
    }

    public Task DeadLetterAsync(Dictionary<string, object> propertiesToModify, string reason, string description)
    {
        return _processMessageEvent.DeadLetterMessageAsync(Message, propertiesToModify, reason, description, _token);
    }
}

public class ReceivedSessionMessage : IReceivedMessage
{
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ProcessSessionMessageEventArgs _processSessionMessageEvent;
    private readonly CancellationToken _token;
    public ServiceBusReceivedMessage Message { get; }

    public ReceivedSessionMessage(ProcessSessionMessageEventArgs processSessionMessageEvent)
    {
        _semaphoreSlim = new SemaphoreSlim(0, 1);
        _processSessionMessageEvent = processSessionMessageEvent;
        _token = processSessionMessageEvent.CancellationToken;
        Message = processSessionMessageEvent.Message;
    }

    public Task WaitForHandlingAsync()
    {
        return _semaphoreSlim.WaitAsync(_token);
    }

    public Task CompleteAsync()
    {
        return _processSessionMessageEvent.CompleteMessageAsync(Message, _token)
            .ContinueWith(_ => _semaphoreSlim.Release(), _token);;
    }

    public Task AbandonAsync(Dictionary<string, object> propertiesToModify)
    {
        return _processSessionMessageEvent.AbandonMessageAsync(Message, propertiesToModify, _token)
            .ContinueWith(_ => _semaphoreSlim.Release(), _token);
    }

    public Task DeadLetterAsync(Dictionary<string, object> propertiesToModify, string reason, string description)
    {
        return _processSessionMessageEvent.DeadLetterMessageAsync(Message, propertiesToModify, reason, description, _token)
            .ContinueWith(_ => _semaphoreSlim.Release(), _token);
    }
}
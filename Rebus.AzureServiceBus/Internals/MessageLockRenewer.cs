using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Rebus.Internals;

class MessageLockRenewer
{
    readonly ServiceBusReceivedMessage _message;
    readonly ServiceBusReceiver _messageReceiver;

    DateTimeOffset _nextRenewal;

    public MessageLockRenewer(ServiceBusReceivedMessage message, ServiceBusReceiver messageReceiver)
    {
        _message = message;
        _messageReceiver = messageReceiver;
        _nextRenewal = GetTimeOfNextRenewal();
    }

    public string MessageId => _message.MessageId;

    public bool IsDue => DateTimeOffset.Now >= _nextRenewal;

    public async Task Renew()
    {
        // intentionally let exceptions bubble out here, so the caller can log it as a warning
        await _messageReceiver.RenewMessageLockAsync(_message);

        _nextRenewal = GetTimeOfNextRenewal();
    }

    DateTimeOffset GetTimeOfNextRenewal()
    {
        var now = DateTimeOffset.Now;

        var remainingTime = LockedUntil - now;
        var halfOfRemainingTime = TimeSpan.FromMinutes(0.5 * remainingTime.TotalMinutes);

        return now + halfOfRemainingTime;
    }

    DateTimeOffset LockedUntil => _message.LockedUntil;
}
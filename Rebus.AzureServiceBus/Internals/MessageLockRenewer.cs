using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Rebus.Internals
{
    class MessageLockRenewer
    {
        readonly ServiceBusReceivedMessage _message;
        readonly ServiceBusReceiver _messageReceiver;

        DateTime _nextRenewal;

        public MessageLockRenewer(ServiceBusReceivedMessage message, ServiceBusReceiver messageReceiver)
        {
            _message = message;
            _messageReceiver = messageReceiver;

            SetNextRenewal();
        }

        public string MessageId => _message.MessageId;

        public bool IsDue => DateTime.UtcNow >= _nextRenewal;

        public async Task Renew()
        {
            try
            {
                await _messageReceiver.RenewMessageLockAsync(_message);

                SetNextRenewal();
            }
            catch { } //< will automatically be retried if it fails

        }

        void SetNextRenewal()
        {
            var now = DateTime.UtcNow;

            var remainingTime = LockedUntil - now;
            var halfOfRemainingTime = TimeSpan.FromMinutes(0.5 * remainingTime.TotalMinutes);
            _nextRenewal = now + halfOfRemainingTime;
        }

        DateTimeOffset LockedUntil => _message.LockedUntil;
    }
}
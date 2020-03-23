using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Rebus.Internals
{
    class MessageLockRenewer
    {
        readonly Message _message;
        readonly MessageReceiver _messageReceiver;

        DateTime _nextRenewal;

        public MessageLockRenewer(Message message, MessageReceiver messageReceiver)
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
                await _messageReceiver.RenewLockAsync(_message);

                SetNextRenewal();
            }
            catch { } //< will automatically be retried if it fails

        }

        void SetNextRenewal()
        {
            var now = DateTime.UtcNow;

            var remainingTime = LockedUntilUtc - now;
            var halfOfRemainingTime = TimeSpan.FromMinutes(0.5 * remainingTime.TotalMinutes);
            _nextRenewal = now + halfOfRemainingTime;
        }

        DateTime LockedUntilUtc => _message.SystemProperties.LockedUntilUtc;
    }
}
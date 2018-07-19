using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
#pragma warning disable 1998

namespace Rebus.Internals
{
    static class ManagementExtensions
    {
        public static async Task DeleteQueueIfExistsAsync(this ManagementClient client, string queuePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await client.DeleteQueueAsync(queuePath, cancellationToken);
            }
            catch (MessagingEntityNotFoundException)
            {
                // it's ok man
            }
        }

        public static async Task CreateQueueIfNotExistsAsync(this ManagementClient client, string queuePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await client.CreateQueueAsync(queuePath, cancellationToken);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // it's ok man
            }
        }

        public static async Task PurgeQueue(string connectionString, string queuePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queueClient = new QueueClient(connectionString, queuePath, receiveMode: ReceiveMode.ReceiveAndDelete);

            try
            {
                var lastReceiveTime = DateTime.UtcNow.Ticks;

                void UpdateLastReceivedTime() => Interlocked.Exchange(ref lastReceiveTime, DateTime.UtcNow.Ticks);

                async Task MessageHandler(Message message, CancellationToken token) => UpdateLastReceivedTime();
                async Task ExceptionHandler(ExceptionReceivedEventArgs exception) => UpdateLastReceivedTime();

                queueClient.RegisterMessageHandler(MessageHandler, ExceptionHandler);

                var timeoutTicks = TimeSpan.FromSeconds(1).Ticks;

                // wait until we have a period of one second without any messages
                while (DateTime.UtcNow.Ticks - Interlocked.Read(ref lastReceiveTime) < timeoutTicks)
                {
                    await Task.Delay(300, cancellationToken);
                }
            }
            finally
            {
                await queueClient.CloseAsync();
            }
        }
    }
}
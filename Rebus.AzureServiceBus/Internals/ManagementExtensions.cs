using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
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

        public static async Task PurgeQueue(string connectionString, string queuePath, CancellationToken cancellationToken = default(CancellationToken), bool ignoreNonExistentQueue = false)
        {
            var messageReceiver = new MessageReceiver(connectionString, queuePath, receiveMode: ReceiveMode.ReceiveAndDelete);

            try
            {
                while (true)
                {
                    var messages = await messageReceiver.ReceiveAsync(100, TimeSpan.FromSeconds(2));

                    if (!messages.Any()) break;
                }
            }
            catch (MessagingEntityNotFoundException) when (ignoreNonExistentQueue)
            {
                // ignore it then
            }
            finally
            {
                await messageReceiver.CloseAsync();
            }
        }
    }
}
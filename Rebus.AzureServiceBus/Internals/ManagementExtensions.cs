using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Rebus.Internals;

static class ManagementExtensions
{
    public static async Task DeleteQueueIfExistsAsync(this ServiceBusAdministrationClient client, string queuePath, CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            await client.DeleteQueueAsync(queuePath, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException)
        {
            // it's ok man
        }
    }

    public static async Task CreateQueueIfNotExistsAsync(this ServiceBusAdministrationClient client, string queuePath, CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            await client.CreateQueueAsync(queuePath, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException)
        {
            // it's ok man
        }
    }

    public static async Task PurgeQueue(string connectionString, string queueName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var messageReceiver = new ServiceBusClient(connectionString).CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        });

        try
        {
            while (true)
            {
                var messages = await messageReceiver.ReceiveMessagesAsync(100, TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);

                if (messages == null) break;
                if (!messages.Any()) break;
            }
        }
        catch (ServiceBusException)
        {
            // ignore it then
        }
        finally
        {
            await messageReceiver.CloseAsync().ConfigureAwait(false);
        }
    }
}
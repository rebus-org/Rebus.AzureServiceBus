using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;

namespace Rebus.AzureServiceBus.Tests.Extensions
{
    static class TestManagementClientInternals
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
    }
}
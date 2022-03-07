using Azure.Messaging.ServiceBus;

namespace Rebus.Internals;

static class SeviceBusSenderExtensions
{
    public static string GetQueuePath(this ServiceBusSender sender) => $"http://{sender.FullyQualifiedNamespace}/{sender.EntityPath}";

}
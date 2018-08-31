using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Rebus.AzureServiceBus
{
    class ReceivedMessage
    {
        public Message Message { get; }
        public MessageReceiver MessageReceiver { get; }

        public ReceivedMessage(Message message, MessageReceiver messageReceiver)
        {
            Message = message;
            MessageReceiver = messageReceiver;
        }    
    }
}
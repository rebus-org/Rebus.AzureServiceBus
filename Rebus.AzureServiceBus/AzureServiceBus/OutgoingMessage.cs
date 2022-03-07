using Rebus.Messages;

namespace Rebus.AzureServiceBus;

class OutgoingMessage
{
    public string DestinationAddress { get; }
    public TransportMessage TransportMessage { get; }

    public OutgoingMessage(string destinationAddress, TransportMessage transportMessage)
    {
        DestinationAddress = destinationAddress;
        TransportMessage = transportMessage;
    }
}
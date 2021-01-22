using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Transport;
using Message = Microsoft.Azure.ServiceBus.Message;

namespace Rebus.Config
{
    /// <summary>
    /// Experimental configuration extensions for changing the way dead-lettering works with Rebus
    /// </summary>
    public static class AdditionalAzureServiceBusConfigurationExtensions
    {
        /// <summary>
        /// Extends Rebus' built-in deadlettering with the ability to use Azure Service Bus' built-in deadlettering
        /// </summary>
        public static void UseNativeDeadlettering(this StandardConfigurer<ITransport> configurer)
        {
            configurer
                .OtherService<IErrorHandler>()
                .Decorate(c => new BuiltInDeadletteringErrorHandler(c.Get<IErrorHandler>()));
        }

        class BuiltInDeadletteringErrorHandler : IErrorHandler
        {
            readonly IErrorHandler _errorHandler;

            public BuiltInDeadletteringErrorHandler(IErrorHandler errorHandler) => _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

            public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
            {
                if (transactionContext.Items.TryGetValue("asb-message", out var messageObject)
                    && messageObject is Message message
                    && transactionContext.Items.TryGetValue("asb-message-receiver", out var messageReceiverObject)
                    && messageReceiverObject is IMessageReceiver messageReceiver)
                {
                    await messageReceiver.DeadLetterAsync(message.SystemProperties.LockToken, exception.Message, exception.ToString());

                    // remove the message from the context, so the transport doesn't try to complete the message
                    transactionContext.Items.TryRemove("asb-message", out _);
                }
                else
                {
                    await _errorHandler.HandlePoisonMessage(transportMessage, transactionContext, exception);
                }
            }
        }
    }
}
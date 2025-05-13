using System;
using System.Collections.Generic;
using System.Text;

namespace Rebus.AzureServiceBus.Events
{
    public delegate void RenewMessageFailedHandler(string messageId, Exception exception);
   
}

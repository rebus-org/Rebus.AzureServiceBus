namespace Rebus.Config
{
    /// <summary>
    /// Allows for configuring additional options for the Azure Service Bus transport (when running in one-way client mode)
    /// </summary>
    public class AzureServiceBusTransportClientSettings
    {
        internal bool LegacyNamingEnabled { get; set; }

        /// <summary>
        /// Enables "legacy naming", which means that queue names are lowercased, and topic names are "normalized" to be in accordance
        /// with how v6 of the transport did it.
        /// </summary>
        public AzureServiceBusTransportClientSettings UseLegacyNaming()
        {
            LegacyNamingEnabled = true;
            return this;
        }
    }
}
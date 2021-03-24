using System;

namespace Rebus.Config
{
    /// <summary>
    /// Allows for configuring additional options for the Azure Service Bus transport (when running in one-way client mode)
    /// </summary>
    public class AzureServiceBusTransportClientSettings
    {
        internal bool LegacyNamingEnabled { get; set; }
        internal int MaximumMessagePayloadBytes { get; set; } = 210 * 1024;

        /// <summary>
        /// Configures the maxiumum payload request limit. Relevant, when Rebus auto-batches sent messages, keeping the size of each individual batch below 256 kB.
        /// If the SKU allows more than the default 256 kB, it can be increased by calling this method.
        /// </summary>
        public AzureServiceBusTransportClientSettings SetMessagePayloadSizeLimit(int maximumMessagePayloadBytes)
        {
            if (maximumMessagePayloadBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumMessagePayloadBytes), maximumMessagePayloadBytes, "Please provide a value greater than 0");
            }

            MaximumMessagePayloadBytes = maximumMessagePayloadBytes;

            return this;
        }

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
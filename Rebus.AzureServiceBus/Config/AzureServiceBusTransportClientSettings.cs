namespace Rebus.Config;

/// <summary>
/// Allows for configuring additional options for the Azure Service Bus transport (when running in one-way client mode)
/// </summary>
public class AzureServiceBusTransportClientSettings
{
    internal bool LegacyNamingEnabled { get; set; }
    
    /// <summary>
    /// Gets/sets whether to skip checking topics configuration
    /// </summary>
    public bool DoNotConfigureTopicEnabled { get; set; }

    /// <summary>
    /// Enables "legacy naming", which means that queue names are lowercased, and topic names are "normalized" to be in accordance
    /// with how v6 of the transport did it.
    /// </summary>
    public AzureServiceBusTransportClientSettings UseLegacyNaming()
    {
        LegacyNamingEnabled = true;
        return this;
    }
    
    /// <summary>
    /// Skips topic verification. Can be used when the connection string does not have administration access
    /// Should be careful, your topics should already have configured the forwards configurations
    /// </summary>
    public AzureServiceBusTransportClientSettings DoNotConfigureTopic()
    {
        DoNotConfigureTopicEnabled = true;
        return this;
    }
}
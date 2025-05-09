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
    /// When enabled
    /// - will not create the topic, subscription, or configure forwarding to the input queue.
    /// - When needed subscribe method, will not delete the subscription for the specified topic.
    ///
    /// This flag is particularly useful in scenarios where:
    /// - The application has only "Listen" permissions and lacks administrative rights to manage 
    ///   topics and subscriptions in Azure Service Bus.
    /// - The infrastructure is centrally managed, and topics/subscriptions are provisioned 
    ///   manually or by deployment scripts.
    /// - Security restrictions require more controlled and audited modifications to Service Bus entities.
    ///
    /// However, enabling this flag introduces the following considerations:
    /// - <b>Manual Provisioning Required:</b> Topics, subscriptions, and forwarding must be manually configured 
    ///   in the Azure Service Bus namespace.
    /// - <b>Potential Message Loss:</b> If the subscription does not exist or is misconfigured, 
    ///   messages will not be received and may be lost if not handled with a retry mechanism or DLQ (Dead Letter Queue).
    /// - <b>Higher Maintenance Overhead:</b> Scaling or deploying new instances requires explicit verification 
    ///   that all Service Bus entities are in place and correctly configured.
    /// </summary>
    public AzureServiceBusTransportClientSettings DoNotConfigureTopic()
    {
        DoNotConfigureTopicEnabled = true;
        return this;
    }
}
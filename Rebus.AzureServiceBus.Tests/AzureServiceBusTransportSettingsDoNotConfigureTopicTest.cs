using NUnit.Framework;
using Rebus.Config;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class AzureServiceBusTransportSettingsDoNotConfigureTopicTest
{
    [Test]
    public void DoNotConfigureTopic_SetsDoNotConfigureTopicEnabled_ToTrue()
    {
        var settings = new AzureServiceBusTransportSettings();
        
        settings.DoNotConfigureTopic();
        Assert.That(settings.DoNotConfigureTopicEnabled, Is.True);
    }
    
    [Test]
    public void DoNotConfigureTopicEnabled_IsFalse_ByDefault()
    {
        var settings = new AzureServiceBusTransportSettings();
        Assert.That(settings.DoNotConfigureTopicEnabled, Is.False);
    }
    
    [Test]
    public void DoNotConfigureTopic_ReturnsSelf_ForChaining()
    {
        var settings = new AzureServiceBusTransportSettings();
        
        var result = settings.DoNotConfigureTopic();
        Assert.That(result, Is.SameAs(settings));
    }
}

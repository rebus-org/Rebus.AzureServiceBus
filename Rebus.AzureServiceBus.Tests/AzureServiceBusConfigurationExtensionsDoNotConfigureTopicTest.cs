using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class AzureServiceBusConfigurationExtensionsDoNotConfigureTopicTest : FixtureBase
    {
        static readonly string TestQueueName = TestConfig.GetName("test-queue");
        readonly ConsoleLoggerFactory _consoleLoggerFactory = new ConsoleLoggerFactory(false);
        
        [Test]
        public void UseAzureServiceBus_WithDoNotConfigureTopic_SetsDoNotConfigureTopicEnabledToTrue()
        {
            var transport = new AzureServiceBusTransport(
                AsbTestConfig.ConnectionString, 
                TestQueueName, 
                _consoleLoggerFactory, 
                new TplAsyncTaskFactory(_consoleLoggerFactory), 
                new DefaultNameFormatter(), 
                new AzureServiceBus.Messages.DefaultMessageConverter());
            
            Using(transport);
            
            transport.Initialize();
            
            var settings = new AzureServiceBusTransportSettings();
            settings.DoNotConfigureTopic();
            
            transport.DoNotConfigureTopicEnabled = settings.DoNotConfigureTopicEnabled;
            
            Assert.That(transport.DoNotConfigureTopicEnabled, Is.True);
        }
        
        [Test]
        public void UseAzureServiceBusAsOneWayClient_WithDoNotConfigureTopic_SetsDoNotConfigureTopicEnabledToTrue()
        {
            var transport = new AzureServiceBusTransport(
                AsbTestConfig.ConnectionString, 
                null, 
                _consoleLoggerFactory, 
                new TplAsyncTaskFactory(_consoleLoggerFactory), 
                new DefaultNameFormatter(), 
                new AzureServiceBus.Messages.DefaultMessageConverter());
            
            Using(transport);
            
            var settings = new AzureServiceBusTransportClientSettings();
            settings.DoNotConfigureTopic();
            
            transport.DoNotConfigureTopicEnabled = settings.DoNotConfigureTopicEnabled;
            
            Assert.That(transport.DoNotConfigureTopicEnabled, Is.True);
        }
        
        [Test]
        public void UseAzureServiceBus_KeepsDoNotConfigureTopicEnabled_FalseByDefault()
        {
            var transport = new AzureServiceBusTransport(
                AsbTestConfig.ConnectionString, 
                TestQueueName, 
                _consoleLoggerFactory, 
                new TplAsyncTaskFactory(_consoleLoggerFactory), 
                new DefaultNameFormatter(), 
                new AzureServiceBus.Messages.DefaultMessageConverter());
            
            Using(transport);
            
            transport.Initialize();
            
            Assert.That(transport.DoNotConfigureTopicEnabled, Is.False);
        }
        
        [Test]
        public void UseAzureServiceBusAsOneWayClient_KeepsDoNotConfigureTopicEnabled_FalseByDefault()
        {
            var transport = new AzureServiceBusTransport(
                AsbTestConfig.ConnectionString, 
                null, 
                _consoleLoggerFactory, 
                new TplAsyncTaskFactory(_consoleLoggerFactory), 
                new DefaultNameFormatter(), 
                new AzureServiceBus.Messages.DefaultMessageConverter());
            
            Using(transport);
            
            Assert.That(transport.DoNotConfigureTopicEnabled, Is.False);
        }
    }
}

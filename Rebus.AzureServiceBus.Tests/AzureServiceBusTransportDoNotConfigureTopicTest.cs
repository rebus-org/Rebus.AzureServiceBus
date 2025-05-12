using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.AzureServiceBus.NameFormat;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class AzureServiceBusTransportDoNotConfigureTopicTest : FixtureBase
{
    static readonly string TestQueueName = TestConfig.GetName("test-queue");
    readonly ConsoleLoggerFactory _consoleLoggerFactory = new ConsoleLoggerFactory(false);
        
    [Test]
    public async Task RegisterSubscriber_SkipsTopicConfiguration_WhenDoNotConfigureTopicEnabledIsTrue()
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
        transport.DoNotConfigureTopicEnabled = true;
            
        await transport.RegisterSubscriber("test-topic", TestQueueName);
            
        Assert.Pass();
    }
        
    [Test]
    public async Task UnregisterSubscriber_SkipsTopicConfiguration_WhenDoNotConfigureTopicEnabledIsTrue()
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
        transport.DoNotConfigureTopicEnabled = true;
            
        await transport.UnregisterSubscriber("test-topic", TestQueueName);
            
        Assert.Pass();
    }
}
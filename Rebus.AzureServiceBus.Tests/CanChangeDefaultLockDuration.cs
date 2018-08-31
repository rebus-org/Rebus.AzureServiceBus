using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
using Rebus.Internals;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class CanChangeDefaultLockDuration : FixtureBase
    {
        ManagementClient _managementClient;
        string _queueName;

        protected override void SetUp()
        {
            _managementClient = new ManagementClient(AzureServiceBusTransportFactory.ConnectionString);
            _queueName = TestConfig.GetName("lockduration");

            AsyncHelpers.RunSync(() => _managementClient.DeleteQueueIfExistsAsync(_queueName));
        }

        protected override void TearDown()
        {
            AsyncHelpers.RunSync(() => _managementClient.DeleteQueueIfExistsAsync(_queueName));
        }

        [Test]
        public async Task CanDoIt()
        {
            void InitializeBusWith(TimeSpan peekLockDuration, TimeSpan defaultMessageTtl)
            {
                Configure.With(Using(new BuiltinHandlerActivator()))
                    .Transport(t =>
                    {
                        t.UseAzureServiceBus(AzureServiceBusTransportFactory.ConnectionString, _queueName)
                            .SetMessagePeekLockDuration(peekLockDuration)
                            .SetDefaultMessageTimeToLive(defaultMessageTtl);
                    })
                    .Start();
            }

            InitializeBusWith(
                peekLockDuration: TimeSpan.FromMinutes(2),
                defaultMessageTtl: TimeSpan.FromDays(5)
            );

            CleanUpDisposables();

            InitializeBusWith(
                peekLockDuration: TimeSpan.FromMinutes(1),
                defaultMessageTtl: TimeSpan.FromDays(1)
            );

            CleanUpDisposables();

            var queueDescription = await _managementClient.GetQueueAsync(_queueName);

            Assert.That(queueDescription.DefaultMessageTimeToLive, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(queueDescription.LockDuration, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }
    }
}
using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
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
        ServiceBusAdministrationClient _managementClient;
        string _queueName;

        protected override void SetUp()
        {
            _managementClient = new ServiceBusAdministrationClient(AsbTestConfig.ConnectionString);
            _queueName = TestConfig.GetName("lockduration");

            AsyncHelpers.RunSync(() => _managementClient.DeleteQueueIfExistsAsync(_queueName));
        }

        protected override void TearDown()
        {
            AsyncHelpers.RunSync(() => _managementClient.DeleteQueueIfExistsAsync(_queueName));
        }

        [Test]
        public async Task CanConfigureDuplicateDetection()
        {
            var duration = TimeSpan.FromHours(2);

            Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t =>
                {
                    t.UseAzureServiceBus(AsbTestConfig.ConnectionString, _queueName)
                        .SetDuplicateDetectionHistoryTimeWindow(duration);
                })
                .Start();

            CleanUpDisposables();

            var queueDescription = await _managementClient.GetQueueAsync(_queueName);

            Assert.That(queueDescription.Value.RequiresDuplicateDetection, Is.True);
            Assert.That(queueDescription.Value.DuplicateDetectionHistoryTimeWindow, Is.EqualTo(duration));
        }

        [Test]
        public async Task CanChangeTheseSettingsAfterTheFact()
        {
            void InitializeBusWith(TimeSpan peekLockDuration, TimeSpan defaultMessageTtl, TimeSpan autoDeleteOnIdle)
            {
                Configure.With(Using(new BuiltinHandlerActivator()))
                    .Transport(t =>
                    {
                        t.UseAzureServiceBus(AsbTestConfig.ConnectionString, _queueName)
                            .SetMessagePeekLockDuration(peekLockDuration)
                            .SetDefaultMessageTimeToLive(defaultMessageTtl)
                            .SetAutoDeleteOnIdle(autoDeleteOnIdle);
                    })
                    .Start();
            }

            InitializeBusWith(
                peekLockDuration: TimeSpan.FromMinutes(2),
                defaultMessageTtl: TimeSpan.FromDays(5),
                autoDeleteOnIdle: TimeSpan.FromHours(1)
            );

            CleanUpDisposables();

            // wait a while because some of the settings seem to be updating slowly
            await Task.Delay(TimeSpan.FromSeconds(5));

            InitializeBusWith(
                peekLockDuration: TimeSpan.FromMinutes(1),
                defaultMessageTtl: TimeSpan.FromDays(1),
                autoDeleteOnIdle: TimeSpan.FromHours(5)
            );

            CleanUpDisposables();

            // wait a while because some of the settings seem to be updating slowly
            await Task.Delay(TimeSpan.FromSeconds(5));

            var queueDescription = await _managementClient.GetQueueAsync(_queueName);

            Assert.That(queueDescription.Value.DefaultMessageTimeToLive, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(queueDescription.Value.LockDuration, Is.EqualTo(TimeSpan.FromMinutes(1)));
            Assert.That(queueDescription.Value.AutoDeleteOnIdle, Is.EqualTo(TimeSpan.FromHours(5)));
        }
    }
}
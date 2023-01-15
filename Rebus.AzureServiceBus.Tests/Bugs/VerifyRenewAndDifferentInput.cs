// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using NUnit.Framework;
// using Rebus.Activation;
// using Rebus.Config;
// using Rebus.Tests.Contracts;
// using Rebus.Tests.Contracts.Extensions;
//
// namespace Rebus.AzureServiceBus.Tests.Bugs;
//
// [TestFixture]
// public class VerifyRenewAndDifferentInput : FixtureBase
// {
//     [Test]
//     public async Task ItWorks()
//     {
//         var activator = new BuiltinHandlerActivator();
//         var done = new ManualResetEvent(false);
//
//         activator.Handle<string>(async message =>
//         {
//             await Task.Delay(TimeSpan.FromMinutes(1));
//
//             done.Set();
//         });
//
//         Using(activator);
//
//         var bus = Configure.With(activator)
//             .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, "renew-tjek")
//                 .AutomaticallyRenewPeekLock()
//                 .SetMessagePeekLockDuration(TimeSpan.FromSeconds(10)))
//             .Start();
//
//         await bus.SendLocal("HEJ MED DIG MIN VEN!");
//
//         done.WaitOrDie(TimeSpan.FromMinutes(1.5),
//             errorMessage: "Message did not finish handling within 1.5 minute timeout");
//     }
// }
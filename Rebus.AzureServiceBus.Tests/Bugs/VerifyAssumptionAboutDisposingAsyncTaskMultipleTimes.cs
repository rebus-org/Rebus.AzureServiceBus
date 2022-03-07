using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Threading.TaskParallelLibrary;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus.Tests.Bugs;

[TestFixture]
public class VerifyAssumptionAboutDisposingAsyncTaskMultipleTimes : FixtureBase
{
    [Test]
    public async Task NoProblem()
    {
        var consoleLoggerFactory = new ConsoleLoggerFactory(colored: false);
        var factory = new TplAsyncTaskFactory(consoleLoggerFactory);
        var logger = consoleLoggerFactory.GetLogger<VerifyAssumptionAboutDisposingAsyncTaskMultipleTimes>();
        var task = factory.Create("test-task", intervalSeconds: 1, action: async () => logger.Info("Called back"));

        task.Start();

        await Task.Delay(TimeSpan.FromSeconds(4));

        task.Dispose();
        task.Dispose();
        task.Dispose();
        task.Dispose();
    }
        
}
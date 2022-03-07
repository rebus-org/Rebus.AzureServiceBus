using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Internals;
#pragma warning disable 1998
#pragma warning disable 4014

namespace Rebus.AzureServiceBus.Tests;

[TestFixture]
public class TestExceptionIgnorant
{
    [Test]
    public async Task IgnoresExceptionsAsExpected()
    {
        var attempt = 1;

        await new ExceptionIgnorant()
            .Ignore<InvalidOperationException>()
            .Execute(async () =>
            {
                attempt++;

                if (attempt < 4)
                {
                    throw new InvalidOperationException("SHOULD NOT ESCAPE");
                }
            });
    }

    [Test]
    public async Task OnlyIgnoresExceptionsThatMatchCriteria()
    {
        var executions = 0;

        var applicationException = Assert.ThrowsAsync<ApplicationException>(async () =>
        {
            await new ExceptionIgnorant()
                .Ignore<ApplicationException>(a => a.Message.Contains("not in the message"))
                .Execute(async () =>
                {
                    executions++;
                    throw new ApplicationException("I'm out!!");
                });
        });

        Console.WriteLine(applicationException);

        Assert.That(executions, Is.EqualTo(1), "Verify that only one single execution was performed, meaning that the exception 'escaped' immediately");
    }
}
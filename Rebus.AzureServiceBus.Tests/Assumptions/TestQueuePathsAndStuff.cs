using System;
using NUnit.Framework;

namespace Rebus.AzureServiceBus.Tests.Assumptions
{
    [TestFixture]
    public class TestQueuePathsAndStuff
    {
        [Test]
        public void CanGetTheInterestingPart()
        {
            var fullPath = "http://rebustest3.servicebus.windows.net/group/some.inputqueue";

            var uri = new Uri(fullPath);

            Assert.That(uri.PathAndQuery, Is.EqualTo("group/some.inputqueue"));
        }
    }
}
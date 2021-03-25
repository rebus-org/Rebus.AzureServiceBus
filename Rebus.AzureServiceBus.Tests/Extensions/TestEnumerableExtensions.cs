using System.Linq;
using NUnit.Framework;
using Rebus.Internals;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests.Extensions
{
    [TestFixture]
    public class TestEnumerableExtensions : FixtureBase
    {
        [Test]
        public void CanBatchWeighted()
        {
            var items = new[] { 1, 2, 10, 12, 5, 6, 7 };

            var batches = items.BatchWeighted(i => i, maxWeight: 15).ToList();

            Assert.That(batches.Count, Is.EqualTo(4));

            Assert.That(batches[0], Is.EqualTo(new[] { 1, 2, 10 }));
            Assert.That(batches[1], Is.EqualTo(new[] { 12 }));
            Assert.That(batches[2], Is.EqualTo(new[] { 5, 6 }));
            Assert.That(batches[3], Is.EqualTo(new[] { 7 }));
        }

        [Test]
        public void CanBatchWeighted_IntentionallyExceed()
        {
            var items = new[] { 1, 16, 10, 18 };

            var batches = items.BatchWeighted(i => i, maxWeight: 15).ToList();

            Assert.That(batches.Count, Is.EqualTo(4));

            Assert.That(batches[0], Is.EqualTo(new[] { 1 }));
            Assert.That(batches[1], Is.EqualTo(new[] { 16 }));
            Assert.That(batches[2], Is.EqualTo(new[] { 10 }));
            Assert.That(batches[3], Is.EqualTo(new[] { 18 }));
        }

        [Test]
        public void TryWithMessages()
        {
            var messages = Enumerable.Range(0, 1000)
                .Select(n => new MessageWithText($"message {n}"));

            var batches = messages.BatchWeighted(m => m.Text.Length, 1000).ToList();

            Assert.That(batches.All(b => b.Sum(m => m.Text.Length) <= 1000), Is.True);
        }

        record MessageWithText(string Text);
    }
}
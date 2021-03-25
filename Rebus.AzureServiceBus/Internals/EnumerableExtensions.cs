using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Internals
{
    static class EnumerableExtensions
    {
        public static IEnumerable<IReadOnlyList<TItem>> Batch<TItem>(this IEnumerable<TItem> items, int maxBatchSize)
        {
            var list = new List<TItem>();

            foreach (var item in items)
            {
                list.Add(item);

                if (list.Count < maxBatchSize) continue;

                yield return list.ToArray();

                list.Clear();
            }

            if (list.Any())
            {
                yield return list.ToArray();
            }
        }

        /// <summary>
        /// Performs weighted batching of <paramref name="items"/>, delivering batches whose total weight is below <paramref name="maxWeight"/> if possible.
        /// If one of the items has a weight that exceeds the maximum weight, then nothing is done about it, it will just be returned in its own batch.
        ///  </summary>
        public static IEnumerable<IReadOnlyList<TItem>> BatchWeighted<TItem>(this IEnumerable<TItem> items, Func<TItem, decimal> getWeight, decimal maxWeight)
        {
            var list = new List<TItem>();
            var sum = 0m;

            foreach (var item in items)
            {
                var weight = getWeight(item);

                if (sum + weight < maxWeight)
                {
                    list.Add(item);
                    sum += weight;
                    continue;
                }

                yield return list.ToArray();

                list.Clear();
                list.Add(item);
                sum = weight;
            }

            if (list.Any())
            {
                yield return list.ToArray();
            }
        }
    }
}
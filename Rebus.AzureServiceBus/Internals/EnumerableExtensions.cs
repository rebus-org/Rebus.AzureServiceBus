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
    }
}
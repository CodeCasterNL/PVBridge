using System.Collections.Generic;
using System.Linq;

namespace CodeCaster.PVBridge.Utils
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Enumerate over a collection in batches.
        /// By Zaki, https://stackoverflow.com/users/1129995/zaki, https://stackoverflow.com/a/15414496/
        /// </summary>
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int batchSize)
        {
            var batch = new List<TSource>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count != batchSize)
                {
                    continue;
                }
                
                yield return batch;
                batch = new List<TSource>(batchSize);
            }

            if (batch.Any()) yield return batch;
        }

        /// <summary>
        /// Returns the count followed by the name, and an "s" if the count contains something other than 1 item, to prevent the ugly construct "(s)" in logs.
        /// </summary>
        public static string SIfPlural(this int count, string collectionName, string? pluralName = null)
            => $"{count} " + (count == 1 ? collectionName : pluralName ?? collectionName + "s");

        /// <summary>
        /// Shorthand syntax for <code>new[] { Foo, Bar }.Contains(foo)</code>.
        /// </summary>
        public static bool In<T>(this T value, params T[] values) => values.Contains(value);
    }
}

using System.Runtime.CompilerServices;
using Shared.FileReader;

namespace Shared
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Tap<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T? item in source)
            {
                action(item);
                yield return item;
            }
        }

        public static List<List<T>> ChunkRows<T>(this List<T> source, int chunkSize)
        {
            List<List<T>> chunks = [];
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                chunks.Add(source.GetRange(i, Math.Min(chunkSize, source.Count - i)));
            }
            return chunks;
        }

        public static async IAsyncEnumerable<T> SortAsync<T>(this IAsyncEnumerable<T> rows, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where T : IRow
        {
            var sorted = await rows
                .OrderByAwait(row => new ValueTask<string>(row.WorkSheetName))
                .ThenByAwait(row => new ValueTask<int>(row.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var row in sorted)
            {
                yield return row;
            }
        }
        public static async IAsyncEnumerable<List<T>> BatchAsync<T>(this IAsyncEnumerable<T> source, int batchSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

            List<T> batch = new(batchSize);

            await foreach (var item in source)
            {
                batch.Add(item);

                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

    }
}
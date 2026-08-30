using Shared.FileReader;

namespace Shared
{
    public class RowFilter
    {
        public async IAsyncEnumerable<T> FilterAsync<T>(IAsyncEnumerable<T> rows, Func<T, bool> predicate)
            where T : IRow
        {
            Console.WriteLine("Begin FilterAsync");

            await foreach (T line in rows)
            {
                if (predicate(line))
                    yield return line;
            }

            Console.WriteLine("End FilterAsync");
        }

        public async Task<IRow?> FilterAsync(Task<IRow> task, Func<IRow, bool> predicate)
        {
            IRow line = await task.ConfigureAwait(false);
            return predicate(line) ? line : null;
        }
    }

    public class LogFilter
    {
        private readonly string[] _keywords = { ": error"/*, "WARN", "Timeout"*/ };

        public async IAsyncEnumerable<string> FilterAsync(IAsyncEnumerable<string> lines)
        {
            Console.WriteLine("Begin FilterAsync");

            await foreach (string line in lines)
            {
                if (_keywords.Any(k => line.Contains(k)))
                    yield return line;
            }

            Console.WriteLine("End FilterAsync");
        }

        public async Task<string?> FilterAsync(Task<string> task)
        {
            string line = await task.ConfigureAwait(false);
            return _keywords.Any(k => line.Contains(k)) ? line : null;
        }
    }
}

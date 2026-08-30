namespace Shared.FileReader
{
    public static partial class Reader
    {
        public interface IFileReader
        {
            IAsyncEnumerable<IRow> ReadEnumerableAsync(CancellationToken cancellationToken = default);
            Task<IEnumerable<IRow>> ReadTaskEnumerableAsync(CancellationToken cancellationToken = default);
        }
    }
}
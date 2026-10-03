using System.Runtime.CompilerServices;

namespace Shared.FileReader
{
    public static partial class Reader
    {
        public sealed class ClosedXMLExcelStream : IFileReader
        {
            private readonly WorksheetProcessor _processor;
            private readonly IFileLoader<Stream> _loader;

            public ClosedXMLExcelStream(
                WorksheetProcessor processor,
                IFileLoader<Stream> loader)
            {
                ArgumentNullException.ThrowIfNull(processor);
                ArgumentNullException.ThrowIfNull(loader);

                _processor = processor;
                _loader = loader;
            }

            public async IAsyncEnumerable<IRow> ReadEnumerableAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                byte[] buffer = await ReadBytesAsync(cancellationToken);

                Reader.ClosedXMLExcel reader = new(
                    _processor,
                    new ByteArrayLoader(buffer));

                await foreach (IRow row in reader.ReadEnumerableAsync(cancellationToken))
                {
                    yield return row;
                }
            }

            public async Task<IEnumerable<IRow>> ReadTaskEnumerableAsync(
                CancellationToken cancellationToken = default)
            {
                byte[] buffer = await ReadBytesAsync(cancellationToken);

                Reader.ClosedXMLExcel reader = new(
                    _processor,
                    new ByteArrayLoader(buffer));

                return await reader.ReadTaskEnumerableAsync(cancellationToken);
            }

            private async Task<byte[]> ReadBytesAsync(
                CancellationToken cancellationToken)
            {
                Stream stream = _loader.Load();

                ArgumentNullException.ThrowIfNull(stream);

                if (!stream.CanRead)
                {
                    throw new InvalidOperationException(
                        "The source stream must be readable.");
                }

                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                await using MemoryStream memoryStream = new();

                await stream.CopyToAsync(memoryStream, cancellationToken);

                return memoryStream.ToArray();
            }

            private sealed class ByteArrayLoader : IFileLoader<byte[]>
            {
                private readonly byte[] _buffer;

                public ByteArrayLoader(byte[] buffer)
                {
                    _buffer = buffer;
                }

                public byte[] Load() => _buffer;
            }
        }
    }
}
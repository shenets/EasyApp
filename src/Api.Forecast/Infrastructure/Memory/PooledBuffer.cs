using System.Buffers;

namespace Api.Forecast.Infrastructure.Memory;

public sealed class PooledBuffer : IDisposable
{
    private readonly byte[] _buffer;
    private readonly int _size;

    public PooledBuffer(int size)
    {
        _size = size;
        _buffer = ArrayPool<byte>.Shared.Rent(size);
    }

    public Span<byte> Span => _buffer.AsSpan(0, _size);
    public Memory<byte> Memory => _buffer.AsMemory(0, _size);

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}


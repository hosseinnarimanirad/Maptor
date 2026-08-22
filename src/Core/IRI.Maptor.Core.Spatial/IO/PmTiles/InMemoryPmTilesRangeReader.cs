using System;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

public sealed class InMemoryPmTilesRangeReader : IPmTilesRangeReader
{
    private readonly byte[] _buffer;

    public InMemoryPmTilesRangeReader(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public ValueTask<byte[]> ReadAsync(long offset, int length, CancellationToken cancellationToken = default)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (offset < 0 || offset + length > _buffer.LongLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var result = new byte[length];
        Array.Copy(_buffer, offset, result, 0, length);
        return new ValueTask<byte[]>(result);
    }

    public ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<long>((long)_buffer.LongLength);
    }

    public void Dispose()
    {
        // No resources to dispose.
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }
}


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

public sealed class FilePmTilesRangeReader : IPmTilesRangeReader
{
    private readonly string _filePath;
    private FileStream? _stream;

    public FilePmTilesRangeReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        _filePath = filePath;
    }

    public async ValueTask<byte[]> ReadAsync(long offset, int length, CancellationToken cancellationToken = default)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        EnsureStream();
        var buffer = new byte[length];
        _stream!.Seek(offset, SeekOrigin.Begin);
        var read = 0;
        while (read < length)
        {
            var slice = buffer.AsMemory(read, length - read);
            var result = await _stream.ReadAsync(slice, cancellationToken).ConfigureAwait(false);
            if (result == 0)
            {
                throw new EndOfStreamException("Reached end of file while reading PMTiles data.");
            }
            read += result;
        }

        return buffer;
    }

    public ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default)
    {
        EnsureStream();
        return new ValueTask<long>(_stream!.Length);
    }

    private void EnsureStream()
    {
        if (_stream == null)
        {
            _stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            _stream.Dispose();
        }

        return default;
    }
}


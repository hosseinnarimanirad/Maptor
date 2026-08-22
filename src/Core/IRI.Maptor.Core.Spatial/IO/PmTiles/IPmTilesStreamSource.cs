using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Abstraction for fetching PMTiles archive bytes (local files, HTTP range requests, etc.).
/// </summary>
public interface IPmTilesStreamSource : IAsyncDisposable
{
    /// <summary>
    /// Reads a contiguous range of bytes from the archive.
    /// </summary>
    ValueTask<ReadOnlyMemory<byte>> ReadAsync(long offset, int length, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a forward-only stream positioned at the start of the archive.
    /// </summary>
    ValueTask<Stream> OpenStreamAsync(CancellationToken cancellationToken = default);
}


using System;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

public interface IPmTilesRangeReader : IAsyncDisposable, IDisposable
{
    ValueTask<byte[]> ReadAsync(long offset, int length, CancellationToken cancellationToken = default);

    ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default);
}


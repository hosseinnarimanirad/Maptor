using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

/// <summary>
/// Provides read access to PMTiles archives produced according to the v3 specification.
/// </summary>
public sealed class PmTilesReader : IAsyncDisposable
{
    private readonly IPmTilesStreamSource source;
    private readonly Dictionary<ulong, PmTilesDirectory> leafCache = new();

    private bool initialized;

    public PmTilesReader(IPmTilesStreamSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public PmTilesHeader Header { get; private set; } = null!;

    public PmTilesDirectory RootDirectory { get; private set; } = null!;

    public string? MetadataJson { get; private set; }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (initialized)
        {
            return;
        }

        var headerMemory = await source.ReadAsync(0, PmTilesConstants.HeaderLength, cancellationToken).ConfigureAwait(false);
        Header = PmTilesHeader.Read(headerMemory.Span);

        RootDirectory = await ReadDirectoryAsync(Header.RootDirectoryOffset, Header.RootDirectoryLength, cancellationToken).ConfigureAwait(false);

        if (Header.MetadataLength > 0)
        {
            var metadataBytes = await ReadSectionAsync(Header.MetadataOffset, Header.MetadataLength, cancellationToken).ConfigureAwait(false);
            var decompressed = PmTilesCompressionHelper.Decompress(metadataBytes.Span, Header.InternalCompression);
            MetadataJson = Encoding.UTF8.GetString(decompressed);
        }

        initialized = true;
    }

    public async ValueTask<PmTilesTile?> GetTileAsync(int zoom, int x, int y, bool decompress = true, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var address = new PmTilesTileAddress(zoom, x, y);
        return await GetTileByIdAsync(address.TileId, address, decompress, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<PmTilesTile?> GetTileByIdAsync(ulong tileId, bool decompress = true, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var address = new PmTilesTileAddress(tileId);
        return await GetTileByIdAsync(tileId, address, decompress, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<PmTilesDirectory> GetLeafDirectoryAsync(PmTilesDirectoryEntry pointer, CancellationToken cancellationToken = default)
    {
        if (!pointer.IsLeafDirectory)
        {
            throw new ArgumentException("The provided entry is not a leaf directory pointer.", nameof(pointer));
        }

        if (leafCache.TryGetValue(pointer.Offset, out var cached))
        {
            return cached;
        }

        var directory = await ReadDirectoryAsync(
            Header.LeafDirectoriesOffset + pointer.Offset,
            pointer.Length,
            cancellationToken).ConfigureAwait(false);

        leafCache[pointer.Offset] = directory;
        return directory;
    }

    public async ValueTask<string?> GetMetadataJsonAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        return MetadataJson;
    }

    public ValueTask DisposeAsync()
    {
        leafCache.Clear();
        return source.DisposeAsync();
    }

    private async ValueTask<PmTilesTile?> GetTileByIdAsync(ulong tileId, PmTilesTileAddress address, bool decompress, CancellationToken cancellationToken)
    {
        var entry = FindEntry(RootDirectory, tileId);

        if (entry is null)
        {
            return null;
        }

        if (entry.Value.RunLength == 0)
        {
            var leafDirectory = await GetLeafDirectoryAsync(entry.Value, cancellationToken).ConfigureAwait(false);
            var leafEntry = FindEntry(leafDirectory, tileId);

            if (leafEntry is null || leafEntry.Value.RunLength == 0)
            {
                return null;
            }

            return await ReadTileAsync(leafEntry.Value, address, decompress, cancellationToken).ConfigureAwait(false);
        }

        return await ReadTileAsync(entry.Value, address, decompress, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<PmTilesTile?> ReadTileAsync(PmTilesDirectoryEntry entry, PmTilesTileAddress address, bool decompress, CancellationToken cancellationToken)
    {
        if (entry.RunLength == 0)
        {
            return null;
        }

        var relativeIndex = address.TileId - entry.TileId;
        if (relativeIndex >= entry.RunLength)
        {
            return null;
        }

        var rawBytes = await ReadSectionAsync(Header.TileDataOffset + entry.Offset, entry.Length, cancellationToken).ConfigureAwait(false);

        if (!decompress || Header.TileCompression == PmTilesCompression.None)
        {
            return new PmTilesTile(address, rawBytes, Header.TileType, Header.TileCompression, isDecompressed: Header.TileCompression == PmTilesCompression.None);
        }

        var payload = PmTilesCompressionHelper.Decompress(rawBytes.Span, Header.TileCompression);
        return new PmTilesTile(address, payload, Header.TileType, Header.TileCompression, isDecompressed: true);
    }

    private async ValueTask<PmTilesDirectory> ReadDirectoryAsync(ulong relativeOffset, ulong length, CancellationToken cancellationToken)
    {
        if (length == 0)
        {
            throw new InvalidOperationException("Directory length cannot be zero.");
        }

        var uncompressed = await ReadSectionAsync(relativeOffset, length, cancellationToken).ConfigureAwait(false);
        var buffer = PmTilesCompressionHelper.Decompress(uncompressed.Span, Header.InternalCompression);
        return PmTilesDirectory.Decode(buffer);
    }

    private static PmTilesDirectoryEntry? FindEntry(PmTilesDirectory directory, ulong tileId)
    {
        var entries = directory.Entries;
        var left = 0;
        var right = entries.Count - 1;
        PmTilesDirectoryEntry? candidate = null;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            var entry = entries[mid];

            if (entry.TileId == tileId)
            {
                candidate = entry;
                break;
            }

            if (entry.TileId < tileId)
            {
                candidate = entry;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        if (candidate == null)
        {
            return null;
        }

        if (candidate.Value.RunLength == 0)
        {
            return candidate;
        }

        var rangeEnd = candidate.Value.TileId + candidate.Value.RunLength;
        return tileId < rangeEnd ? candidate : null;
    }

    private async ValueTask<ReadOnlyMemory<byte>> ReadSectionAsync(ulong offset, ulong length, CancellationToken cancellationToken)
    {
        var readOffset = checked((long)offset);
        var readLength = checked((int)length);
        return await source.ReadAsync(readOffset, readLength, cancellationToken).ConfigureAwait(false);
    }
} 


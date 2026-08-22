using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Builds PMTiles archives from in-memory tile payloads.
/// </summary>
public sealed class PmTilesWriter
{
    private readonly List<TileRecord> tiles = new();

    public void AddTile(int zoom, int x, int y, ReadOnlySpan<byte> data, bool isCompressed = false)
    {
        var address = new PmTilesTileAddress(zoom, x, y);
        tiles.Add(new TileRecord(address, data.ToArray(), isCompressed));
    }

    public void AddTile(PmTilesTileAddress address, ReadOnlySpan<byte> data, bool isCompressed = false)
    {
        tiles.Add(new TileRecord(address, data.ToArray(), isCompressed));
    }

    public async Task WriteAsync(Stream destination, PmTilesWriterOptions options, CancellationToken cancellationToken = default)
    {
        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!destination.CanWrite)
        {
            throw new InvalidOperationException("Destination stream must be writable.");
        }

        if (tiles.Count == 0)
        {
            throw new InvalidOperationException("At least one tile is required to produce a PMTiles archive.");
        }

        if (options.TileType == PmTilesTileType.Unknown)
        {
            throw new InvalidOperationException("TileType must be specified for PMTiles output.");
        }

        var orderedTiles = tiles
            .OrderBy(t => t.Address.TileId)
            .ToList();

        var tileEntries = new List<PmTilesDirectoryEntry>(orderedTiles.Count);
        using var tileDataStream = new MemoryStream();
        ulong tileOffset = 0;

        foreach (var tile in orderedTiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = tile.Payload;

            if (options.TileCompression != PmTilesCompression.None && !tile.IsCompressed)
            {
                payload = PmTilesCompressionHelper.Compress(payload, options.TileCompression);
            }

            tileDataStream.Write(payload, 0, payload.Length);

            var entry = new PmTilesDirectoryEntry(tile.Address.TileId, tileOffset, (ulong)payload.Length, runLength: 1);
            tileEntries.Add(entry);

            tileOffset += (ulong)payload.Length;
        }

        var rootDirectory = new PmTilesDirectory(tileEntries);
        var rootBytes = rootDirectory.Encode();
        var rootCompressed = PmTilesCompressionHelper.Compress(rootBytes, options.InternalCompression);

        byte[] metadataCompressed = Array.Empty<byte>();
        if (!string.IsNullOrWhiteSpace(options.MetadataJson))
        {
            var metadataBytes = Encoding.UTF8.GetBytes(options.MetadataJson!);
            metadataCompressed = PmTilesCompressionHelper.Compress(metadataBytes, options.InternalCompression);
        }

        using var archive = new MemoryStream();
        archive.SetLength(0);
        archive.Position = PmTilesConstants.HeaderLength;

        archive.Write(rootCompressed, 0, rootCompressed.Length);

        ulong metadataOffset = 0;
        if (metadataCompressed.Length > 0)
        {
            metadataOffset = (ulong)archive.Position;
            archive.Write(metadataCompressed, 0, metadataCompressed.Length);
        }

        var leafDirectoriesOffset = 0UL;
        var leafDirectoriesLength = 0UL;

        var tileDataOffset = (ulong)archive.Position;
        var tileDataBytes = tileDataStream.ToArray();
        archive.Write(tileDataBytes, 0, tileDataBytes.Length);
        var tileDataLength = (ulong)tileDataBytes.LongLength;

        var zoomStats = ComputeZoomStatistics(orderedTiles);
        var bounds = options.BoundsOverride ?? ComputeBounds(orderedTiles);
        var center = options.CenterOverride ?? ComputeCenter(bounds);

        var addressedTiles = tileEntries.Aggregate(0UL, (sum, entry) => entry.RunLength == 0 ? sum : sum + entry.RunLength);
        var tileContents = tileEntries.Count(entry => entry.RunLength > 0);

        var header = new PmTilesHeader
        {
            RootDirectoryOffset = PmTilesConstants.HeaderLength,
            RootDirectoryLength = (ulong)rootCompressed.Length,
            MetadataOffset = metadataOffset,
            MetadataLength = (ulong)metadataCompressed.Length,
            LeafDirectoriesOffset = leafDirectoriesOffset,
            LeafDirectoriesLength = leafDirectoriesLength,
            TileDataOffset = tileDataOffset,
            TileDataLength = tileDataLength,
            NumberOfAddressedTiles = addressedTiles,
            NumberOfTileEntries = (ulong)tileEntries.Count,
            NumberOfTileContents = (ulong)tileContents,
            IsClustered = options.ClusterTiles,
            InternalCompression = options.InternalCompression,
            TileCompression = options.TileCompression,
            TileType = options.TileType,
            MinZoom = options.MinZoomOverride ?? zoomStats.minZoom,
            MaxZoom = options.MaxZoomOverride ?? zoomStats.maxZoom,
            Bounds = bounds,
            CenterZoom = options.CenterZoomOverride ?? zoomStats.centerZoom,
            CenterPosition = center
        };

        var headerBytes = header.ToBytes();
        archive.Position = 0;
        archive.Write(headerBytes, 0, headerBytes.Length);

        archive.Position = 0;
        await archive.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> BuildAsync(PmTilesWriterOptions options, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await WriteAsync(buffer, options, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static (byte minZoom, byte maxZoom, byte centerZoom) ComputeZoomStatistics(IEnumerable<TileRecord> tiles)
    {
        byte minZoom = byte.MaxValue;
        byte maxZoom = 0;

        foreach (var tile in tiles)
        {
            var zoom = (byte)tile.Address.Zoom;
            if (zoom < minZoom)
            {
                minZoom = zoom;
            }

            if (zoom > maxZoom)
            {
                maxZoom = zoom;
            }
        }

        if (minZoom == byte.MaxValue)
        {
            minZoom = 0;
        }

        var centerZoom = maxZoom;
        return (minZoom, maxZoom, centerZoom);
    }

    private static PmTilesBounds ComputeBounds(IEnumerable<TileRecord> tiles)
    {
        double minLon = double.PositiveInfinity;
        double minLat = double.PositiveInfinity;
        double maxLon = double.NegativeInfinity;
        double maxLat = double.NegativeInfinity;

        foreach (var tile in tiles)
        {
            TileBounds(tile.Address.Zoom, tile.Address.X, tile.Address.Y, out var tileMinLon, out var tileMinLat, out var tileMaxLon, out var tileMaxLat);

            minLon = Math.Min(minLon, tileMinLon);
            minLat = Math.Min(minLat, tileMinLat);
            maxLon = Math.Max(maxLon, tileMaxLon);
            maxLat = Math.Max(maxLat, tileMaxLat);
        }

        if (double.IsPositiveInfinity(minLon))
        {
            minLon = -180;
            minLat = -85;
            maxLon = 180;
            maxLat = 85;
        }

        return new PmTilesBounds(
            PmTilesPosition.FromDegrees(minLon, minLat),
            PmTilesPosition.FromDegrees(maxLon, maxLat));
    }

    private static PmTilesPosition ComputeCenter(PmTilesBounds bounds)
    {
        var lon = (bounds.Min.Longitude + bounds.Max.Longitude) / 2d;
        var lat = (bounds.Min.Latitude + bounds.Max.Latitude) / 2d;
        return PmTilesPosition.FromDegrees(lon, lat);
    }

    private static void TileBounds(int zoom, int x, int y, out double minLon, out double minLat, out double maxLon, out double maxLat)
    {
        var scale = 1 << zoom;

        minLon = TileXToLongitude(x, scale);
        maxLon = TileXToLongitude(x + 1, scale);
        minLat = TileYToLatitude(y + 1, scale);
        maxLat = TileYToLatitude(y, scale);
    }

    private static double TileXToLongitude(int x, int scale)
    {
        return x / (double)scale * 360.0 - 180.0;
    }

    private static double TileYToLatitude(int y, int scale)
    {
        var n = Math.PI - (2.0 * Math.PI * y) / scale;
        return Math.Atan(Math.Sinh(n)) * (180.0 / Math.PI);
    }

    private readonly struct TileRecord
    {
        public TileRecord(PmTilesTileAddress address, byte[] payload, bool isCompressed)
        {
            Address = address;
            Payload = payload;
            IsCompressed = isCompressed;
        }

        public PmTilesTileAddress Address { get; }

        public byte[] Payload { get; }

        public bool IsCompressed { get; }
    }
}

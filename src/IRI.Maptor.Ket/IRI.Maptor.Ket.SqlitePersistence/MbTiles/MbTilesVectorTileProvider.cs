using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.VectorTiles;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.SqlitePersistence.MbTiles;

/// <summary>
/// Shared access point for a vector MBTiles file. Owns a single <see cref="MbTilesReader"/> and a
/// bounded LRU cache of decoded tiles, so each physical tile is read from SQLite and decoded once
/// per extent regardless of how many per-layer data sources query it. SQLite reads are serialized
/// because the underlying connection is not thread-safe; decoding runs outside the lock.
/// </summary>
public sealed class MbTilesVectorTileProvider : IDisposable
{
    private sealed class CacheEntry
    {
        public long Key;
        public MvtTile? Tile;
    }

    private readonly MbTilesReader _reader;
    private readonly object _readerLock = new object();
    private readonly object _cacheLock = new object();
    private readonly int _cacheCapacity;
    private readonly Dictionary<long, LinkedListNode<CacheEntry>> _cache = new Dictionary<long, LinkedListNode<CacheEntry>>();
    private readonly LinkedList<CacheEntry> _lru = new LinkedList<CacheEntry>();

    private bool _disposed;

    public MbTilesMetadata? Metadata => _reader.Metadata;

    public List<int> AvailableZoomLevels { get; }

    public IReadOnlyList<MvtVectorLayerInfo> VectorLayers { get; }

    public BoundingBox WebMercatorExtent { get; }

    public MbTilesVectorTileProvider(string filePath, int cacheCapacity = 64)
    {
        _cacheCapacity = Math.Max(1, cacheCapacity);

        _reader = new MbTilesReader(filePath);
        _reader.Open();

        AvailableZoomLevels = _reader.GetZoomLevels();
        WebMercatorExtent = ComputeWebMercatorExtent();
        VectorLayers = LoadVectorLayers();
    }

    /// <summary>
    /// Returns the decoded tile at the given XYZ address (origin top-left), or null when the tile
    /// is absent. The XYZ row is converted to the MBTiles TMS row only at the database boundary.
    /// </summary>
    public MvtTile? GetDecodedTile(int zoom, int tileColumn, int xyzRow)
    {
        long key = TileKey(zoom, tileColumn, xyzRow);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return existing.Value.Tile;
            }
        }

        var tile = ReadAndDecode(zoom, tileColumn, xyzRow);

        lock (_cacheLock)
        {
            if (!_cache.ContainsKey(key))
            {
                var node = _lru.AddFirst(new CacheEntry { Key = key, Tile = tile });
                _cache[key] = node;

                if (_cache.Count > _cacheCapacity)
                {
                    var lru = _lru.Last;
                    if (lru != null)
                    {
                        _lru.RemoveLast();
                        _cache.Remove(lru.Value.Key);
                    }
                }
            }
        }

        return tile;
    }

    private MvtTile? ReadAndDecode(int zoom, int tileColumn, int xyzRow)
    {
        int tmsRow = ((1 << zoom) - 1) - xyzRow;

        byte[]? raw;
        lock (_readerLock)
        {
            raw = _reader.GetTile(zoom, tileColumn, tmsRow);
        }

        if (raw == null || raw.Length == 0)
            return null;

        var decompressed = MvtDecompressionHelper.Decompress(raw);
        return MvtTileReader.Decode(decompressed);
    }

    private MvtTile? DecodeSampleTile(int zoom)
    {
        byte[]? raw;
        lock (_readerLock)
        {
            raw = _reader.GetFirstTileData(zoom);
        }

        if (raw == null || raw.Length == 0)
            return null;

        return MvtTileReader.Decode(MvtDecompressionHelper.Decompress(raw));
    }

    private IReadOnlyList<MvtVectorLayerInfo> LoadVectorLayers()
    {
        var infos = new List<MvtVectorLayerInfo>();

        if (Metadata?.AdditionalMetadata != null &&
            Metadata.AdditionalMetadata.TryGetValue("json", out var json))
        {
            infos = MbTilesVectorMetadata.Parse(json);
        }

        // Geometry type is not in the metadata, and a single tile may not contain every layer.
        // Probe one tile per available zoom (ascending) until each layer's type is known.
        foreach (var zoom in AvailableZoomLevels ?? new List<int>())
        {
            if (infos.Count > 0 && infos.All(i => i.GeometryType != null))
                break;

            var sample = DecodeSampleTile(zoom);

            if (sample == null)
                continue;

            // Fallback: no vector_layers metadata -> enumerate layer names from a sample tile.
            if (infos.Count == 0)
            {
                infos = sample.Layers
                    .Select(l => new MvtVectorLayerInfo { Id = l.Name })
                    .ToList();
            }

            foreach (var info in infos)
            {
                if (info.GeometryType != null)
                    continue;

                var layer = sample.Layers.FirstOrDefault(l => l.Name == info.Id);
                var feature = layer?.Features.FirstOrDefault();

                if (feature != null)
                    info.GeometryType = ToGeometryType(feature.GeometryKind);
            }
        }

        // Default any still-unknown geometry type so the layer remains symbolizable
        // (SpatialModelMode != None) and gets a sensible legend icon.
        foreach (var info in infos)
        {
            info.GeometryType ??= GeometryType.Polygon;
        }

        return infos;
    }

    private BoundingBox ComputeWebMercatorExtent()
    {
        var wgs84 = _reader.GetBoundingBox();

        if (wgs84 != null)
        {
            var bottomLeft = MapProjects.GeodeticWgs84ToWebMercator(new Point(wgs84.Value.XMin, wgs84.Value.YMin));
            var topRight = MapProjects.GeodeticWgs84ToWebMercator(new Point(wgs84.Value.XMax, wgs84.Value.YMax));

            return new BoundingBox(bottomLeft.X, bottomLeft.Y, topRight.X, topRight.Y);
        }

        // No bounds metadata: derive the extent from tile coverage at the lowest zoom,
        // falling back to the whole world. Never NaN (which would break zoom-to-layer).
        return ComputeExtentFromTiles() ?? FullWorldExtent();
    }

    private BoundingBox? ComputeExtentFromTiles()
    {
        if (AvailableZoomLevels == null || AvailableZoomLevels.Count == 0)
            return null;

        int zoom = AvailableZoomLevels.Min();

        var bounds = _reader.GetTileBounds(zoom);

        if (bounds == null)
            return null;

        double max = MvtTileTransform.MaxExtent;
        int tileCount = 1 << zoom;
        double tileSpan = (2.0 * max) / tileCount;

        // tile_row is stored TMS (origin bottom); convert to XYZ (origin top) for the Y span.
        int xyzRowTop = (tileCount - 1) - bounds.Value.MaxRow;
        int xyzRowBottom = (tileCount - 1) - bounds.Value.MinRow;

        double xWest = -max + bounds.Value.MinColumn * tileSpan;
        double xEast = -max + (bounds.Value.MaxColumn + 1) * tileSpan;
        double yNorth = max - xyzRowTop * tileSpan;
        double ySouth = max - (xyzRowBottom + 1) * tileSpan;

        return new BoundingBox(xWest, ySouth, xEast, yNorth);
    }

    private static BoundingBox FullWorldExtent()
    {
        double max = MvtTileTransform.MaxExtent;
        return new BoundingBox(-max, -max, max, max);
    }

    private static GeometryType? ToGeometryType(MvtGeometryKind kind) => kind switch
    {
        MvtGeometryKind.Point => GeometryType.Point,
        MvtGeometryKind.LineString => GeometryType.LineString,
        MvtGeometryKind.Polygon => GeometryType.Polygon,
        _ => (GeometryType?)null,
    };

    private static long TileKey(int zoom, int column, int row) =>
        ((long)zoom << 58) | ((long)(column & 0x1FFFFFFF) << 29) | (long)(row & 0x1FFFFFFF);

    public void Dispose()
    {
        if (_disposed)
            return;

        _reader.Dispose();
        _disposed = true;
    }
}

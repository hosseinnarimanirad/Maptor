using IRI.Maptor.Infrastructure.Sqlite.MbTiles;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Model;
using IRI.Maptor.Core.SpatialReferenceSystem;

using Microsoft.Maui.Graphics;

using StaPoint = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// A raster (PNG/JPG) MBTiles file shown as a tile overlay on the map. It owns an
/// <see cref="MbTilesReader"/> and serves tile bytes for a given <see cref="TileInfo"/>, converting
/// the XYZ row the map uses into the TMS row MBTiles stores. Rendered by
/// <see cref="Controls.TileMapDrawable"/> as a tile grid (its <see cref="MapLayer.Parts"/> stays
/// empty).
/// </summary>
internal sealed class MbTilesRasterLayer : MapLayer, IDisposable
{
    // Web Mercator half-extent (meters); the world spans [-Max, +Max] in X and Y.
    private const double WebMercatorMax = 20037508.342789244;

    private readonly MbTilesReader _reader;
    private readonly object _lock = new();
    private readonly List<int> _zoomLevels;
    private bool _disposed;

    public MbTilesRasterLayer(string filePath, string name, Color color)
        : base(name, color)
    {
        LayerKey = filePath;

        _reader = new MbTilesReader(filePath);
        _reader.Open();

        _zoomLevels = _reader.GetZoomLevels();
        Extent = ComputeExtent();
        Description = "Raster (MBTiles)";
    }

    /// <summary>Cache key so tiles of different files/basemaps don't collide.</summary>
    public string LayerKey { get; }

    public IReadOnlyList<int> AvailableZoomLevels => _zoomLevels;

    /// <summary>The available zoom closest to <paramref name="mapZoom"/> (for graceful over/under-zoom).</summary>
    public int ClosestZoom(int mapZoom)
    {
        if (_zoomLevels.Count == 0)
        {
            return mapZoom;
        }

        return _zoomLevels.OrderBy(z => Math.Abs(z - mapZoom)).First();
    }

    /// <summary>Raw bytes (PNG/JPG) for the given XYZ tile, or null if absent. Thread-safe.</summary>
    public byte[]? GetTileBytes(TileInfo tile)
    {
        int zoom = tile.ZoomLevel;
        int tmsRow = ((1 << zoom) - 1) - tile.RowNumber;

        lock (_lock)
        {
            if (_disposed)
            {
                return null;
            }

            return _reader.GetTile(zoom, tile.ColumnNumber, tmsRow);
        }
    }

    private BoundingBox? ComputeExtent()
    {
        var wgs84 = _reader.GetBoundingBox();

        if (wgs84 != null)
        {
            var bottomLeft = MapProjects.GeodeticWgs84ToWebMercator(new StaPoint(wgs84.Value.XMin, wgs84.Value.YMin));
            var topRight = MapProjects.GeodeticWgs84ToWebMercator(new StaPoint(wgs84.Value.XMax, wgs84.Value.YMax));

            return new BoundingBox(bottomLeft.X, bottomLeft.Y, topRight.X, topRight.Y);
        }

        return ComputeExtentFromTiles();
    }

    // No bounds metadata: derive the extent from tile coverage at the lowest zoom.
    private BoundingBox? ComputeExtentFromTiles()
    {
        if (_zoomLevels.Count == 0)
        {
            return null;
        }

        int zoom = _zoomLevels.Min();

        var bounds = _reader.GetTileBounds(zoom);

        if (bounds == null)
        {
            return null;
        }

        int tileCount = 1 << zoom;
        double tileSpan = (2.0 * WebMercatorMax) / tileCount;

        // tile_row is stored TMS (origin bottom); convert to XYZ (origin top) for the Y span.
        int xyzRowTop = (tileCount - 1) - bounds.Value.MaxRow;
        int xyzRowBottom = (tileCount - 1) - bounds.Value.MinRow;

        double xWest = -WebMercatorMax + bounds.Value.MinColumn * tileSpan;
        double xEast = -WebMercatorMax + (bounds.Value.MaxColumn + 1) * tileSpan;
        double yNorth = WebMercatorMax - xyzRowTop * tileSpan;
        double ySouth = WebMercatorMax - (xyzRowBottom + 1) * tileSpan;

        return new BoundingBox(xWest, ySouth, xEast, yNorth);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _reader.Dispose();
        }
    }
}

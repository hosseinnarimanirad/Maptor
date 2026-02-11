using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

/// <summary>
/// Tile/Raster data source for OGC GeoPackage format
/// Provides access to tile layers stored in GeoPackage
/// </summary>
public class GeoPackageTileDataSource : RasterDataSource, IDisposable
{
    private readonly GpkgTileReader _reader;
    private readonly string _tableName;
    private GpkgLayerMetadata? _layerMetadata;
    private GpkgTileMatrixSet? _tileMatrixSet;
    private List<GpkgTileMatrix>? _tileMatrices;
    private List<int>? _availableZoomLevels;
    private bool _disposed;

    //public BoundingBox WebMercatorExtent { get; private set; }

    //public int Srid => SridHelper.WebMercator;

    /// <summary>
    /// Gets the layer metadata
    /// </summary>
    public GpkgLayerMetadata? LayerMetadata => _layerMetadata;

    /// <summary>
    /// Gets the tile matrix set information
    /// </summary>
    public GpkgTileMatrixSet? TileMatrixSet => _tileMatrixSet;

    /// <summary>
    /// Creates a new GeoPackage tile data source
    /// </summary>
    /// <param name="filePath">Path to the .gpkg file</param>
    /// <param name="tableName">Name of the tile table/layer to read</param>
    /// <param name="openImmediately">If true, opens the database immediately</param>
    public GeoPackageTileDataSource(string filePath, string tableName, bool openImmediately = true)
    {
        _reader = new GpkgTileReader(filePath);
        _tableName = tableName;

        if (openImmediately)
        {
            _reader.Open();
            Initialize();
        }
    }

    /// <summary>
    /// Opens the GeoPackage database if not already opened
    /// </summary>
    public void Open()
    {
        _reader.Open();
        Initialize();
    }

    private void Initialize()
    {
        // Get layer metadata
        var layers = _reader.GetTileLayers();
        _layerMetadata = layers.FirstOrDefault(l => l.TableName.Equals(_tableName, StringComparison.OrdinalIgnoreCase));

        if (_layerMetadata == null)
            throw new InvalidOperationException($"Tile layer not found: {_tableName}");

        // Get tile matrix set
        _tileMatrixSet = _reader.GetTileMatrixSet(_tableName);

        if (_tileMatrixSet == null)
            throw new InvalidOperationException($"No tile matrix set found for layer: {_tableName}");

        // Get tile matrices (zoom levels)
        _tileMatrices = _reader.GetTileMatrices(_tableName);
        _availableZoomLevels = _tileMatrices.Select(m => m.ZoomLevel).ToList();

        // Set extent from tile matrix set
        var bbox = new BoundingBox(
            _tileMatrixSet.MinX,
            _tileMatrixSet.MinY,
            _tileMatrixSet.MaxX,
            _tileMatrixSet.MaxY);

        // If SRS is not Web Mercator, might need transformation
        // For now, assume Web Mercator or handle as-is
        WebMercatorExtent = bbox;
    }

    /// <summary>
    /// Gets tiles for the specified geographic bounding box and map scale
    /// </summary>
    /// <param name="geographicBoundingBox">Bounding box in geographic coordinates</param>
    /// <param name="mapScale">Map scale to determine appropriate zoom level</param>
    /// <returns>List of geo-referenced tile images</returns>
    public List<GeoReferencedImage> GetTiles(BoundingBox geographicBoundingBox, double mapScale)
    {
        if (_availableZoomLevels == null || !_availableZoomLevels.Any())
            return new List<GeoReferencedImage>();

        int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale);

        // Find the closest available zoom level
        zoomLevel = GetClosestAvailableZoomLevel(zoomLevel);

        var result = new List<GeoReferencedImage>();

        // Get tile matrix for this zoom level
        var tileMatrix = _tileMatrices?.FirstOrDefault(m => m.ZoomLevel == zoomLevel);
        if (tileMatrix == null)
            return result;

        // Calculate tile coordinates for the bounding box
        var lowerLeft = WebMercatorUtility.LatLonToImageNumber(
            geographicBoundingBox.YMin,
            geographicBoundingBox.XMin,
            zoomLevel);

        var upperRight = WebMercatorUtility.LatLonToImageNumber(
            geographicBoundingBox.YMax,
            geographicBoundingBox.XMax,
            zoomLevel);

        // GeoPackage uses different Y-axis orientation than standard XYZ tiles
        // GeoPackage: Y=0 is at top (like XYZ), not bottom (like TMS)
        for (int x = (int)lowerLeft.X; x <= upperRight.X; x++)
        {
            for (int y = (int)upperRight.Y; y <= lowerLeft.Y; y++)
            {
                var tileData = _reader.GetTile(_tableName, zoomLevel, x, y);

                if (tileData != null && tileData.Length > 0)
                {
                    result.Add(new GeoReferencedImage(
                        tileData,
                        WebMercatorUtility.GetWgs84ImageBoundingBox(y, x, zoomLevel)));
                }
            }
        }

        System.Diagnostics.Trace.WriteLine($"GeoPackage: {result.Count} tiles loaded from {_tableName} at zoom level {zoomLevel}");

        return result;
    }

    /// <summary>
    /// Gets tiles for export to formats like Google Earth KML
    /// </summary>
    public List<GeoReferencedImage> GetTilesForGoogleEarth(BoundingBox geographicBoundingBox, double mapScale)
    {
        return GetTiles(geographicBoundingBox, mapScale);
    }

    /// <summary>
    /// Gets a specific tile by zoom, column, and row
    /// </summary>
    /// <param name="zoom">Zoom level</param>
    /// <param name="column">Tile column (X)</param>
    /// <param name="row">Tile row (Y)</param>
    /// <returns>Tile data as byte array, or null if not found</returns>
    public byte[]? GetTile(int zoom, int column, int row)
    {
        return _reader.GetTile(_tableName, zoom, column, row);
    }

    /// <summary>
    /// Gets the available zoom levels in this tile layer
    /// </summary>
    public List<int> GetAvailableZoomLevels()
    {
        return _availableZoomLevels ?? new List<int>();
    }

    /// <summary>
    /// Gets the total number of tiles in the layer
    /// </summary>
    public long GetTileCount(int? zoomLevel = null)
    {
        return _reader.GetTileCount(_tableName, zoomLevel);
    }

    /// <summary>
    /// Gets the zoom level range (min and max)
    /// </summary>
    public (int minZoom, int maxZoom)? GetZoomRange()
    {
        return _reader.GetZoomRange(_tableName);
    }

    /// <summary>
    /// Finds the closest available zoom level to the requested one
    /// </summary>
    private int GetClosestAvailableZoomLevel(int requestedZoom)
    {
        if (_availableZoomLevels == null || !_availableZoomLevels.Any())
            return requestedZoom;

        return _availableZoomLevels
            .OrderBy(z => Math.Abs(z - requestedZoom))
            .First();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reader?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~GeoPackageTileDataSource()
    {
        Dispose();
    }

    public override string ToString()
    {
        var zoomRange = GetZoomRange();
        return $"GeoPackageTileDataSource: {_tableName} (Zoom: {zoomRange?.minZoom}-{zoomRange?.maxZoom})";
    }
}


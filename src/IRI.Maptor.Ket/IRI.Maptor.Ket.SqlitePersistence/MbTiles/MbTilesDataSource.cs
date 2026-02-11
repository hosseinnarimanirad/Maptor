using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.SqlitePersistence.MbTiles;

/// <summary>
/// Data source for MBTiles format - provides tile-based raster data from SQLite database
/// MBTiles uses TMS (Tile Map Service) coordinate scheme where origin is bottom-left
/// </summary>
public class MbTilesDataSource : RasterDataSource, IDisposable
{
    private readonly MbTilesReader _reader;
    private List<int>? _availableZoomLevels;
    private bool _disposed;

    //public BoundingBox WebMercatorExtent { get; private set; }

    //public int Srid => SridHelper.WebMercator;

    public MbTilesMetadata? Metadata => _reader.Metadata;

    /// <summary>
    /// Creates a new MBTiles data source
    /// </summary>
    /// <param name="filePath">Path to the .mbtiles file</param>
    /// <param name="openImmediately">If true, opens the database immediately</param>
    public MbTilesDataSource(string filePath, bool openImmediately = true)
    {
        _reader = new MbTilesReader(filePath);

        if (openImmediately)
        {
            _reader.Open();
            Initialize();
        }
    }

    /// <summary>
    /// Opens the MBTiles database if not already opened
    /// </summary>
    public void Open()
    {
        _reader.Open();
        Initialize();
    }

    private void Initialize()
    {
        _availableZoomLevels = _reader.GetZoomLevels();

        // Get bounding box from metadata
        var bbox = _reader.GetBoundingBox();

        if (bbox != null)
        {
            // Transform from WGS84 to Web Mercator
            WebMercatorExtent = TransformToWebMercator(bbox.Value);
        }
        else
        {
            // Default to world extent if no bounds in metadata
            WebMercatorExtent = BoundingBox.NaN;
        }
    }

    /// <summary>
    /// Gets tiles for the specified geographic bounding box and map scale
    /// </summary>
    /// <param name="geographicBoundingBox">Bounding box in WGS84 (geographic coordinates)</param>
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

        // Calculate tile coordinates for the bounding box
        var lowerLeft = WebMercatorUtility.LatLonToImageNumber(
            geographicBoundingBox.YMin, 
            geographicBoundingBox.XMin, 
            zoomLevel);

        var upperRight = WebMercatorUtility.LatLonToImageNumber(
            geographicBoundingBox.YMax, 
            geographicBoundingBox.XMax, 
            zoomLevel);

        // MBTiles uses TMS scheme (Y origin at bottom), but we need to convert from XYZ (Y origin at top)
        int maxTileIndex = (1 << zoomLevel) - 1; // 2^zoom - 1

        for (int x = (int)lowerLeft.X; x <= upperRight.X; x++)
        {
            for (int y = (int)upperRight.Y; y <= lowerLeft.Y; y++)
            {
                // Convert from XYZ to TMS
                int tmsY = maxTileIndex - y;

                var tileData = _reader.GetTile(zoomLevel, x, tmsY);

                if (tileData != null && tileData.Length > 0)
                {
                    result.Add(new GeoReferencedImage(
                        tileData,
                        WebMercatorUtility.GetWgs84ImageBoundingBox(y, x, zoomLevel)));
                }
            }
        }

        System.Diagnostics.Trace.WriteLine($"MBTiles: {result.Count} tiles loaded at zoom level {zoomLevel}");

        return result;
    }

    /// <summary>
    /// Gets tiles for export to formats like Google Earth KML
    /// </summary>
    public List<GeoReferencedImage> GetTilesForGoogleEarth(BoundingBox geographicBoundingBox, double mapScale)
    {
        // Same implementation as GetTiles for MBTiles
        return GetTiles(geographicBoundingBox, mapScale);
    }

    /// <summary>
    /// Gets a specific tile by zoom, column, and row (TMS scheme)
    /// </summary>
    /// <param name="zoom">Zoom level</param>
    /// <param name="column">Tile column (X)</param>
    /// <param name="row">Tile row (Y) in TMS scheme (origin bottom-left)</param>
    /// <returns>Tile data as byte array, or null if not found</returns>
    public byte[]? GetTile(int zoom, int column, int row)
    {
        return _reader.GetTile(zoom, column, row);
    }

    /// <summary>
    /// Gets the available zoom levels in this MBTiles database
    /// </summary>
    public List<int> GetAvailableZoomLevels()
    {
        return _availableZoomLevels ?? new List<int>();
    }

    /// <summary>
    /// Gets the total number of tiles in the database
    /// </summary>
    public long GetTileCount(int? zoomLevel = null)
    {
        return _reader.GetTileCount(zoomLevel);
    }

    /// <summary>
    /// Finds the closest available zoom level to the requested one
    /// </summary>
    private int GetClosestAvailableZoomLevel(int requestedZoom)
    {
        if (_availableZoomLevels == null || !_availableZoomLevels.Any())
            return requestedZoom;

        // Return the closest available zoom level
        return _availableZoomLevels
            .OrderBy(z => Math.Abs(z - requestedZoom))
            .First();
    }

    /// <summary>
    /// Transforms a WGS84 bounding box to Web Mercator
    /// </summary>
    private BoundingBox TransformToWebMercator(BoundingBox wgs84Box)
    {
        var bottomLeft = MapProjects.GeodeticWgs84ToWebMercator(
            new Sta.Common.Primitives.Point(wgs84Box.XMin, wgs84Box.YMin));

        var topRight = MapProjects.GeodeticWgs84ToWebMercator(
            new Sta.Common.Primitives.Point(wgs84Box.XMax, wgs84Box.YMax));

        return new BoundingBox(
            bottomLeft.X, 
            bottomLeft.Y, 
            topRight.X, 
            topRight.Y);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reader?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~MbTilesDataSource()
    {
        Dispose();
    }

    public override string ToString()
    {
        return $"MBTilesDataSource: {Metadata?.Name ?? "Unnamed"} (Zoom: {Metadata?.MinZoom}-{Metadata?.MaxZoom})";
    }
}


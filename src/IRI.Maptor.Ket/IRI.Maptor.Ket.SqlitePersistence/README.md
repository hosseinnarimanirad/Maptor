# IRI.Maptor.Ket.SqlitePersistence

SQLite-based geospatial format support for the Maptor library.

## Overview

This package provides support for reading SQLite-based geospatial formats:
- **MBTiles** - Tile-based map storage format for offline mapping
- **OGC GeoPackage** - Universal geospatial data container (vector features and raster tiles)

Both formats are cross-platform compatible and work seamlessly with .NET 8, MAUI, and mobile platforms (Android, iOS, Windows, macOS).

## Supported Formats

### 🗺️ MBTiles

MBTiles is a specification for storing tiled map data in SQLite databases for immediate use and for transfer. It's optimized for offline mapping applications.

**Features:**
- Raster tiles (PNG, JPEG, WebP)
- Vector tiles (PBF)
- Metadata support (name, description, bounds, attribution)
- TMS (Tile Map Service) coordinate scheme
- Efficient storage with SQLite compression

**Specification:** [MBTiles Spec](https://github.com/mapbox/mbtiles-spec)

### 📦 GeoPackage (GPKG)

GeoPackage is an OGC standard for geospatial data exchange. It can store multiple types of data in a single file:
- **Vector features** (points, lines, polygons)
- **Raster tiles** and coverage data
- **Attributes and metadata**
- **Multiple layers** in a single file

**Features:**
- OGC standard format
- Cross-platform compatibility
- Rich metadata support
- Spatial indexing (R-Tree)
- Multiple coordinate reference systems

**Specification:** [OGC GeoPackage](https://www.geopackage.org/)

## Installation

```bash
dotnet add package IRI.Maptor.Ket.SqlitePersistence
```

## Usage Examples

### MBTiles - Reading Tiles

```csharp
using IRI.Maptor.Ket.SqlitePersistence.MbTiles;
using IRI.Maptor.Sta.Common.Primitives;

// Create and open MBTiles reader
using var reader = new MbTilesReader("path/to/map.mbtiles");
reader.Open();

// Get metadata
var metadata = reader.Metadata;
Console.WriteLine($"Name: {metadata?.Name}");
Console.WriteLine($"Format: {metadata?.Format}");
Console.WriteLine($"Zoom Range: {metadata?.MinZoom} - {metadata?.MaxZoom}");
Console.WriteLine($"Bounds: {metadata?.Bounds}");

// Get available zoom levels
var zoomLevels = reader.GetZoomLevels();
Console.WriteLine($"Available zooms: {string.Join(", ", zoomLevels)}");

// Get a specific tile (zoom, column, row in TMS scheme)
byte[]? tileData = reader.GetTile(zoom: 10, column: 512, row: 384);
if (tileData != null)
{
    // Save tile to file or display
    File.WriteAllBytes("tile.png", tileData);
}

// Get tile count
long totalTiles = reader.GetTileCount();
long tilesAtZoom10 = reader.GetTileCount(zoomLevel: 10);

// Get bounding box
BoundingBox? bbox = reader.GetBoundingBox();
```

### MBTiles - Using Data Source

```csharp
using IRI.Maptor.Ket.SqlitePersistence.MbTiles;
using IRI.Maptor.Sta.Common.Primitives;

// Create MBTiles data source (for use with map viewers)
using var dataSource = new MbTilesDataSource("path/to/map.mbtiles");

// Get tiles for a specific area and map scale
var boundingBox = new BoundingBox(
    xMin: -74.01, yMin: 40.70,  // New York City area
    xMax: -73.99, yMax: 40.72
);

double mapScale = 10000; // Adjust based on your zoom level

var tiles = dataSource.GetTiles(boundingBox, mapScale);

Console.WriteLine($"Loaded {tiles.Count} tiles");

foreach (var tile in tiles)
{
    Console.WriteLine($"Tile: {tile.ImageBytes.Length} bytes, Bounds: {tile.BoundingBox}");
}

// Get available zoom levels
var availableZooms = dataSource.GetAvailableZoomLevels();

// Get specific tile
byte[]? specificTile = dataSource.GetTile(zoom: 10, column: 512, row: 384);
```

### GeoPackage - Reading Vector Features

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;
using IRI.Maptor.Sta.Common.Primitives;

// Create and open GeoPackage vector reader
using var reader = new GpkgVectorReader("path/to/data.gpkg");
reader.Open();

// Get all feature layers
var layers = reader.GetFeatureLayers();
foreach (var layer in layers)
{
    Console.WriteLine($"Layer: {layer.TableName}");
    Console.WriteLine($"  Type: {layer.DataType}");
    Console.WriteLine($"  Description: {layer.Description}");
    Console.WriteLine($"  Bounds: ({layer.MinX}, {layer.MinY}) to ({layer.MaxX}, {layer.MaxY})");
}

// Get geometry column information
var geometryInfo = reader.GetGeometryColumnInfo("countries");
Console.WriteLine($"Geometry Column: {geometryInfo?.ColumnName}");
Console.WriteLine($"Geometry Type: {geometryInfo?.GeometryTypeName}");
Console.WriteLine($"SRID: {geometryInfo?.SrsId}");

// Read all features from a layer
var features = reader.ReadFeatures("countries");
Console.WriteLine($"Loaded {features.Count} features");

foreach (var feature in features)
{
    Console.WriteLine($"Geometry Type: {feature.TheGeometry?.Type}");
    
    // Access attributes
    if (feature.Attributes != null)
    {
        foreach (var attr in feature.Attributes)
        {
            Console.WriteLine($"  {attr.Key}: {attr.Value}");
        }
    }
}

// Read features within a bounding box
var bbox = new BoundingBox(-10, 35, 5, 45); // Approximate bounds for Western Europe
var filteredFeatures = reader.ReadFeatures("countries", bbox);

// Get feature count
long featureCount = reader.GetFeatureCount("countries");

// Get spatial reference systems
var srsList = reader.GetSpatialReferenceSystems();
```

### GeoPackage - Using Vector Data Source

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;
using IRI.Maptor.Sta.Common.Primitives;

// Create GeoPackage vector data source
using var dataSource = new GeoPackageDataSource("path/to/data.gpkg", "countries");

// Get all features as FeatureSet
var featureSet = await dataSource.GetAsFeatureSetAsync();
Console.WriteLine($"Total features: {featureSet.Features.Count}");
Console.WriteLine($"SRID: {featureSet.Srid}");

// Get features within a bounding box
var bbox = new BoundingBox(-10, 35, 5, 45);
var filteredFeatureSet = await dataSource.GetAsFeatureSetAsync(bbox);

// Search features by text
var searchResults = await dataSource.SearchAsync("Germany");

// Get layer metadata
var metadata = dataSource.LayerMetadata;
Console.WriteLine($"Layer: {metadata?.TableName}");
Console.WriteLine($"Description: {metadata?.Description}");

// Get geometry column info
var geomCol = dataSource.GeometryColumn;
Console.WriteLine($"Geometry: {geomCol?.GeometryTypeName}");

// Get feature count
long count = dataSource.GetFeatureCount();
```

### GeoPackage - Reading Tile Layers

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

// Create and open GeoPackage tile reader
using var reader = new GpkgTileReader("path/to/tiles.gpkg");
reader.Open();

// Get all tile layers
var tileLayers = reader.GetTileLayers();
foreach (var layer in tileLayers)
{
    Console.WriteLine($"Tile Layer: {layer.TableName}");
    Console.WriteLine($"  Description: {layer.Description}");
}

// Get tile matrix set (pyramid information)
var tileMatrixSet = reader.GetTileMatrixSet("satellite_tiles");
Console.WriteLine($"SRS ID: {tileMatrixSet?.SrsId}");
Console.WriteLine($"Bounds: {tileMatrixSet?.MinX},{tileMatrixSet?.MinY} to {tileMatrixSet?.MaxX},{tileMatrixSet?.MaxY}");

// Get all zoom levels (tile matrices)
var matrices = reader.GetTileMatrices("satellite_tiles");
foreach (var matrix in matrices)
{
    Console.WriteLine($"Zoom {matrix.ZoomLevel}: {matrix.MatrixWidth}x{matrix.MatrixHeight} tiles");
    Console.WriteLine($"  Tile size: {matrix.TileWidth}x{matrix.TileHeight} pixels");
}

// Get a specific tile
byte[]? tile = reader.GetTile("satellite_tiles", zoom: 10, column: 512, row: 384);

// Get available zoom levels
var zooms = reader.GetZoomLevels("satellite_tiles");

// Get tile count
long totalTiles = reader.GetTileCount("satellite_tiles");
long tilesAtZoom = reader.GetTileCount("satellite_tiles", zoomLevel: 10);

// Get zoom range
var (minZoom, maxZoom) = reader.GetZoomRange("satellite_tiles") ?? (0, 0);
```

### GeoPackage - Using Tile Data Source

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;
using IRI.Maptor.Sta.Common.Primitives;

// Create GeoPackage tile data source
using var dataSource = new GeoPackageTileDataSource("path/to/tiles.gpkg", "satellite_tiles");

// Get tiles for a specific area and map scale
var bbox = new BoundingBox(-74.01, 40.70, -73.99, 40.72); // NYC
double mapScale = 10000;

var tiles = dataSource.GetTiles(bbox, mapScale);
Console.WriteLine($"Loaded {tiles.Count} tiles");

// Get available zoom levels
var zoomLevels = dataSource.GetAvailableZoomLevels();

// Get specific tile
byte[]? tile = dataSource.GetTile(zoom: 10, column: 512, row: 384);

// Get metadata
var metadata = dataSource.LayerMetadata;
var tileMatrixSet = dataSource.TileMatrixSet;

// Get zoom range
var zoomRange = dataSource.GetZoomRange();
Console.WriteLine($"Zoom range: {zoomRange?.minZoom} - {zoomRange?.maxZoom}");
```

## Coordinate Systems

### MBTiles
- Uses **TMS (Tile Map Service)** coordinate scheme
- Y-axis origin at **bottom-left** (row 0 is at the bottom)
- Tiles are typically in **Web Mercator (EPSG:3857)**
- Bounds are stored in **WGS84 (EPSG:4326)** format

### GeoPackage
- Uses **XYZ** tile scheme for tiles (Y-axis origin at top-left)
- Supports **multiple coordinate reference systems** (CRS)
- Check `srs_id` in metadata for the coordinate system
- Common systems:
  - `4326` - WGS84 (Geographic)
  - `3857` - Web Mercator
  - `900913` - Google Web Mercator (legacy)

## Converting Between Tile Schemes

```csharp
// Converting from XYZ to TMS
int xyzY = 384;
int zoom = 10;
int maxTileIndex = (1 << zoom) - 1; // 2^zoom - 1
int tmsY = maxTileIndex - xyzY;

// Converting from TMS to XYZ
int tmsYValue = 639;
int xyzYValue = maxTileIndex - tmsYValue;
```

## Async Operations

Both readers support async operations:

```csharp
// MBTiles async
using var mbReader = new MbTilesReader("map.mbtiles");
await mbReader.OpenAsync();
byte[]? tile = await mbReader.GetTileAsync(10, 512, 384);

// GeoPackage async
using var gpReader = new GpkgVectorReader("data.gpkg");
await gpReader.OpenAsync();

using var tileReader = new GpkgTileReader("tiles.gpkg");
await tileReader.OpenAsync();
byte[]? gpTile = await tileReader.GetTileAsync("layer", 10, 512, 384);
```

## Platform Support

✅ **.NET 8+**  
✅ **MAUI (Android, iOS, Windows, macOS)**  
✅ **Desktop (Windows, Linux, macOS)**  
✅ **Mobile (Android, iOS)**

## Dependencies

- `Microsoft.Data.Sqlite.Core` - Modern SQLite ADO.NET provider
- `SQLitePCLRaw.bundle_e_sqlite3` - Native SQLite binaries for all platforms
- `IRI.Maptor.Sta.Persistence` - Maptor persistence abstractions
- `IRI.Maptor.Sta.Spatial` - Maptor spatial types and algorithms
- `IRI.Maptor.Sta.Ogc` - OGC standard implementations

## Performance Tips

1. **Use spatial indexes**: Both formats support spatial indexing (R-Tree) for fast queries
2. **Batch operations**: When reading multiple tiles, consider parallel processing
3. **Connection pooling**: Reuse readers/data sources instead of creating new ones
4. **Bounding box queries**: Use bounding box filtering to limit data transfer
5. **Appropriate zoom levels**: Request tiles at appropriate zoom levels for your map scale

## Validation

```csharp
// Validate MBTiles schema
using var mbReader = new MbTilesReader("map.mbtiles");
mbReader.Open();
bool isMbTilesValid = mbReader.ValidateSchema();

// Validate GeoPackage schema
using var gpReader = new GpkgVectorReader("data.gpkg");
gpReader.Open();
bool isGpkgValid = gpReader.ValidateSchema();
```

## Error Handling

```csharp
try
{
    using var reader = new MbTilesReader("map.mbtiles");
    reader.Open();
    
    var tile = reader.GetTile(10, 512, 384);
    if (tile == null)
    {
        Console.WriteLine("Tile not found");
    }
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid operation: {ex.Message}");
}
catch (SqliteException ex)
{
    Console.WriteLine($"SQLite error: {ex.Message}");
}
```

## Best Practices

1. **Dispose properly**: Always use `using` statements or call `Dispose()` to release resources
2. **Check for null**: Tiles and features may not exist, always check for null
3. **Validate files**: Use `ValidateSchema()` to ensure files are valid before processing
4. **Handle exceptions**: Wrap file operations in try-catch blocks
5. **Use async for UI apps**: Use async methods in UI applications to avoid blocking

## Contributing

Contributions are welcome! Please see the [Maptor Contributing Guide](https://github.com/hosseinnarimanirad/Maptor/blob/master/CONTRIBUTING.md).

## License

This package is part of the Maptor library and is licensed under the [MIT License](https://github.com/hosseinnarimanirad/Maptor/blob/master/LICENSE.txt).

## Resources

- [MBTiles Specification](https://github.com/mapbox/mbtiles-spec)
- [OGC GeoPackage Standard](https://www.geopackage.org/)
- [Maptor Documentation](https://github.com/hosseinnarimanirad/Maptor)
- [SQLite Documentation](https://www.sqlite.org/docs.html)

## Support

- 📖 [Documentation](https://github.com/hosseinnarimanirad/Maptor/wiki)
- 🐛 [Report Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
- 💬 [Discussions](https://github.com/hosseinnarimanirad/Maptor/discussions)


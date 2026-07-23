# IRI.Maptor.Ket.SqlitePersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlitePersistence?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlitePersistence/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

SQLite-based geospatial format support for the Maptor library: readers and map data sources for
MBTiles (tiled map storage for offline mapping) and OGC GeoPackage (vector features and raster
tiles in a single container). Both formats are cross-platform and work with .NET 8, MAUI, and
mobile platforms.

## Installation

```bash
dotnet add package IRI.Maptor.Ket.SqlitePersistence
```

## Features

- `MbTilesReader` — read MBTiles files: metadata, tiles (raster PNG/JPEG/WebP or vector PBF), zoom levels, tile counts, bounds, schema validation
- `MbTilesDataSource` — MBTiles-backed raster data source for map viewers (tiles by bounding box and map scale)
- `GpkgVectorReader` — read GeoPackage vector layers: layer metadata, geometry column info, features (optionally filtered by bounding box), feature counts, spatial reference systems
- `GeoPackageDataSource` — GeoPackage-backed vector data source (`FeatureSet` loading, bounding-box filtering, text search)
- `GpkgTileReader` — read GeoPackage tile layers: tile matrix sets, tile matrices, tiles, zoom levels and ranges
- `GeoPackageTileDataSource` — GeoPackage-backed tile data source for map viewers
- Async variants of the read operations (`OpenAsync`, `GetTileAsync`, `ReadFeaturesAsync`)

## Usage

### MBTiles - reading tiles

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

// Get a specific tile (zoom, column, row in TMS scheme)
byte[]? tileData = reader.GetTile(zoom: 10, column: 512, row: 384);
if (tileData != null)
{
    File.WriteAllBytes("tile.png", tileData);
}

// Get tile count
long totalTiles = reader.GetTileCount();
long tilesAtZoom10 = reader.GetTileCount(zoomLevel: 10);

// Get bounding box
BoundingBox? bbox = reader.GetBoundingBox();
```

### MBTiles - using the data source

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

double mapScale = 10000;

var tiles = dataSource.GetTiles(boundingBox, mapScale);

foreach (var tile in tiles)
{
    Console.WriteLine($"Tile: {tile.ImageBytes.Length} bytes, Bounds: {tile.BoundingBox}");
}

// Get available zoom levels
var availableZooms = dataSource.GetAvailableZoomLevels();

// Get specific tile
byte[]? specificTile = dataSource.GetTile(zoom: 10, column: 512, row: 384);
```

### GeoPackage - reading vector features

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
    Console.WriteLine($"  Bounds: ({layer.MinX}, {layer.MinY}) to ({layer.MaxX}, {layer.MaxY})");
}

// Get geometry column information
var geometryInfo = reader.GetGeometryColumnInfo("countries");
Console.WriteLine($"Geometry Column: {geometryInfo?.ColumnName}");
Console.WriteLine($"Geometry Type: {geometryInfo?.GeometryTypeName}");
Console.WriteLine($"SRID: {geometryInfo?.SrsId}");

// Read all features from a layer
var features = reader.ReadFeatures("countries");

foreach (var feature in features)
{
    Console.WriteLine($"Geometry Type: {feature.TheGeometry?.Type}");

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

### GeoPackage - using the vector data source

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;
using IRI.Maptor.Sta.Common.Primitives;

// Create GeoPackage vector data source
using var dataSource = new GeoPackageDataSource("path/to/data.gpkg", "countries");

// Get all features as FeatureSet
var featureSet = await dataSource.GetAsFeatureSetAsync();
Console.WriteLine($"Total features: {featureSet.Features.Count}");

// Get features within a bounding box
var bbox = new BoundingBox(-10, 35, 5, 45);
var filteredFeatureSet = await dataSource.GetAsFeatureSetAsync(bbox);

// Search features by text
var searchResults = await dataSource.SearchAsync("Germany");

// Get layer metadata and geometry column info
var metadata = dataSource.LayerMetadata;
var geomCol = dataSource.GeometryColumn;

// Get feature count
long count = dataSource.GetFeatureCount();
```

### GeoPackage - reading tile layers

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

// Create and open GeoPackage tile reader
using var reader = new GpkgTileReader("path/to/tiles.gpkg");
reader.Open();

// Get all tile layers
var tileLayers = reader.GetTileLayers();

// Get tile matrix set (pyramid information)
var tileMatrixSet = reader.GetTileMatrixSet("satellite_tiles");
Console.WriteLine($"SRS ID: {tileMatrixSet?.SrsId}");

// Get all zoom levels (tile matrices)
var matrices = reader.GetTileMatrices("satellite_tiles");
foreach (var matrix in matrices)
{
    Console.WriteLine($"Zoom {matrix.ZoomLevel}: {matrix.MatrixWidth}x{matrix.MatrixHeight} tiles");
    Console.WriteLine($"  Tile size: {matrix.TileWidth}x{matrix.TileHeight} pixels");
}

// Get a specific tile
byte[]? tile = reader.GetTile("satellite_tiles", zoom: 10, column: 512, row: 384);

// Get available zoom levels, tile counts, and zoom range
var zooms = reader.GetZoomLevels("satellite_tiles");
long totalTiles = reader.GetTileCount("satellite_tiles");
var (minZoom, maxZoom) = reader.GetZoomRange("satellite_tiles") ?? (0, 0);
```

### GeoPackage - using the tile data source

```csharp
using IRI.Maptor.Ket.SqlitePersistence.GeoPackage;
using IRI.Maptor.Sta.Common.Primitives;

// Create GeoPackage tile data source
using var dataSource = new GeoPackageTileDataSource("path/to/tiles.gpkg", "satellite_tiles");

// Get tiles for a specific area and map scale
var bbox = new BoundingBox(-74.01, 40.70, -73.99, 40.72); // NYC
double mapScale = 10000;

var tiles = dataSource.GetTiles(bbox, mapScale);

// Get available zoom levels
var zoomLevels = dataSource.GetAvailableZoomLevels();

// Get specific tile
byte[]? tile = dataSource.GetTile(zoom: 10, column: 512, row: 384);

// Get metadata
var metadata = dataSource.LayerMetadata;
var tileMatrixSet = dataSource.TileMatrixSet;

// Get zoom range
var zoomRange = dataSource.GetZoomRange();
```

## Coordinate systems

MBTiles:

- Uses the TMS (Tile Map Service) coordinate scheme
- Y-axis origin at bottom-left (row 0 is at the bottom)
- Tiles are typically in Web Mercator (EPSG:3857)
- Bounds are stored in WGS84 (EPSG:4326)

GeoPackage:

- Uses the XYZ tile scheme for tiles (Y-axis origin at top-left)
- Supports multiple coordinate reference systems (check `srs_id` in the metadata)
- Common systems: `4326` (WGS84 geographic), `3857` (Web Mercator), `900913` (legacy Google Web Mercator)

## Converting between tile schemes

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

## Async operations

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

## Dependencies

- `Microsoft.Data.Sqlite.Core` - SQLite ADO.NET provider
- `SQLitePCLRaw.bundle_e_sqlite3` - native SQLite binaries for all platforms
- `IRI.Maptor.Sta.Persistence` - Maptor persistence abstractions
- `IRI.Maptor.Sta.Spatial` - Maptor spatial types and algorithms
- `IRI.Maptor.Sta.Ogc` - OGC standard implementations

## References

- [MBTiles specification](https://github.com/mapbox/mbtiles-spec)
- [OGC GeoPackage standard](https://www.geopackage.org/)

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlitePersistence/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Ket](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Ket/README.md)

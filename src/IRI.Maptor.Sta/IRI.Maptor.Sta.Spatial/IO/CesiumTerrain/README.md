# Cesium Quantized-Mesh Terrain Format Support

This module provides support for reading Cesium Quantized-Mesh terrain tiles (`.terrain` files), which are used for efficient 3D terrain visualization in web-based mapping applications.

## 📋 Format Overview

**Cesium Quantized-Mesh** is a binary format for terrain data that provides:
- **Efficient compression** through quantization and delta encoding
- **Level-of-detail (LOD)** support via tile pyramids
- **Fast rendering** with optimized triangle meshes
- **Tile stitching** through edge indices
- **Optional extensions** (water masks, vertex normals, metadata)

## 🔧 Usage

### Basic Reading

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Read a .terrain file
var terrainData = QuantizedMeshReader.Read("path/to/tile/15/12345/67890.terrain");

// Access header information
Console.WriteLine($"Min Height: {terrainData.Header.MinimumHeight}m");
Console.WriteLine($"Max Height: {terrainData.Header.MaximumHeight}m");
Console.WriteLine($"Vertices: {terrainData.VertexCount}");
Console.WriteLine($"Triangles: {terrainData.TriangleCount}");

// Access quantized vertex data
for (int i = 0; i < terrainData.VertexCount; i++)
{
    double u = terrainData.GetNormalizedU(i);      // [0, 1]
    double v = terrainData.GetNormalizedV(i);      // [0, 1]
    double height = terrainData.GetHeight(i);      // meters
}

// Access triangle indices
for (int i = 0; i < terrainData.Indices.Length; i += 3)
{
    uint v0 = terrainData.Indices[i];
    uint v1 = terrainData.Indices[i + 1];
    uint v2 = terrainData.Indices[i + 2];
    // Process triangle
}
```

### Working with Tile Coordinates

```csharp
// Create a tile coordinate
var tileCoord = new TerrainTileCoordinate(level: 15, x: 12345, y: 67890);

// Get the file path
string fileName = tileCoord.GetFileName(); // "15/12345/67890.terrain"

// Get geographic bounds (WGS84)
var (west, south, east, north) = tileCoord.GetBoundingBox();

// Navigate the tile hierarchy
var parent = tileCoord.GetParent();
var children = tileCoord.GetChildren();

// Parse from path
var coord = TerrainTileCoordinate.FromPath("data/terrain/15/12345/67890.terrain");
```

### Working with Extensions

```csharp
var terrainData = QuantizedMeshReader.Read("tile.terrain");

if (terrainData.Extensions != null)
{
    // Check for water mask
    if (terrainData.Extensions.HasWaterMask)
    {
        byte[] waterMask = terrainData.Extensions.WaterMask;
        // Process water mask (0 = land, 255 = water)
    }

    // Check for vertex normals
    if (terrainData.Extensions.HasVertexNormals)
    {
        byte[] normals = terrainData.Extensions.VertexNormals;
        
        // Decode oct-encoded normals (2 bytes per vertex)
        for (int i = 0; i < terrainData.VertexCount; i++)
        {
            byte octX = normals[i * 2];
            byte octY = normals[i * 2 + 1];
            
            var (nx, ny, nz) = QuantizedMeshReader.DecodeOctNormal(octX, octY);
            // Use normal vector for lighting calculations
        }
    }

    // Check for metadata
    if (terrainData.Extensions.HasMetadata)
    {
        string metadata = terrainData.Extensions.Metadata;
        // Parse JSON metadata
    }
}
```

### Converting to Geographic Coordinates

```csharp
// Convert quantized mesh coordinates to WGS84 lat/lon
var tileCoord = new TerrainTileCoordinate(15, 12345, 67890);
var (west, south, east, north) = tileCoord.GetBoundingBox();

var terrainData = QuantizedMeshReader.Read("tile.terrain");

for (int i = 0; i < terrainData.VertexCount; i++)
{
    // Get normalized coordinates [0, 1]
    double u = terrainData.GetNormalizedU(i);
    double v = terrainData.GetNormalizedV(i);
    
    // Convert to geographic coordinates
    double longitude = west + u * (east - west);
    double latitude = south + v * (north - south);
    double height = terrainData.GetHeight(i);
    
    // Now you have WGS84 coordinates
    Console.WriteLine($"Point: ({longitude}, {latitude}, {height}m)");
}
```

### Converting Between Terrain and Raster Formats

The library provides bidirectional conversion between Cesium terrain mesh format and raster DEM (RasterGeoTiff).

#### Terrain → Raster (Mesh to Grid)

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Read terrain tile
var terrainData = QuantizedMeshReader.Read("15/12345/67890.terrain");
var tileCoord = new TerrainTileCoordinate(15, 12345, 67890);

// Convert to raster DEM with specified resolution
var raster = TerrainRasterConverter.ToRasterGeoTiff(
    terrainData, 
    tileCoord,
    outputWidth: 512,   // Desired raster width
    outputHeight: 512   // Desired raster height
);

// Access the elevation data
Console.WriteLine($"Raster bounds: {raster.GeodeticWgs84BoundingBox}");
Console.WriteLine($"Dimensions: {raster.Data.NumberOfRows} × {raster.Data.NumberOfColumns}");

// Sample elevation at specific pixel
double elevation = raster.Data[256, 256]; // Center pixel
```

#### Raster → Terrain (Grid to Mesh)

```csharp
using IRI.Maptor.Sta.Spatial.IO;
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Read GeoTIFF DEM
var raster = TiffReader.ReadGeoTiff32bitDEM("dem.tif");

// Convert to terrain mesh
var tileCoord = new TerrainTileCoordinate(15, 12345, 67890);
var terrainData = TerrainRasterConverter.FromRasterGeoTiff(raster, tileCoord);

// Validate the mesh
if (terrainData.IsValid())
{
    Console.WriteLine($"Created mesh with {terrainData.VertexCount} vertices");
    Console.WriteLine($"Triangle count: {terrainData.TriangleCount}");
    Console.WriteLine($"Height range: {terrainData.Header.MinimumHeight}m to {terrainData.Header.MaximumHeight}m");
}
```

#### Batch Processing Multiple Tiles

```csharp
// Convert multiple terrain tiles to a single raster mosaic
var tiles = new[]
{
    ("15/12345/67890.terrain", new TerrainTileCoordinate(15, 12345, 67890)),
    ("15/12346/67890.terrain", new TerrainTileCoordinate(15, 12346, 67890)),
    ("15/12345/67891.terrain", new TerrainTileCoordinate(15, 12345, 67891)),
    ("15/12346/67891.terrain", new TerrainTileCoordinate(15, 12346, 67891))
};

foreach (var (path, coord) in tiles)
{
    var terrainData = QuantizedMeshReader.Read(path);
    var raster = TerrainRasterConverter.ToRasterGeoTiff(terrainData, coord, 256, 256);
    
    // Save as GeoTIFF or process further
    Console.WriteLine($"Converted {path} to raster");
}
```

#### Getting DEM for a Specific Bounding Box and Zoom Level

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;
using IRI.Maptor.Sta.Common.Primitives;
using System.Collections.Generic;

/// <summary>
/// Example: Get DEM tiles for a specific area at zoom level 6
/// </summary>
public static List<RasterGeoTiff> GetDEMForBoundingBox(
    BoundingBox bbox,           // Geographic bounding box (WGS84)
    int zoomLevel,              // Zoom level (e.g., 6)
    string terrainBasePath)     // Base path to terrain tiles
{
    // Example bounding box: San Francisco Bay Area
    // var bbox = new BoundingBox(
    //     xMin: -122.5,  // West (longitude)
    //     yMin: 37.2,    // South (latitude)
    //     xMax: -121.8,  // East (longitude)
    //     yMax: 37.9     // North (latitude)
    // );
    
    // Calculate which tiles are needed
    var tileCoords = GetTileCoordinatesForBoundingBox(bbox, zoomLevel);
    
    Console.WriteLine($"Found {tileCoords.Count} tiles for zoom level {zoomLevel}");
    
    var rasters = new List<RasterGeoTiff>();
    
    foreach (var coord in tileCoords)
    {
        try
        {
            // Build the file path (e.g., "terrain/6/12/34.terrain")
            string filePath = Path.Combine(terrainBasePath, coord.GetFileName());
            
            // Check if file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Warning: Tile not found: {filePath}");
                continue;
            }
            
            // Read terrain tile
            var terrainData = QuantizedMeshReader.Read(filePath);
            
            // Convert to raster (256x256 is a good default resolution)
            var raster = TerrainRasterConverter.ToRasterGeoTiff(
                terrainData, 
                coord,
                outputWidth: 256,
                outputHeight: 256
            );
            
            rasters.Add(raster);
            
            var tileBbox = coord.GetBoundingBox();
            Console.WriteLine($"Processed tile {coord}: " +
                            $"[{tileBbox.west:F4}, {tileBbox.south:F4}, {tileBbox.east:F4}, {tileBbox.north:F4}]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing tile {coord}: {ex.Message}");
        }
    }
    
    return rasters;
}

/// <summary>
/// Calculate which tile coordinates intersect with the bounding box at given zoom level
/// </summary>
private static List<TerrainTileCoordinate> GetTileCoordinatesForBoundingBox(
    BoundingBox bbox, 
    int zoomLevel)
{
    var tiles = new List<TerrainTileCoordinate>();
    
    double west = bbox.XMin;
    double south = bbox.YMin;
    double east = bbox.XMax;
    double north = bbox.YMax;
    
    // Calculate tile indices for the bounding box
    int numTiles = 1 << zoomLevel; // 2^zoomLevel
    double tileWidth = 360.0 / numTiles;
    double tileHeight = 180.0 / numTiles;
    
    // Calculate tile range
    int minX = (int)Math.Floor((west + 180.0) / tileWidth);
    int maxX = (int)Math.Floor((east + 180.0) / tileWidth);
    int minY = (int)Math.Floor((90.0 - north) / tileHeight);
    int maxY = (int)Math.Floor((90.0 - south) / tileHeight);
    
    // Clamp to valid range
    minX = Math.Max(0, Math.Min(minX, numTiles - 1));
    maxX = Math.Max(0, Math.Min(maxX, numTiles - 1));
    minY = Math.Max(0, Math.Min(minY, numTiles - 1));
    maxY = Math.Max(0, Math.Min(maxY, numTiles - 1));
    
    // Generate all tile coordinates in the range
    for (int y = minY; y <= maxY; y++)
    {
        for (int x = minX; x <= maxX; x++)
        {
            tiles.Add(new TerrainTileCoordinate(zoomLevel, x, y));
        }
    }
    
    return tiles;
}

// Example usage:
// Get DEM for San Francisco Bay Area at zoom level 6
var sanFranciscoBbox = new BoundingBox(
    xMin: -122.5,   // West
    yMin: 37.2,     // South
    xMax: -121.8,   // East
    yMax: 37.9      // North
);

var rasters = GetDEMForBoundingBox(
    sanFranciscoBbox, 
    zoomLevel: 6,
    terrainBasePath: @"C:\terrain\tiles"
);

// Now you have a list of raster DEMs covering the area
Console.WriteLine($"\nGenerated {rasters.Count} DEM tiles");
foreach (var raster in rasters)
{
    var bbox = raster.GeodeticWgs84BoundingBox;
    double minHeight = double.MaxValue;
    double maxHeight = double.MinValue;
    
    // Calculate height statistics
    for (int row = 0; row < raster.Data.NumberOfRows; row++)
    {
        for (int col = 0; col < raster.Data.NumberOfColumns; col++)
        {
            double height = raster.Data[row, col];
            if (!double.IsNaN(height))
            {
                minHeight = Math.Min(minHeight, height);
                maxHeight = Math.Max(maxHeight, height);
            }
        }
    }
    
    Console.WriteLine($"  Bounds: [{bbox.XMin:F4}, {bbox.YMin:F4}, {bbox.XMax:F4}, {bbox.YMax:F4}]");
    Console.WriteLine($"  Elevation: {minHeight:F1}m to {maxHeight:F1}m");
}

// Optional: Merge all rasters into a single mosaic
// (You would need to implement a raster merging function)
```

**Expected Output:**
```
Found 4 tiles for zoom level 6
Processed tile Level 6: (10, 24): [-122.6250, 37.2656, -121.8750, 37.9688]
Processed tile Level 6: (10, 25): [-122.6250, 36.5625, -121.8750, 37.2656]
Processed tile Level 6: (11, 24): [-121.8750, 37.2656, -121.1250, 37.9688]
Processed tile Level 6: (11, 25): [-121.8750, 36.5625, -121.1250, 37.2656]

Generated 4 DEM tiles
  Bounds: [-122.6250, 37.2656, -121.8750, 37.9688]
  Elevation: 0.0m to 927.5m
  Bounds: [-122.6250, 36.5625, -121.8750, 37.2656]
  Elevation: 0.0m to 1234.2m
  ...
```

**Notes:**
- Zoom level 6 provides global coverage with 128 × 64 tiles
- Higher zoom levels provide more detail but require more tiles
- Each tile is typically 256×256 pixels when converted to raster
- For large areas, consider processing tiles in parallel
- Some terrain providers may use different tiling schemes (TMS vs. XYZ)

## 📦 Data Structure

### File Format

```
+------------------+
| Header (88 bytes)|
+------------------+
| Vertex Count     | 4 bytes (uint32)
+------------------+
| U coordinates    | Variable (zigzag-encoded deltas)
+------------------+
| V coordinates    | Variable (zigzag-encoded deltas)
+------------------+
| Height values    | Variable (zigzag-encoded deltas)
+------------------+
| Triangle Count   | 4 bytes (uint32)
+------------------+
| Indices          | Variable (high water mark encoding)
+------------------+
| West Edges       | Variable
+------------------+
| South Edges      | Variable
+------------------+
| East Edges       | Variable
+------------------+
| North Edges      | Variable
+------------------+
| Extensions       | Optional
+------------------+
```

### Header Structure (88 bytes)

| Field | Type | Size | Description |
|-------|------|------|-------------|
| CenterX, Y, Z | double | 24 | Tile center in ECEF coordinates |
| MinHeight, MaxHeight | float | 8 | Height range in meters |
| BoundingSphereCenter | double[3] | 24 | Bounding sphere center (relative) |
| BoundingSphereRadius | double | 8 | Bounding sphere radius |
| HorizonOcclusionPoint | double[3] | 24 | Horizon culling optimization |

### Quantization

- **U, V coordinates**: Quantized to 16-bit integers (0-32767)
- **Heights**: Quantized to 16-bit integers (0-32767)
- **Actual value** = min + (quantized / 32767.0) × (max - min)

### Encoding

- **Zigzag encoding**: Converts signed integers to unsigned for better compression
- **Delta encoding**: Stores differences between consecutive values
- **Variable-length encoding**: Uses fewer bytes for smaller numbers
- **High water mark**: Efficient encoding for triangle indices

## 🌍 Tile Pyramid Structure

Terrain tiles are organized in a pyramid structure:

```
Level 0: 2 tiles (2×1)
Level 1: 8 tiles (4×2)
Level 2: 32 tiles (8×4)
...
Level n: (2^(n+1)) × (2^n) tiles
```

Each tile covers a specific geographic area, with higher levels providing more detail.

## 🔗 Resources

- **Specification**: [Cesium Quantized-Mesh](https://github.com/CesiumGS/quantized-mesh)
- **Cesium Ion**: [Official terrain service](https://cesium.com/platform/cesium-ion/)
- **CesiumJS**: [3D mapping library](https://cesium.com/cesiumjs/)

## ⚠️ Notes

- All data is stored in **little-endian** format
- Coordinates use **Earth-Centered, Earth-Fixed (ECEF)** reference frame
- Geographic coordinates are typically **WGS84**
- This implementation currently supports **reading only** (no writer)
- Edge indices are used for **tile stitching** to prevent cracks between adjacent tiles

## 🔄 Conversion Methodology

### Terrain → Raster

The conversion from mesh to raster uses **barycentric interpolation**:

1. **Triangle Lookup**: For each output pixel, find which triangle contains that geographic point
2. **Barycentric Coordinates**: Calculate the barycentric coordinates (weights) within the triangle
3. **Height Interpolation**: Interpolate the height using the weighted average of triangle vertices
4. **Grid Sampling**: Sample at regular intervals to create a uniform grid

**Advantages:**
- Preserves elevation accuracy from the mesh
- Smooth interpolation between vertices
- Handles irregular meshes well

**Trade-offs:**
- Higher resolution rasters take longer to generate
- Some detail may be lost if output resolution is too low

### Raster → Terrain

The conversion from raster to mesh uses **grid triangulation**:

1. **Vertex Creation**: Each valid raster cell becomes a mesh vertex
2. **Grid Triangulation**: Create two triangles per grid cell (diagonal split)
3. **Quantization**: Convert coordinates and heights to 16-bit quantized values
4. **Edge Extraction**: Identify edge vertices for tile stitching
5. **ECEF Calculation**: Compute Earth-Centered coordinates for the header

**Advantages:**
- Simple and predictable mesh topology
- Preserves all raster data points
- Compatible with tile stitching

**Trade-offs:**
- Creates more triangles than adaptive meshing
- No mesh simplification (use simplificationTolerance parameter for future implementation)

## 🔮 Future Enhancements

- ✅ ~~Conversion utilities (DEM ↔ .terrain)~~ **COMPLETED**
- Writer support (`.terrain` file binary writer)
- Advanced mesh simplification (Ramer-Douglas-Peucker, Garland-Heckbert)
- Adaptive mesh generation with error metrics
- Integration with tile cache/streaming systems
- Level-of-detail (LOD) selection algorithms
- Delaunay triangulation for better mesh quality
- Terrain skirt generation for crack prevention


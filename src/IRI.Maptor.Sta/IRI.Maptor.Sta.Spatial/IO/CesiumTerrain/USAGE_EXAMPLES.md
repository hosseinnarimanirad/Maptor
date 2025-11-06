# Terrain Format Usage Examples

Complete examples for working with both **heightmap-1.0** and **quantized-mesh-1.0** terrain formats.

## 🎯 Quick Start

### Auto-Detect and Read Any Format

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Automatically detect and read any terrain format
var (format, data) = TerrainReader.ReadAuto("path/to/tile.terrain");

switch (format)
{
    case TerrainFormat.Heightmap:
        var heightmap = (HeightmapData)data;
        Console.WriteLine($"Heightmap: {heightmap.GridSize}×{heightmap.GridSize}");
        break;
        
    case TerrainFormat.QuantizedMesh:
        var mesh = (QuantizedMeshData)data;
        Console.WriteLine($"Mesh: {mesh.VertexCount} vertices, {mesh.TriangleCount} triangles");
        break;
}
```

## 📖 Format-Specific Reading

### Read Heightmap Format

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Read heightmap-1.0 file
var heightmapData = HeightmapReader.Read("heightmap.terrain");

Console.WriteLine($"Grid size: {heightmapData.GridSize}×{heightmapData.GridSize}");
Console.WriteLine($"Height range: {heightmapData.MinHeight}m to {heightmapData.MaxHeight}m");

// Get height at specific grid cell
float height = heightmapData.GetHeight(row: 64, col: 64);

// Get interpolated height at normalized position (0-1)
float interpolatedHeight = heightmapData.GetInterpolatedHeight(u: 0.5, v: 0.5);
```

### Read Quantized-Mesh Format

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Read quantized-mesh-1.0 file
var meshData = QuantizedMeshReader.Read("mesh.terrain");

Console.WriteLine($"Vertices: {meshData.VertexCount}");
Console.WriteLine($"Triangles: {meshData.TriangleCount}");
Console.WriteLine($"Min height: {meshData.Header.MinimumHeight}m");
Console.WriteLine($"Max height: {meshData.Header.MaximumHeight}m");

// Access vertex data
for (int i = 0; i < meshData.VertexCount; i++)
{
    double u = meshData.GetNormalizedU(i);  // 0-1
    double v = meshData.GetNormalizedV(i);  // 0-1
    double height = meshData.GetHeight(i);   // meters
}

// Access triangles
for (int i = 0; i < meshData.Indices.Length; i += 3)
{
    uint v0 = meshData.Indices[i];
    uint v1 = meshData.Indices[i + 1];
    uint v2 = meshData.Indices[i + 2];
}
```

## 🗺️ Height Queries

### Query by Normalized Coordinates

```csharp
// Get height at specific position within tile (0-1 coordinates)
float height = TerrainReader.GetHeightAt("tile.terrain", u: 0.5, v: 0.5);
Console.WriteLine($"Height at center: {height}m");
```

### Query by Pixel Position

```csharp
// Get height at specific pixel in a 256×256 tile
float pixelHeight = TerrainReader.GetHeightAtPixel(
    terrainBasePath: @"E:\terrain",
    zoom: 13,
    tileX: 4096,
    tileY: 2048,
    pixelX: 128,    // Center of 256×256 tile
    pixelY: 128,
    tileSize: 256
);

Console.WriteLine($"Elevation at pixel (128,128): {pixelHeight}m");
```

## 🔄 Format Conversions

### Heightmap ↔ Raster

```csharp
using IRI.Maptor.Sta.Spatial.IO;
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

// Heightmap → Raster
var heightmapData = HeightmapReader.Read("heightmap.terrain");
var tileCoord = new TerrainTileCoordinate(5, 39, 20);
var raster = HeightmapRasterConverter.ToRasterGeoTiff(heightmapData, tileCoord);

Console.WriteLine($"Raster: {raster.Data.NumberOfRows}×{raster.Data.NumberOfColumns}");

// Raster → Heightmap
var inputRaster = TiffReader.ReadGeoTiff32bitDEM("dem.tif");
var newHeightmap = HeightmapRasterConverter.FromRasterGeoTiff(inputRaster, targetGridSize: 257);

// Resample heightmap to different grid size
var resampled = HeightmapRasterConverter.Resample(heightmapData, targetGridSize: 129);
```

### Quantized-Mesh ↔ Raster

```csharp
// Mesh → Raster
var meshData = QuantizedMeshReader.Read("mesh.terrain");
var tileCoord = new TerrainTileCoordinate(5, 39, 20);
var raster = QuantizedMeshRasterConverter.ToRasterGeoTiff(
    meshData, 
    tileCoord,
    outputWidth: 512,
    outputHeight: 512
);

// Raster → Mesh
var inputRaster = TiffReader.ReadGeoTiff32bitDEM("dem.tif");
var newMesh = QuantizedMeshRasterConverter.FromRasterGeoTiff(inputRaster, tileCoord);
```

## 🌐 Practical Scenarios

### Scenario 1: Build 3D Terrain Mesh for Rendering

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

string terrainPath = @"E:\terrain\13\4096\2048.terrain";
var tileCoord = new TerrainTileCoordinate(13, 4096, 2048);
var (west, south, east, north) = tileCoord.GetBoundingBox();

// Sample at regular intervals (64×64 grid)
var vertices = new List<(double lon, double lat, float height)>();

int gridSize = 64;
for (int row = 0; row < gridSize; row++)
{
    for (int col = 0; col < gridSize; col++)
    {
        double u = col / (double)(gridSize - 1);
        double v = row / (double)(gridSize - 1);
        
        float height = TerrainReader.GetHeightAt(terrainPath, u, v);
        double lon = west + u * (east - west);
        double lat = north - v * (north - south);
        
        vertices.Add((lon, lat, height));
    }
}

Console.WriteLine($"Generated {vertices.Count} vertices for 3D terrain");
```

### Scenario 2: Elevation Profile Along a Path

```csharp
// Define waypoints along a path
var waypoints = new List<(double lon, double lat)>
{
    (51.5, 35.7),
    (51.51, 35.705),
    (51.52, 35.71),
    (51.53, 35.715),
    (51.54, 35.72)
};

int zoom = 10;
string terrainBase = @"E:\terrain";

foreach (var (lon, lat) in waypoints)
{
    // Calculate tile containing this coordinate
    int numTiles = 1 << zoom;
    int tileX = (int)Math.Floor((lon + 180.0) / 360.0 * numTiles);
    int tileY = (int)Math.Floor((90.0 - lat) / 180.0 * numTiles);
    
    var coord = new TerrainTileCoordinate(zoom, tileX, tileY);
    var (west, south, east, north) = coord.GetBoundingBox();
    
    // Calculate position within tile
    double u = (lon - west) / (east - west);
    double v = (north - lat) / (north - south);
    
    string tilePath = Path.Combine(terrainBase, $"{zoom}/{tileX}/{tileY}.terrain");
    
    if (File.Exists(tilePath))
    {
        float elevation = TerrainReader.GetHeightAt(tilePath, u, v);
        Console.WriteLine($"({lon:F4}, {lat:F4}): {elevation:F1}m");
    }
}
```

### Scenario 3: Extract Contour Lines

```csharp
// Convert terrain to raster for contour extraction
var meshData = QuantizedMeshReader.Read("tile.terrain");
var tileCoord = new TerrainTileCoordinate(10, 512, 256);
var raster = QuantizedMeshRasterConverter.ToRasterGeoTiff(meshData, tileCoord, 512, 512);

var (west, south, east, north) = tileCoord.GetBoundingBox();
int width = raster.Data.NumberOfColumns;
int height = raster.Data.NumberOfRows;

// Find contour at 1000m elevation
double targetElevation = 1000.0;
double tolerance = 5.0;  // ±5m

var contourPoints = new List<(double lon, double lat)>();

for (int row = 0; row < height; row++)
{
    for (int col = 0; col < width; col++)
    {
        double elevation = raster.Data[row, col];
        
        if (Math.Abs(elevation - targetElevation) < tolerance)
        {
            // This point is on the contour line
            double u = col / (double)(width - 1);
            double v = row / (double)(height - 1);
            
            double lon = west + u * (east - west);
            double lat = north - v * (north - south);
            
            contourPoints.Add((lon, lat));
        }
    }
}

Console.WriteLine($"Found {contourPoints.Count} points on {targetElevation}m contour");
```

### Scenario 4: Terrain Analysis (Slope, Aspect)

```csharp
// Convert to raster for analysis
var heightmapData = HeightmapReader.Read("tile.terrain");
int size = heightmapData.GridSize;

// Calculate slope at each point (simple method)
for (int row = 1; row < size - 1; row++)
{
    for (int col = 1; col < size - 1; col++)
    {
        float center = heightmapData.GetHeight(row, col);
        float east = heightmapData.GetHeight(row, col + 1);
        float west = heightmapData.GetHeight(row, col - 1);
        float north = heightmapData.GetHeight(row - 1, col);
        float south = heightmapData.GetHeight(row + 1, col);
        
        // Calculate slope in degrees
        float dzdx = (east - west) / 2.0f;
        float dzdy = (south - north) / 2.0f;
        float slopeRadians = (float)Math.Atan(Math.Sqrt(dzdx * dzdx + dzdy * dzdy));
        float slopeDegrees = slopeRadians * 180.0f / (float)Math.PI;
        
        // Calculate aspect (direction of slope)
        float aspectRadians = (float)Math.Atan2(dzdy, -dzdx);
        float aspectDegrees = aspectRadians * 180.0f / (float)Math.PI;
        
        if (slopeDegrees > 30) // Steep slope
        {
            Console.WriteLine($"Steep slope at ({row},{col}): {slopeDegrees:F1}° facing {aspectDegrees:F0}°");
        }
    }
}
```

## 🔍 Format Detection and Inspection

```csharp
string filePath = "unknown.terrain";

// Detect format
var format = TerrainReader.DetectFormat(filePath);
Console.WriteLine($"Detected format: {format}");

// Check specific formats
bool isHeightmap = HeightmapReader.IsHeightmapFormat(filePath);
Console.WriteLine($"Is heightmap: {isHeightmap}");

// Get file info
var fileInfo = new FileInfo(filePath);
Console.WriteLine($"File size: {fileInfo.Length} bytes");

if (format == TerrainFormat.Heightmap)
{
    // Heightmap has fixed size based on grid
    long numValues = fileInfo.Length / 2;  // 2 bytes per value
    int gridSize = (int)Math.Sqrt(numValues);
    Console.WriteLine($"Likely grid size: {gridSize}×{gridSize}");
}
```

## ⚡ Performance Optimization

### Cache Frequently Accessed Tiles

```csharp
var tileCache = new Dictionary<string, object>();

float GetCachedHeight(string terrainPath, double u, double v)
{
    if (!tileCache.ContainsKey(terrainPath))
    {
        var (format, data) = TerrainReader.ReadAuto(terrainPath);
        tileCache[terrainPath] = data;
    }
    
    var terrainData = tileCache[terrainPath];
    
    if (terrainData is HeightmapData heightmap)
    {
        return heightmap.GetInterpolatedHeight(u, v);
    }
    else if (terrainData is QuantizedMeshData mesh)
    {
        // Implement mesh interpolation
        // ...
    }
    
    return 0;
}
```

### Batch Process Multiple Tiles

```csharp
var tileFiles = Directory.GetFiles(@"E:\terrain\13", "*.terrain", SearchOption.AllDirectories);
Console.WriteLine($"Processing {tileFiles.Length} tiles...");

var results = new ConcurrentBag<(string path, float avgHeight)>();

Parallel.ForEach(tileFiles, tilePath =>
{
    try
    {
        var (format, data) = TerrainReader.ReadAuto(tilePath);
        
        float avgHeight = 0;
        int count = 0;
        
        if (data is HeightmapData heightmap)
        {
            for (int r = 0; r < heightmap.GridSize; r++)
                for (int c = 0; c < heightmap.GridSize; c++)
                {
                    avgHeight += heightmap.GetHeight(r, c);
                    count++;
                }
        }
        else if (data is QuantizedMeshData mesh)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                avgHeight += (float)mesh.GetHeight(i);
                count++;
            }
        }
        
        avgHeight /= count;
        results.Add((tilePath, avgHeight));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {tilePath}: {ex.Message}");
    }
});

Console.WriteLine($"Processed {results.Count} tiles successfully");
```

## 📏 Coordinate Conversion Helpers

```csharp
// Convert geographic coordinate to tile coordinate
public static TerrainTileCoordinate GeographicToTile(double longitude, double latitude, int zoom)
{
    int numTiles = 1 << zoom;
    
    int x = (int)Math.Floor((longitude + 180.0) / 360.0 * numTiles);
    int y = (int)Math.Floor((90.0 - latitude) / 180.0 * numTiles);
    
    // Clamp to valid range
    x = Math.Max(0, Math.Min(x, numTiles - 1));
    y = Math.Max(0, Math.Min(y, numTiles - 1));
    
    return new TerrainTileCoordinate(zoom, x, y);
}

// Convert tile coordinate to geographic bounds
public static (double west, double south, double east, double north) TileToGeographic(int zoom, int x, int y)
{
    var coord = new TerrainTileCoordinate(zoom, x, y);
    return coord.GetBoundingBox();
}
```

## 🎨 Visualization Example

```csharp
// Create a height-colored bitmap
public static void CreateTerrainVisualization(string terrainPath, string outputImage)
{
    var (format, data) = TerrainReader.ReadAuto(terrainPath);
    
    int width = 512;
    int height = 512;
    var bitmap = new Bitmap(width, height);
    
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            double u = x / (double)(width - 1);
            double v = y / (double)(height - 1);
            
            float h = TerrainReader.GetHeightAt(terrainPath, u, v);
            
            // Color code by elevation
            Color color = GetElevationColor(h);
            bitmap.SetPixel(x, y, color);
        }
    }
    
    bitmap.Save(outputImage);
}

private static Color GetElevationColor(float elevation)
{
    // Simple color gradient: blue (low) → green → yellow → red (high)
    float normalized = Math.Clamp((elevation + 100) / 3000.0f, 0, 1);
    
    if (normalized < 0.25f) return Color.FromArgb(0, 0, 255); // Blue
    if (normalized < 0.5f) return Color.FromArgb(0, 255, 0);  // Green
    if (normalized < 0.75f) return Color.FromArgb(255, 255, 0); // Yellow
    return Color.FromArgb(255, 0, 0); // Red
}
```

## 📚 See Also

- [README.md](./README.md) - Complete API documentation
- [Cesium Terrain Specification](https://github.com/CesiumGS/quantized-mesh)

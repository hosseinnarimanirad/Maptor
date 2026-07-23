# Cesium terrain

Reader support for the two Cesium terrain tile formats (`.terrain` files) used for 3D terrain
visualization in web mapping: **quantized-mesh-1.0** (adaptive triangle mesh, compressed via
quantization and delta encoding) and **heightmap-1.0** (regular elevation grid). This module is
read-only — it parses tiles, answers height queries across tile sets, and converts terrain data
to raster DEMs; it does not write `.terrain` files. Ships in the
[IRI.Maptor.Sta.Spatial](../../README.md) package.

## Supported capabilities

| Capability | Supported | Implemented in |
|---|---|---|
| Read quantized-mesh-1.0 | Yes | `QuantizedMeshReader.Read` (path/stream/reader) |
| Read heightmap-1.0 | Yes | `HeightmapReader.Read`, `IsHeightmapFormat` |
| Auto-detect format | Yes | `TerrainReader.ReadAuto`, `DetectFormat` |
| Height queries (single tile / tile set) | Yes | `TerrainReader.GetHeightAt`/`GetHeightAtPixel`, `TerrainHeightQuery` |
| Terrain → raster DEM (`RasterGeoTiff`) | Yes | `HeightmapRasterConverter.ToRasterGeoTiff`, `QuantizedMeshRasterConverter.ToRasterGeoTiff` |
| Raster DEM → in-memory terrain data | Yes | `HeightmapRasterConverter.FromRasterGeoTiff`, `QuantizedMeshRasterConverter.FromRasterGeoTiff` |
| Write `.terrain` files (either format) | No | — |

## Usage

### Auto-detection and reading

```csharp
using IRI.Maptor.Sta.Spatial.IO.CesiumTerrain;

var (format, data) = TerrainReader.ReadAuto("path/to/tile/15/12345/67890.terrain");

if (format == TerrainFormat.QuantizedMesh)
{
    var meshData = (QuantizedMeshData)data;
    Console.WriteLine($"Mesh: {meshData.VertexCount} vertices");
}
else if (format == TerrainFormat.Heightmap)
{
    var heightmapData = (HeightmapData)data;
    Console.WriteLine($"Grid: {heightmapData.GridSize}x{heightmapData.GridSize}");
}
```

### Height queries

```csharp
// By file path and normalized tile coordinates (0-1)
float height = TerrainReader.GetHeightAt("tile.terrain", u: 0.5, v: 0.5);

// By z/x/y tile coordinates and pixel position
float pixelHeight = TerrainReader.GetHeightAtPixel(
    terrainBasePath: @"C:\terrain",
    zoom: 13, tileX: 4096, tileY: 2048,
    pixelX: 128, pixelY: 128);           // pixel in a 256x256 tile
```

`TerrainHeightQuery` handles multi-tile queries against a tile directory: `GetHeightsForBoundary`
returns a `Matrix` of elevations for a WGS84 bounding box at a zoom level (loading and merging all
intersecting tiles), `GetHeightsForBoundaryWithSize`/`GetHeightsForDisplay` resample to exact
output dimensions, and `GetElevationProfile` samples elevations along a list of lon/lat waypoints.

```csharp
var (heights, actualBounds) = TerrainHeightQuery.GetHeightsForBoundary(
    boundingBox, zoomLevel: 6, terrainBasePath: @"C:\terrain");
```

### Reading quantized-mesh tiles

```csharp
var terrainData = QuantizedMeshReader.Read("path/to/tile/15/12345/67890.terrain");

Console.WriteLine($"Height range: {terrainData.Header.MinimumHeight}m to {terrainData.Header.MaximumHeight}m");
Console.WriteLine($"Vertices: {terrainData.VertexCount}, Triangles: {terrainData.TriangleCount}");

// Vertex data (dequantized on access)
for (int i = 0; i < terrainData.VertexCount; i++)
{
    double u = terrainData.GetNormalizedU(i);      // [0, 1]
    double v = terrainData.GetNormalizedV(i);      // [0, 1]
    double height = terrainData.GetHeight(i);      // meters
}

// Triangle indices
for (int i = 0; i < terrainData.Indices.Length; i += 3)
{
    uint v0 = terrainData.Indices[i];
    uint v1 = terrainData.Indices[i + 1];
    uint v2 = terrainData.Indices[i + 2];
}
```

Edge vertex indices (`WestIndices`, `SouthIndices`, `EastIndices`, `NorthIndices`) are exposed for
tile stitching.

### Reading heightmap tiles

```csharp
var heightmapData = HeightmapReader.Read("tile.terrain");

Console.WriteLine($"Grid size: {heightmapData.GridSize}x{heightmapData.GridSize}");
Console.WriteLine($"Height range: {heightmapData.MinHeight}m to {heightmapData.MaxHeight}m");

float heightAt = heightmapData.GetHeight(row: 128, col: 128);
float interpolated = heightmapData.GetInterpolatedHeight(u: 0.5, v: 0.5);
```

### Tile coordinates

```csharp
var tileCoord = new TerrainTileCoordinate(level: 15, x: 12345, y: 67890);

string fileName = tileCoord.GetFileName();               // "15/12345/67890.terrain"
var (west, south, east, north) = tileCoord.GetBoundingBox(); // WGS84 degrees

var parent = tileCoord.GetParent();
var children = tileCoord.GetChildren();

var coord = TerrainTileCoordinate.FromPath("data/terrain/15/12345/67890.terrain");
var fromGeo = TerrainTileCoordinate.FromGeographic(longitude: 51.4, latitude: 35.7, zoom: 12);
```

To convert vertex positions to geographic coordinates, combine the tile's bounding box with the
normalized vertex coordinates: `longitude = west + u * (east - west)`,
`latitude = south + v * (north - south)`.

### Quantized-mesh extensions

```csharp
var terrainData = QuantizedMeshReader.Read("tile.terrain");

if (terrainData.Extensions != null)
{
    if (terrainData.Extensions.HasWaterMask)
    {
        byte[] waterMask = terrainData.Extensions.WaterMask;   // 0 = land, 255 = water
    }

    if (terrainData.Extensions.HasVertexNormals)
    {
        byte[] normals = terrainData.Extensions.VertexNormals; // oct-encoded, 2 bytes/vertex
        var (nx, ny, nz) = QuantizedMeshReader.DecodeOctNormal(normals[0], normals[1]);
    }

    if (terrainData.Extensions.HasMetadata)
    {
        string metadata = terrainData.Extensions.Metadata;     // JSON string
    }
}
```

### Converting terrain to raster DEM

Both formats convert to `RasterGeoTiff` (see [`../GeoTiff`](../GeoTiff)):

```csharp
// Quantized-mesh -> raster (barycentric interpolation over the mesh)
var terrainData = QuantizedMeshReader.Read("15/12345/67890.terrain");
var tileCoord = new TerrainTileCoordinate(15, 12345, 67890);
var raster = QuantizedMeshRasterConverter.ToRasterGeoTiff(
    terrainData, tileCoord, outputWidth: 512, outputHeight: 512);

Console.WriteLine($"Bounds: {raster.GeodeticWgs84BoundingBox}");
double elevation = raster.Data[256, 256];

// Heightmap -> raster
var heightmapData = HeightmapReader.Read("tile.terrain");
var raster2 = HeightmapRasterConverter.ToRasterGeoTiff(heightmapData, new TerrainTileCoordinate(5, 39, 20));
```

### Converting raster DEM to in-memory terrain data

The reverse converters build in-memory `HeightmapData`/`QuantizedMeshData` objects from a raster
DEM. Because there is no `.terrain` writer, the result can be queried or converted back but
cannot be saved as a terrain tile.

```csharp
var raster = TiffReader.ReadGeoTiff32bitDEM("dem.tif");   // IRI.Maptor.Sta.Spatial.IO

var heightmap = HeightmapRasterConverter.FromRasterGeoTiff(raster, targetGridSize: 257);
var resampled = HeightmapRasterConverter.Resample(heightmap, targetGridSize: 65);

var mesh = QuantizedMeshRasterConverter.FromRasterGeoTiff(raster, new TerrainTileCoordinate(15, 12345, 67890));
```

## Quantized-mesh binary layout

```
Header (88 bytes)
Vertex count            uint32
U coordinates           zigzag-encoded deltas
V coordinates           zigzag-encoded deltas
Height values           zigzag-encoded deltas
Triangle count          uint32
Indices                 high-water-mark encoding
West/South/East/North   edge vertex indices (for tile stitching)
Extensions              optional (normals, water mask, metadata)
```

Header fields (all little-endian): tile center in ECEF (`CenterX/Y/Z`, 3 doubles),
`MinimumHeight`/`MaximumHeight` (2 floats), bounding sphere center and radius (4 doubles), and
horizon occlusion point (3 doubles).

U, V, and height values are quantized to 16-bit integers in [0, 32767];
`actual = min + (quantized / 32767.0) * (max - min)`. Heightmap-1.0 files are a plain row-major
grid of elevation samples; the grid size is detected from the file length.

## Tile pyramid

Terrain tiles live in a level/x/y pyramid addressed as `{level}/{x}/{y}.terrain`, with each level
doubling the resolution of the previous one. `TerrainTileCoordinate` implements the coordinate
arithmetic: file naming, parent/children navigation, geographic bounds, and lookup by geographic
position (see the tiling-scheme caveat under Limitations).

## Format comparison

| Feature | heightmap-1.0 | quantized-mesh-1.0 |
|---|---|---|
| Structure | Regular grid | Triangle mesh |
| Sampling | Uniform | Adaptive |
| File size | Fixed (grid-based) | Variable |
| Height lookup | Direct grid access | Barycentric interpolation |

## Conversion methodology

- **Mesh → raster**: for each output pixel, find the containing triangle, compute barycentric
  weights, and interpolate the height from the triangle's vertices.
- **Raster → mesh**: each valid raster cell becomes a vertex; each grid cell is split into two
  triangles; coordinates and heights are quantized to 16-bit values; edge vertices are extracted
  for stitching and ECEF header values computed.
- **Raster → heightmap**: bilinear resampling to a `2^n + 1` grid.

## Limitations

- **Writing is not implemented** for either format: there is no serializer that produces
  `.terrain` files, so `FromRasterGeoTiff` results exist in memory only.
- `QuantizedMeshRasterConverter.FromRasterGeoTiff` accepts a `simplificationTolerance` parameter,
  but mesh simplification is not implemented — the full grid triangulation is always produced.
- Heightmap grid sizes must be `2^n + 1` (65, 129, 257, 513, ...); other sizes are rejected.
- `TerrainTileCoordinate` assumes a simplified geographic tiling with a single 2^level x 2^level
  grid per level — not the standard Cesium layout with two root tiles (2^(n+1) x 2^n at level n).
  `FromGeographic` maps latitude over 180/2^level degrees per tile while `GetBoundingBox` uses
  360/2^level on both axes, so providers using the standard scheme need their own coordinate
  mapping.
- Quantized-mesh water mask, vertex normals, and metadata extensions are parsed but not
  interpreted beyond raw access.
- No tile caching or streaming; each query loads tiles from disk.

## References

- [Cesium quantized-mesh specification](https://github.com/CesiumGS/quantized-mesh)
- [CesiumJS](https://cesium.com/cesiumjs/)
- [USAGE_EXAMPLES.md](./USAGE_EXAMPLES.md) — additional code examples

---
[Back to IRI.Maptor.Sta.Spatial](../../README.md)

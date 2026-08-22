# Mapbox vector tiles

A dependency-free reader for Mapbox Vector Tiles (`.mvt` / `.pbf`). It decodes the protobuf tile into layers and features, and converts feature geometry into the library's `Geometry<Point>` in Web Mercator (EPSG:3857).

## Supported capabilities

| Capability | Supported |
|---|---|
| Read | Yes — `MvtTileReader.Decode`, `MvtGeometryDecoder.ToGeometry` |
| Write | No — there is no encoder |
| Gzip decompression | Yes — `MvtDecompressionHelper.Decompress` (auto-detects the gzip magic bytes) |
| Z / M coordinates | No — 2D only |

The protobuf decoding is self-contained (`MvtProtoReader`) — no external protobuf dependency. Layers, features, attributes, and geometry kind (point / linestring / polygon) are all surfaced on the decoded model.

## Usage

Types live in `IRI.Maptor.Core.Spatial.IO.VectorTiles`. The typical pipeline is: decompress → decode → build a per-layer transform → convert each feature.

```csharp
using IRI.Maptor.Core.Spatial.IO.VectorTiles;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;

// The tile address the bytes were fetched for
int z = 12, x = 2341, y = 1567;

byte[] raw = File.ReadAllBytes("tile.mvt");

// 1) gzip-decompress if needed (no-op for uncompressed input)
byte[] bytes = MvtDecompressionHelper.Decompress(raw);

// 2) decode protobuf into the tile model
MvtTile tile = MvtTileReader.Decode(bytes);

// 3) per layer, build a tile-local → Web Mercator transform and convert features
foreach (MvtLayer layer in tile.Layers)
{
    Func<int, int, Point> toPoint = MvtTileTransform.LocalToWebMercator(z, x, y, layer.Extent);

    foreach (MvtFeature feature in layer.Features)
    {
        Geometry<Point>? geometry = MvtGeometryDecoder.ToGeometry(feature, toPoint, srid: 3857);
        if (geometry is null)
            continue; // unknown/empty geometry

        // feature.Attributes holds the decoded tags; geometry is ready to use
    }
}
```

## Model

- `MvtTile` — `Layers`
- `MvtLayer` — `Name`, `Version`, `Extent` (default 4096), `Features`
- `MvtFeature` — `Id`, `GeometryKind` (`Point` / `LineString` / `Polygon`), `Attributes`, and the raw MVT command stream in `Geometry`

`MvtGeometryDecoder.ToGeometry` turns the raw command stream into `Geometry<Point>` (point → `Point`/`MultiPoint`, line → `LineString`/`MultiLineString`, polygon → `Polygon`/`MultiPolygon`).

## Format details

| Aspect | MVT in Maptor |
|--------|---------------|
| **Coordinate system** | MVT stores tile-local integer coordinates; the standard XYZ scheme is Web Mercator. `MvtTileTransform.LocalToWebMercator` outputs **Web Mercator (EPSG:3857) only** — pass `srid: 3857` to `ToGeometry`. |
| **Z / M** | 2D only. |
| **Polygon rings** | Rings are closed via the MVT `ClosePath` command. The decoder normalizes winding to `Geometry<Point>` orientation (exterior CCW, holes CW) using `CreatePolygonOrMultiPolygon(..., fixOrientation: true)`. |
| **Serialization** | Protobuf wire format (not JSON/XML). **Decode-only**: `MvtTileReader.Decode(byte[])` (with `MvtDecompressionHelper.Decompress` for gzip). There is no encoder. |
| **Specification** | [Mapbox Vector Tile Specification (v2.1)](https://github.com/mapbox/vector-tile-spec/tree/master/2.1) |

## Limitations

- Read-only: this module decodes MVT tiles; it does not encode/write them.
- The built-in transform emits Web Mercator coordinates only; supply your own `Func<int, int, Point>` for other target systems.
- `MvtTileReader.Decode` takes an already-decompressed `byte[]`; call `MvtDecompressionHelper.Decompress` first when the source may be gzipped.

---
[Back to IRI.Maptor.Core.Spatial](../../README.md)

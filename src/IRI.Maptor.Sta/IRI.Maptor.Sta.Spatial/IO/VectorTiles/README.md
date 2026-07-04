# 🧩 Mapbox Vector Tiles (MVT) in Maptor

![MVT](https://img.shields.io/badge/MVT-2.1_decode-blue)
![.NET](https://img.shields.io/badge/.NET-Standard_2.1-green)

A dependency-free reader for Mapbox Vector Tiles (`.mvt` / `.pbf`). It decodes the protobuf tile into layers and features, and converts feature geometry into the library's `Geometry<Point>` in Web Mercator (EPSG:3857).

> **Read-only.** This module decodes MVT tiles; it does not encode/write them.

## ✨ Features

- Self-contained protobuf decoding — no external protobuf dependency
- Transparent gzip handling (auto-detects the gzip magic bytes)
- Layers, features, attributes, and geometry kind (point / linestring / polygon)
- Tile-local → Web Mercator transform, then decode to `Geometry<Point>`

## ⚙️ Installation

```bash
dotnet add package IRI.Maptor.Sta.Spatial
```

## 🚀 Getting Started

Types live in `IRI.Maptor.Sta.Spatial.IO.VectorTiles`. The typical pipeline is: decompress → decode → build a per-layer transform → convert each feature.

```csharp
using IRI.Maptor.Sta.Spatial.IO.VectorTiles;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

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

## 🧱 Model

- `MvtTile` — `Layers`
- `MvtLayer` — `Name`, `Version`, `Extent` (default 4096), `Features`
- `MvtFeature` — `Id`, `GeometryKind` (`Point` / `LineString` / `Polygon`), `Attributes`, and the raw MVT command stream in `Geometry`

`MvtGeometryDecoder.ToGeometry` turns the raw command stream into `Geometry<Point>` (point → `Point`/`MultiPoint`, line → `LineString`/`MultiLineString`, polygon → `Polygon`/`MultiPolygon`).

## 📋 Format Details

| Aspect | MVT in Maptor |
|--------|---------------|
| **Coordinate system** | MVT stores tile-local integer coordinates; the standard XYZ scheme is Web Mercator. `MvtTileTransform.LocalToWebMercator` outputs **Web Mercator (EPSG:3857) only** — pass `srid: 3857` to `ToGeometry`. |
| **Z / M** | 2D only. |
| **Polygon rings** | Rings are closed via the MVT `ClosePath` command. The decoder normalizes winding to `Geometry<Point>` orientation (exterior CCW, holes CW) using `CreatePolygonOrMultiPolygon(..., fixOrientation: true)`. |
| **Serialization** | Protobuf wire format (not JSON/XML). **Decode-only**: `MvtTileReader.Decode(byte[])` (with `MvtDecompressionHelper.Decompress` for gzip). There is no encoder. |
| **Specification** | [Mapbox Vector Tile Specification (v2.1)](https://github.com/mapbox/vector-tile-spec/tree/master/2.1) |

## 📝 Notes

- The transform emits Web Mercator coordinates, so pass `srid: 3857` (or a matching SRID) to `ToGeometry`.
- `zoom`, `tileColumn` (X), and `tileRow` (Y) identify the tile; `Extent` comes from each layer (usually 4096).
- `MvtTileReader.Decode` takes an already-decompressed `byte[]`; call `MvtDecompressionHelper.Decompress` first when the source may be gzipped.

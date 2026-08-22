# PMTiles

The `PmTilesReader` and `PmTilesWriter` classes provide PMTiles v3 archive support for both vector and raster tiles.
The implementation targets `netstandard2.1` and is compatible with serverless (range-based) hosting scenarios.

## Supported capabilities

| Capability | Supported |
|---|---|
| Read | Yes — `PmTilesReader` (`GetTileAsync`, `GetTileByIdAsync`, `GetMetadataJsonAsync`) |
| Write | Yes — `PmTilesWriter` (`AddTile`, `WriteAsync`, `BuildAsync`) |
| Gzip / Brotli compression | Yes |
| Zstandard compression | No — throws `NotSupportedException` |
| Leaf directories | Read only — the writer emits a single root directory |

## Features

- Parse and emit the 127-byte PMTiles header (`PmTilesHeader`).
- Decode/encode directory entries with run-length support (`PmTilesDirectory`).
- Read from any random-access byte source via `IPmTilesStreamSource` (local files, HTTP range requests, ...).
- Build archives from a set of tiles with optional automatic Gzip/Brotli compression.
- Store and retrieve TileJSON metadata (compressed via the archive's internal compression).
- Automatic Hilbert curve conversions between `(z, x, y)` and PMTiles tile identifiers (`PmTilesHilbert`).

## Usage

```csharp
// Write
var writer = new PmTilesWriter();
writer.AddTile(0, 0, 0, tileBytes);

await using var stream = File.Create("sample.pmtiles");
await writer.WriteAsync(stream, new PmTilesWriterOptions
{
    TileType = PmTilesTileType.VectorMvt,
    InternalCompression = PmTilesCompression.Gzip,
    TileCompression = PmTilesCompression.Gzip,
    MetadataJson = "{\"name\":\"sample\"}",
});

// Read — supply an IPmTilesStreamSource over your storage
await using var reader = new PmTilesReader(source);
await reader.InitializeAsync();

var tile = await reader.GetTileAsync(0, 0, 0);
string? metadata = reader.MetadataJson;
```

## Limitations

- Zstandard compression is not implemented; `PmTilesCompression.Zstandard` throws `NotSupportedException`.
- The writer stores every entry in the root directory and never generates leaf directories (the header's leaf-directory section is written empty). The reader, however, can follow leaf directories in archives produced by other tools.
- The writer stores tiles sequentially without deduplication (every entry has run length 1).
- No `IPmTilesStreamSource` implementation ships with the library — implement it over your storage (see the test suite's in-memory source for an example). The `IPmTilesRangeReader` types (`FilePmTilesRangeReader`, `InMemoryPmTilesRangeReader`) are not consumed by `PmTilesReader`.

---
[Back to IRI.Maptor.Core.Spatial](../../README.md)

# PMTiles IO Support

The `PmTilesReader` and `PmTilesWriter` classes provide PMTiles v3 archive support for both vector and raster tiles.  
The implementation targets `netstandard2.1` and is compatible with serverless (range-based) hosting scenarios.

## Features

- Parse and emit the 127-byte PMTiles header (`PmTilesHeader`).
- Enumerate directories and decode/encode entries with run-length support.
- Serve tiles through random-access providers (`IPmTilesRangeReader`), including in-memory and file-backed implementations.
- Build archives from a set of tiles with optional automatic Gzip/Brotli compression.
- Store and retrieve TileJSON metadata (compressed via the internal compression codec).
- Automatic Hilbert curve conversions between `(z, x, y)` and PMTiles tile identifiers.

## Usage

```csharp
var writer = new PmTilesWriter(
    tileType: PmTilesTileType.Mvt,
    internalCompression: PmTilesCompression.Gzip,
    tileCompression: PmTilesCompression.Gzip);

writer.SetMetadata(new { name = "sample" });
writer.AddTile(0, 0, 0, tileBytes);

await using var stream = File.Create("sample.pmtiles");
await writer.WriteAsync(stream);

await using var reader = await PmTilesReader.OpenAsync("sample.pmtiles");
var tile = await reader.TryGetTileAsync(0, 0, 0);
```

## Limitations

- Zstandard compression is not implemented; provide custom codecs if needed.
- Writers group tiles into leaf directories when the root directory would exceed the 16 KiB limit; nested leaves beyond a single level are not generated.
- The current implementation stores tiles sequentially without deduplication.


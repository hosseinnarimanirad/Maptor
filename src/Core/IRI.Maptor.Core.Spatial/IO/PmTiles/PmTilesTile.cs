using System;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Represents a single tile payload retrieved from a PMTiles archive.
/// </summary>
public sealed class PmTilesTile
{
    public PmTilesTile(PmTilesTileAddress address, ReadOnlyMemory<byte> content, PmTilesTileType tileType, PmTilesCompression originalCompression, bool isDecompressed)
    {
        Address = address;
        Content = content;
        TileType = tileType;
        OriginalCompression = originalCompression;
        IsDecompressed = isDecompressed;
    }

    public PmTilesTileAddress Address { get; }

    public ReadOnlyMemory<byte> Content { get; }

    public PmTilesTileType TileType { get; }

    public PmTilesCompression OriginalCompression { get; }

    public bool IsDecompressed { get; }
}


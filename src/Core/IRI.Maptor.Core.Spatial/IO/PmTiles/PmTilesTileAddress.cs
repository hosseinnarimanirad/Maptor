using System;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Represents a tile location identified by both z/x/y and PMTiles tile id.
/// </summary>
public readonly struct PmTilesTileAddress : IEquatable<PmTilesTileAddress>
{
    public PmTilesTileAddress(int zoom, int x, int y)
    {
        Zoom = zoom;
        X = x;
        Y = y;
        TileId = PmTilesHilbert.ToTileId(zoom, x, y);
    }

    public PmTilesTileAddress(ulong tileId)
    {
        var (zoom, x, y) = PmTilesHilbert.FromTileId(tileId);
        Zoom = zoom;
        X = x;
        Y = y;
        TileId = tileId;
    }

    public int Zoom { get; }

    public int X { get; }

    public int Y { get; }

    public ulong TileId { get; }

    public override string ToString()
    {
        return $"z{Zoom}/{X}/{Y} ({TileId})";
    }

    public bool Equals(PmTilesTileAddress other)
    {
        return TileId == other.TileId && Zoom == other.Zoom && X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is PmTilesTileAddress other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TileId, Zoom, X, Y);
    }

    public static bool operator ==(PmTilesTileAddress left, PmTilesTileAddress right) => left.Equals(right);

    public static bool operator !=(PmTilesTileAddress left, PmTilesTileAddress right) => !left.Equals(right);
} 

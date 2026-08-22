using System;

namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

/// <summary>
/// Represents a single entry in a PMTiles directory (root or leaf).
/// </summary>
public readonly struct PmTilesDirectoryEntry : IEquatable<PmTilesDirectoryEntry>
{
    public PmTilesDirectoryEntry(ulong tileId, ulong offset, ulong length, ulong runLength)
    {
        if (length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be greater than zero.");
        }

        TileId = tileId;
        Offset = offset;
        Length = length;
        RunLength = runLength;
    }

    /// <summary>
    /// Tile identifier (Hilbert order). For leaf directories, this is the first tile contained in the directory.
    /// </summary>
    public ulong TileId { get; }

    /// <summary>
    /// Byte offset relative to the tile data section (for tile entries) or the leaf directory section (for leaf entries).
    /// </summary>
    public ulong Offset { get; }

    /// <summary>
    /// Length of the tile or serialized leaf directory in bytes (compressed length).
    /// </summary>
    public ulong Length { get; }

    /// <summary>
    /// Number of tiles covered by the entry. A value of 0 indicates a leaf directory pointer.
    /// </summary>
    public ulong RunLength { get; }

    public bool IsLeafDirectory => RunLength == 0;

    public override string ToString()
    {
        var type = IsLeafDirectory ? "Leaf" : "Tile";
        return $"{type} TileId={TileId} Offset={Offset} Length={Length} RunLength={RunLength}";
    }

    public bool Equals(PmTilesDirectoryEntry other)
    {
        return TileId == other.TileId &&
               Offset == other.Offset &&
               Length == other.Length &&
               RunLength == other.RunLength;
    }

    public override bool Equals(object? obj)
    {
        return obj is PmTilesDirectoryEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TileId, Offset, Length, RunLength);
    }

    public static bool operator ==(PmTilesDirectoryEntry left, PmTilesDirectoryEntry right) => left.Equals(right);

    public static bool operator !=(PmTilesDirectoryEntry left, PmTilesDirectoryEntry right) => !left.Equals(right);
} 
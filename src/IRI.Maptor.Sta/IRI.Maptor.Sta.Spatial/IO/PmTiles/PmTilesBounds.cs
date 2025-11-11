using System;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

/// <summary>
/// Represents the geographic bounds encoded in a PMTiles header.
/// </summary>
public readonly struct PmTilesBounds : IEquatable<PmTilesBounds>
{
    public PmTilesBounds(PmTilesPosition min, PmTilesPosition max)
    {
        Min = min;
        Max = max;
    }

    public PmTilesPosition Min { get; }

    public PmTilesPosition Max { get; }

    public override string ToString()
    {
        return $"{Min} - {Max}";
    }

    public bool Equals(PmTilesBounds other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public override bool Equals(object? obj)
    {
        return obj is PmTilesBounds other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    public static bool operator ==(PmTilesBounds left, PmTilesBounds right) => left.Equals(right);

    public static bool operator !=(PmTilesBounds left, PmTilesBounds right) => !left.Equals(right);
}


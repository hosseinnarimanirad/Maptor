using System;
using System.Globalization;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

/// <summary>
/// Represents a geographic coordinate encoded using the PMTiles fixed-point scheme (degrees multiplied by 1e7).
/// </summary>
public readonly struct PmTilesPosition : IEquatable<PmTilesPosition>
{
    private const double Scale = 1e7;

    public PmTilesPosition(int longitudeE7, int latitudeE7)
    {
        LongitudeE7 = longitudeE7;
        LatitudeE7 = latitudeE7;
    }

    public int LongitudeE7 { get; }

    public int LatitudeE7 { get; }

    public double Longitude => LongitudeE7 / Scale;

    public double Latitude => LatitudeE7 / Scale;

    public static PmTilesPosition FromDegrees(double longitude, double latitude)
    {
        static int ToE7(double value) => (int)Math.Round(value * Scale, MidpointRounding.AwayFromZero);

        return new PmTilesPosition(ToE7(longitude), ToE7(latitude));
    }

    public static PmTilesPosition FromBytes(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 8)
        {
            throw new ArgumentException("A position requires 8 bytes.", nameof(buffer));
        }

        var longitude = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buffer);
        var latitude = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4));

        return new PmTilesPosition(longitude, latitude);
    }

    public static void WriteBytes(Span<byte> buffer, in PmTilesPosition position)
    {
        if (buffer.Length < 8)
        {
            throw new ArgumentException("A position requires 8 bytes.", nameof(buffer));
        }

        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buffer, position.LongitudeE7);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), position.LatitudeE7);
    }

    public override string ToString()
    {
        return FormattableString.Invariant($"({Longitude:F7}, {Latitude:F7})");
    }

    public bool Equals(PmTilesPosition other)
    {
        return LongitudeE7 == other.LongitudeE7 && LatitudeE7 == other.LatitudeE7;
    }

    public override bool Equals(object? obj)
    {
        return obj is PmTilesPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LongitudeE7, LatitudeE7);
    }

    public static bool operator ==(PmTilesPosition left, PmTilesPosition right) => left.Equals(right);

    public static bool operator !=(PmTilesPosition left, PmTilesPosition right) => !left.Equals(right);
}


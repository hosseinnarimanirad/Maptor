using System;
using System.Globalization;
using System.Text;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

/// <summary>
/// One parsed MGRS reference — the grid zone, the 100 km square, and however many digits of
/// easting and northing the reference carried.
/// </summary>
/// <remarks>
/// This is the *text* form: a square on the ground, not a point. The digits are the offsets
/// inside the 100 km square at the stated <see cref="Precision"/>, so <c>39S WV 53 39</c> keeps
/// <c>Easting = 53</c> and <c>Northing = 39</c>, not 53 000 and 39 000. Use
/// <see cref="MgrsConverter"/> to turn it into UTM or geodetic coordinates.
/// </remarks>
public readonly struct MgrsCoordinate : IEquatable<MgrsCoordinate>
{
    /// <summary>UTM zone number, 1–60.</summary>
    public int Zone { get; }

    /// <summary>Latitude band letter, <c>C</c>–<c>X</c> without <c>I</c> and <c>O</c>.</summary>
    public char Band { get; }

    /// <summary>First letter of the 100 km square identifier (the easting one).</summary>
    public char Column { get; }

    /// <summary>Second letter of the 100 km square identifier (the northing one).</summary>
    public char Row { get; }

    /// <summary>Easting digits inside the 100 km square, at <see cref="Precision"/>.</summary>
    public int Easting { get; }

    /// <summary>Northing digits inside the 100 km square, at <see cref="Precision"/>.</summary>
    public int Northing { get; }

    /// <summary>How many digits per axis the reference carries.</summary>
    public MgrsPrecision Precision { get; }

    public MgrsCoordinate(int zone, char band, char column, char row, int easting, int northing, MgrsPrecision precision)
    {
        Zone = zone;
        Band = char.ToUpperInvariant(band);
        Column = char.ToUpperInvariant(column);
        Row = char.ToUpperInvariant(row);
        Easting = easting;
        Northing = northing;
        Precision = precision;
    }

    /// <summary>The grid zone designator — zone number plus band letter, e.g. <c>39S</c>.</summary>
    public string GridZoneDesignator => Zone.ToString(CultureInfo.InvariantCulture) + Band;

    /// <summary>The 100 km square identifier, e.g. <c>WV</c>.</summary>
    public string SquareId => new string(new[] { Column, Row });

    /// <summary>The written reference, with the conventional spaces: <c>39S WV 53516 39501</c>.</summary>
    public override string ToString() => ToString(withSpaces: true);

    /// <summary>
    /// The written reference. Spaces are for reading; the canonical machine form
    /// (<c>39SWV5351639501</c>) leaves them out.
    /// </summary>
    public string ToString(bool withSpaces)
    {
        var digits = (int)Precision;

        var builder = new StringBuilder(20);

        builder.Append(Zone.ToString(CultureInfo.InvariantCulture));
        builder.Append(Band);

        if (withSpaces)
            builder.Append(' ');

        builder.Append(Column);
        builder.Append(Row);

        if (digits > 0)
        {
            if (withSpaces)
                builder.Append(' ');

            builder.Append(Easting.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0'));

            if (withSpaces)
                builder.Append(' ');

            builder.Append(Northing.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0'));
        }

        return builder.ToString();
    }

    public bool Equals(MgrsCoordinate other)
        => Zone == other.Zone
        && Band == other.Band
        && Column == other.Column
        && Row == other.Row
        && Easting == other.Easting
        && Northing == other.Northing
        && Precision == other.Precision;

    public override bool Equals(object? obj) => obj is MgrsCoordinate other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Zone;
            hash = (hash * 397) ^ Band;
            hash = (hash * 397) ^ Column;
            hash = (hash * 397) ^ Row;
            hash = (hash * 397) ^ Easting;
            hash = (hash * 397) ^ Northing;
            hash = (hash * 397) ^ (int)Precision;
            return hash;
        }
    }

    public static bool operator ==(MgrsCoordinate first, MgrsCoordinate second) => first.Equals(second);

    public static bool operator !=(MgrsCoordinate first, MgrsCoordinate second) => !first.Equals(second);
}

using System;
using System.Globalization;

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>
/// The one unit the identify geometry strip shows its measures in: a length unit, whose square is used for
/// areas (km → km for length and perimeter, km² for area). <see cref="Auto"/> picks m or km by size.
/// </summary>
public enum MeasureDisplayUnit
{
    Auto,
    Meter,
    Kilometer,
    Foot,
    Yard,
    Mile,
}

/// <summary>One entry of the unit combo box.</summary>
public sealed record MeasureDisplayUnitOption(MeasureDisplayUnit Unit, string Label);

/// <summary>Formats geodesic lengths (metres) and areas (square metres) in a <see cref="MeasureDisplayUnit"/>; Latin digits.</summary>
public static class MeasureDisplayFormatter
{
    /// <summary>The unit's symbol; empty for <see cref="MeasureDisplayUnit.Auto"/>.</summary>
    public static string GetSymbol(MeasureDisplayUnit unit) => unit switch
    {
        MeasureDisplayUnit.Meter => "m",
        MeasureDisplayUnit.Kilometer => "km",
        MeasureDisplayUnit.Foot => "ft",
        MeasureDisplayUnit.Yard => "yd",
        MeasureDisplayUnit.Mile => "mi",
        _ => string.Empty,
    };

    /// <summary>A length in metres expressed in <paramref name="unit"/> (Auto: metres).</summary>
    public static double FromMeters(double meters, MeasureDisplayUnit unit) => unit switch
    {
        MeasureDisplayUnit.Kilometer => meters / 1_000,
        MeasureDisplayUnit.Foot => UnitConversion.MeterToFoot(meters),
        MeasureDisplayUnit.Yard => UnitConversion.MeterToYard(meters),
        MeasureDisplayUnit.Mile => UnitConversion.MeterToMile(meters),
        _ => meters,
    };

    public static string FormatLength(double meters, MeasureDisplayUnit unit) =>
        unit == MeasureDisplayUnit.Auto
            ? UnitHelper.GetLengthLabel(meters)
            : $"{FormatNumber(FromMeters(meters, unit))} {GetSymbol(unit)}";

    /// <summary>An area in square metres, in the square of <paramref name="unit"/>.</summary>
    public static string FormatArea(double squareMeters, MeasureDisplayUnit unit)
    {
        if (unit == MeasureDisplayUnit.Auto)
            return UnitHelper.GetAreaLabel(squareMeters);

        var factor = FromMeters(1, unit);

        return $"{FormatNumber(squareMeters * factor * factor)} {GetSymbol(unit)}\xB2";
    }

    /// <summary>Two decimals from 1 up; small values (e.g. a parcel in mi²) keep enough digits not to read 0.</summary>
    private static string FormatNumber(double value)
    {
        var magnitude = Math.Abs(value);

        var format = magnitude >= 1 || magnitude == 0 ? "#,0.##"
                   : magnitude >= 0.01 ? "0.####"
                   : "0.########";

        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}

using System;

using IRI.Maptor.Sta.Spatial.Helpers;

namespace IRI.Maptor.Jab.Common.Models;

public class ScaleInterval : IEquatable<ScaleInterval>
{
    public static readonly ScaleInterval All = new ScaleInterval(double.NegativeInfinity, double.PositiveInfinity);

    public double Lower { get; set; }

    public double Upper { get; set; }

    public ScaleInterval(double lower, double upper)
    {
        if (lower > upper)
            throw new ArgumentOutOfRangeException(nameof(lower), "Lower must be less than or equal to upper.");

        Lower = lower;
        Upper = upper;
    }

    public override bool Equals(object? obj) => obj is ScaleInterval other && Equals(other);

    public bool Equals(ScaleInterval? other)
    {
        return other is not null && Lower.Equals(other.Lower) && Upper.Equals(other.Upper);
    }

    public override int GetHashCode() => HashCode.Combine(Lower, Upper);

    public override string ToString()
    {
        return string.Format("Lower: {0}, Upper: {1}", Lower, Upper);
    }

    public static bool operator ==(ScaleInterval? left, ScaleInterval? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(ScaleInterval? left, ScaleInterval? right) => !(left == right);

    public static ScaleInterval Create(int minGoogleZoomLevel, int? maxGoogleZoomLevel = null)
    {
        if (maxGoogleZoomLevel != null && maxGoogleZoomLevel < minGoogleZoomLevel)
            throw new ArgumentOutOfRangeException(nameof(maxGoogleZoomLevel), "Max zoom level must be greater than or equal to min zoom level.");

        var minInverse = 1.0 / WebMercatorUtility.GetGoogleMapScale(minGoogleZoomLevel) + .5;

        var maxInverse = maxGoogleZoomLevel.HasValue ? 1.0 / WebMercatorUtility.GetGoogleMapScale(maxGoogleZoomLevel.Value) - .5 : 0;

        return new ScaleInterval(maxInverse, minInverse);
    }

    public bool IsInRange(double inverseMapScale) => Upper >= inverseMapScale && Lower < inverseMapScale;
}

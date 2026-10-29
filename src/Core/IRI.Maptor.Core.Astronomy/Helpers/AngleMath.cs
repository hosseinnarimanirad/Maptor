namespace IRI.Maptor.Core.Astronomy;

internal static class AngleMath
{
    internal const double DegreesToRadians = Math.PI / 180.0;

    internal const double RadiansToDegrees = 180.0 / Math.PI;

    internal static double SinDeg(double degrees) => Math.Sin(degrees * DegreesToRadians);

    internal static double CosDeg(double degrees) => Math.Cos(degrees * DegreesToRadians);

    /// <summary>Wraps an angle into [0, 360).</summary>
    internal static double Normalize360(double degrees)
    {
        var result = degrees % 360.0;

        return result < 0 ? result + 360.0 : result;
    }

    /// <summary>Wraps an angle into [-180, 180).</summary>
    internal static double Normalize180(double degrees)
    {
        var result = Normalize360(degrees);

        return result >= 180.0 ? result - 360.0 : result;
    }

    internal static void ValidateLocation(Common.Primitives.Point location)
    {
        if (location is null)
            throw new ArgumentNullException(nameof(location));

        if (location.Y < -90 || location.Y > 90 || double.IsNaN(location.Y))
            throw new ArgumentOutOfRangeException(nameof(location), "Latitude (Y) must be in [-90, 90] degrees.");

        if (double.IsNaN(location.X) || double.IsInfinity(location.X))
            throw new ArgumentOutOfRangeException(nameof(location), "Longitude (X) must be a finite number of degrees.");
    }
}

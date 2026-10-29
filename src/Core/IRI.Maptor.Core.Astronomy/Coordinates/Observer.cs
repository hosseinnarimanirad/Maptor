using IRI.Maptor.Core.Astronomy.Time;
using IRI.Maptor.Core.Common.Primitives;
using static IRI.Maptor.Core.Astronomy.AngleMath;

namespace IRI.Maptor.Core.Astronomy.Coordinates;

/// <summary>
/// Converts celestial directions into what an observer on the Earth sees.
/// </summary>
/// <remarks>Locations are geographic <see cref="Point"/>s: X = longitude, Y = latitude, in degrees.</remarks>
public static class Observer
{
    /// <summary>Local hour angle of a body, degrees in [-180, 180): negative before it transits, positive after.</summary>
    public static double GetHourAngle(EquatorialCoordinate body, double longitude, DateTime utc)
    {
        return Normalize180(SiderealTime.LocalApparent(utc, longitude) - body.RightAscension);
    }

    public static HorizontalCoordinate ToHorizontal(EquatorialCoordinate body, Point location, DateTime utc)
    {
        ValidateLocation(location);

        var hourAngle = GetHourAngle(body, location.X, utc) * DegreesToRadians;

        var latitude = location.Y * DegreesToRadians;

        var declination = body.Declination * DegreesToRadians;

        var sinElevation = Math.Sin(latitude) * Math.Sin(declination) + Math.Cos(latitude) * Math.Cos(declination) * Math.Cos(hourAngle);

        var elevation = Math.Asin(Math.Max(-1.0, Math.Min(1.0, sinElevation)));

        var azimuth = Math.Atan2(-Math.Cos(declination) * Math.Sin(hourAngle),
                                 Math.Sin(declination) * Math.Cos(latitude) - Math.Cos(declination) * Math.Sin(latitude) * Math.Cos(hourAngle));

        return new HorizontalCoordinate(Normalize360(azimuth * RadiansToDegrees), elevation * RadiansToDegrees);
    }
}

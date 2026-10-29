using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Satellite.Frames;

namespace IRI.Maptor.Core.Satellite.Passes;

/// <summary>Whether a satellite is in sunlight or in the Earth's shadow.</summary>
/// <remarks>
/// The shadow is taken as a cylinder of the Earth's equatorial radius, pointing away from the Sun. The
/// penumbra is ignored: for a low orbit it lasts a few seconds at each edge of the shadow.
/// </remarks>
public static class Sunlight
{
    private const double DegreesToRadians = Math.PI / 180.0;

    /// <summary>Whether a satellite at <paramref name="ecef"/> (km) is lit by the Sun at <paramref name="utc"/>.</summary>
    public static bool IsSunlit(Vector3 ecef, DateTime utc)
    {
        var sun = SunDirection(utc);

        var along = ecef.X * sun.X + ecef.Y * sun.Y + ecef.Z * sun.Z;

        // on the day side of the plane through the Earth's centre
        if (along >= 0)
            return true;

        var perpendicular = ecef - along * sun;

        return perpendicular.Length > Wgs84Geodetic.SemiMajorAxisKm;
    }

    public static bool IsSunlit(SatellitePosition position)
    {
        if (position is null)
            throw new ArgumentNullException(nameof(position));

        return IsSunlit(position.Ecef, position.Utc);
    }

    /// <summary>Unit vector towards the Sun in the Earth-fixed frame: through the sub-solar point.</summary>
    public static Vector3 SunDirection(DateTime utc)
    {
        var subSolar = Sun.GetSubSolarPoint(utc);

        var lat = subSolar.Y * DegreesToRadians;
        var lon = subSolar.X * DegreesToRadians;

        return new Vector3(Math.Cos(lat) * Math.Cos(lon), Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat));
    }
}

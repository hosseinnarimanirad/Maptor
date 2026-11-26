using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Helpers;

/// <summary>
/// Metres around one place, for measuring short distances the same way everywhere.
/// <para>
/// Geodetic data (WGS84 longitude/latitude) is flattened onto a local plane centred on the place, with the
/// WGS84 metres-per-degree series at its latitude: well under a millimetre off within a few hundred metres.
/// Use it only between things that are near each other; never flatten a whole layer with one frame.
/// </para>
/// <para>
/// Any other SRS is taken as already being in metres, so the frame only moves the origin. Web Mercator is
/// not metres on the ground and must be converted to geodetic coordinates before it is measured.
/// </para>
/// </summary>
public readonly struct LocalMetricFrame
{
    // A rounded figure for search boxes only: together with the margin in SearchHalfSize it gives a box a
    // little larger than needed, never smaller.
    private const double RoughMetresPerDegree = 111_320;

    private LocalMetricFrame(double originX, double originY, double metresPerUnitX, double metresPerUnitY, bool isGeodetic)
    {
        OriginX = originX;
        OriginY = originY;
        MetresPerUnitX = metresPerUnitX;
        MetresPerUnitY = metresPerUnitY;
        IsGeodetic = isGeodetic;
    }

    public double OriginX { get; }

    public double OriginY { get; }

    /// <summary>Metres per coordinate unit eastward (per degree of longitude when geodetic).</summary>
    public double MetresPerUnitX { get; }

    /// <summary>Metres per coordinate unit northward (per degree of latitude when geodetic).</summary>
    public double MetresPerUnitY { get; }

    public bool IsGeodetic { get; }

    public static LocalMetricFrame At<T>(T origin, bool isGeodetic) where T : IPoint => At(origin.X, origin.Y, isGeodetic);

    /// <param name="x">Longitude in degrees when geodetic.</param>
    /// <param name="y">Latitude in degrees when geodetic.</param>
    /// <param name="isGeodetic">True for WGS84 longitude/latitude; false for an SRS in metres.</param>
    public static LocalMetricFrame At(double x, double y, bool isGeodetic)
    {
        if (!isGeodetic)
            return new LocalMetricFrame(x, y, 1, 1, false);

        var latitude = y * Math.PI / 180.0;

        var perDegreeY = 111132.92 - 559.82 * Math.Cos(2 * latitude) + 1.175 * Math.Cos(4 * latitude) - 0.0023 * Math.Cos(6 * latitude);
        var perDegreeX = 111412.84 * Math.Cos(latitude) - 93.5 * Math.Cos(3 * latitude) + 0.118 * Math.Cos(5 * latitude);

        return new LocalMetricFrame(x, y, perDegreeX, perDegreeY, true);
    }

    /// <summary>Metres east and north of the origin.</summary>
    public (double East, double North) ToMetres(double x, double y) => ((x - OriginX) * MetresPerUnitX, (y - OriginY) * MetresPerUnitY);

    /// <summary>Metres east and north of the origin.</summary>
    public (double East, double North) ToMetres<T>(T point) where T : IPoint => ToMetres(point.X, point.Y);

    /// <summary>
    /// Half the width and height, in coordinate units, of a box around the origin that holds everything
    /// within <paramref name="metres"/> of it. Geodetic boxes are deliberately a little too large, so that
    /// rounding never loses something right at the distance.
    /// </summary>
    public (double HalfWidth, double HalfHeight) SearchHalfSize(double metres)
    {
        if (!IsGeodetic)
            return (metres, metres);

        var padded = metres * 1.01 + 0.001;

        var cos = Math.Max(Math.Cos(OriginY * Math.PI / 180.0), 0.01);

        return (padded / (RoughMetresPerDegree * cos), padded / RoughMetresPerDegree);
    }
}

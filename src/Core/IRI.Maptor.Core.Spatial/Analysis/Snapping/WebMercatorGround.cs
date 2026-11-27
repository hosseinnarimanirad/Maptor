using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// Ground measures of a straight segment drawn in Web Mercator, and the way back: the point at a ground distance
/// along a map direction. The one place of the drawing aids that knows the projection.
/// <para>
/// Web Mercator keeps angles and its north is straight up, so a direction on the map is the direction on the ground.
/// It does not keep lengths: a map unit is <c>cos(latitude)</c> metres, about 0.82 at 35° north. A length here is the
/// ellipsoidal distance between the segment's two ends, the same value a map shows as the edge's length, so a
/// point placed with <see cref="PointAt{T}"/> gives an edge whose label reads the asked length.
/// </para>
/// </summary>
public static class WebMercatorGround
{
    // PointAt stops when the edge is within this of the asked length (metres)
    private const double LengthTolerance = 1e-4;

    private const int MaxCorrections = 12;

    /// <summary>The ground length of the segment, in metres.</summary>
    public static double Length<T>(T start, T end) where T : IPoint, new()
        => SpatialUtility.GetEllipsoidalLength(MapProjects.WebMercatorToGeodeticWgs84(start), MapProjects.WebMercatorToGeodeticWgs84(end));

    /// <summary>The direction from <paramref name="start"/> to <paramref name="end"/>, in degrees clockwise from north, 0 up to 360; 0 for a segment of no length.</summary>
    public static double Bearing<T>(T start, T end) where T : IPoint
        => BearingOf(end.X - start.X, end.Y - start.Y);

    /// <summary>The bearing of a map direction, in degrees clockwise from north, 0 up to 360.</summary>
    public static double BearingOf(double dx, double dy)
    {
        if (dx == 0 && dy == 0)
            return 0;

        var degrees = Math.Atan2(dx, dy) * 180.0 / Math.PI;

        return degrees < 0 ? degrees + 360 : degrees;
    }

    /// <summary>The map direction (a unit vector) of a bearing in degrees clockwise from north.</summary>
    public static (double X, double Y) DirectionOf(double bearingDegrees)
    {
        var radians = bearingDegrees * Math.PI / 180.0;

        return (Math.Sin(radians), Math.Cos(radians));
    }

    /// <summary>
    /// How far the segment <paramref name="last"/>→<paramref name="next"/> turns from the segment
    /// <paramref name="previous"/>→<paramref name="last"/>, in degrees: 0 straight on, positive to the right
    /// (clockwise), negative to the left, above -180 up to 180.
    /// </summary>
    public static double TurnAngle<T>(T previous, T last, T next) where T : IPoint
    {
        var turn = Bearing(last, next) - Bearing(previous, last);

        while (turn > 180) turn -= 360;
        while (turn <= -180) turn += 360;

        return turn;
    }

    /// <summary>
    /// The point along the map direction (<paramref name="directionX"/>, <paramref name="directionY"/>) from
    /// <paramref name="start"/> whose ground distance from it is <paramref name="metres"/>. The direction need not be
    /// a unit vector. Returns <paramref name="start"/>'s position for no direction or a length of zero or less.
    /// </summary>
    public static T PointAt<T>(T start, double directionX, double directionY, double metres) where T : IPoint, new()
    {
        var norm = Math.Sqrt(directionX * directionX + directionY * directionY);

        if (norm <= 0 || !(metres > 0) || double.IsInfinity(metres))
            return new T { X = start.X, Y = start.Y };

        var ux = directionX / norm;
        var uy = directionY / norm;

        // first guess from the projection's scale at the start: a map unit is cos(latitude) metres
        var latitude = MapProjects.WebMercatorToGeodeticWgs84(start).Y * Math.PI / 180.0;
        var cos = Math.Max(Math.Cos(latitude), 1e-6);

        var distance = metres / cos;

        var point = new T { X = start.X + distance * ux, Y = start.Y + distance * uy };

        // the ground length grows almost in proportion to the map length, so scaling by the miss converges fast
        for (int i = 0; i < MaxCorrections; i++)
        {
            var length = Length(start, point);

            if (!(length > 0) || Math.Abs(length - metres) <= LengthTolerance)
                break;

            distance *= metres / length;

            point = new T { X = start.X + distance * ux, Y = start.Y + distance * uy };
        }

        return point;
    }
}

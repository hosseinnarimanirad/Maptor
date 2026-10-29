using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite.Tracks;

namespace IRI.Maptor.Core.Satellite.Passes;

/// <summary>
/// The place passes are found for: one point, or a polygon. Points are X = longitude, Y = latitude,
/// degrees; the ring is not closed (the first point is not repeated).
/// </summary>
/// <remarks>
/// A polygon must not cross the ±180° meridian or contain a pole: it is tested for "inside" as a plain
/// longitude/latitude shape. Distances to it are measured along great circles.
/// </remarks>
public sealed class PassRegion
{
    private const double DegreesToRadians = Math.PI / 180.0;

    private readonly Point[] _ring;

    private PassRegion(Point[] ring)
    {
        _ring = ring;

        Center = ring.Length == 1 ? ring[0] : new Point(ring.Average(p => p.X), ring.Average(p => p.Y));
    }

    /// <summary>A single place on the ground.</summary>
    public static PassRegion FromPoint(Point location)
    {
        if (location is null)
            throw new ArgumentNullException(nameof(location));

        CheckLatitude(location.Y);

        return new PassRegion(new[] { new Point(GreatCircle.NormalizeLongitude(location.X), location.Y) });
    }

    /// <summary>An area; at least three vertices, not closed.</summary>
    public static PassRegion FromPolygon(IEnumerable<Point> ring)
    {
        if (ring is null)
            throw new ArgumentNullException(nameof(ring));

        var points = ring.Select(p => new Point(p.X, p.Y)).ToArray();

        if (points.Length < 3)
            throw new ArgumentException("A polygon region needs at least three vertices.", nameof(ring));

        foreach (var p in points)
            CheckLatitude(p.Y);

        if (points.Max(p => p.X) - points.Min(p => p.X) >= 180)
            throw new ArgumentException("A polygon region must not cross the ±180° meridian.", nameof(ring));

        return new PassRegion(points);
    }

    /// <summary>True for a single point.</summary>
    public bool IsPoint => _ring.Length == 1;

    /// <summary>The point, or the polygon's vertices.</summary>
    public IReadOnlyList<Point> Vertices => _ring;

    /// <summary>The point, or the mean of the vertices; where the Sun is judged for the whole region.</summary>
    public Point Center { get; }

    /// <summary>Great-circle distance from <paramref name="location"/> to the region, km; 0 inside it.</summary>
    public double DistanceKm(Point location)
    {
        if (IsPoint)
            return GreatCircle.DistanceKm(location, _ring[0]);

        if (Contains(location))
            return 0;

        var p = ToUnit(location);

        var best = double.MaxValue;

        for (var i = 0; i < _ring.Length; i++)
            best = Math.Min(best, SegmentDistanceKm(p, _ring[i], _ring[(i + 1) % _ring.Length]));

        return best;
    }

    /// <summary>Whether <paramref name="location"/> is inside the polygon (always false for a point region).</summary>
    public bool Contains(Point location)
    {
        if (IsPoint)
            return false;

        // bring the longitude next to the polygon's, so a point given as −179° finds a polygon near 181°
        var x = location.X;

        while (x < Center.X - 180) x += 360;
        while (x > Center.X + 180) x -= 360;

        var inside = false;

        for (int i = 0, j = _ring.Length - 1; i < _ring.Length; j = i++)
        {
            var a = _ring[i];
            var b = _ring[j];

            if ((a.Y > location.Y) != (b.Y > location.Y)
                && x < (b.X - a.X) * (location.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }

        return inside;
    }

    /// <summary>
    /// Points spread over the region, for measuring how much of it a swath covers: a grid of about
    /// <paramref name="target"/> points inside the polygon, or the point itself.
    /// </summary>
    public IReadOnlyList<Point> SamplePoints(int target = 400)
    {
        if (IsPoint)
            return _ring;

        var minX = _ring.Min(p => p.X);
        var maxX = _ring.Max(p => p.X);
        var minY = _ring.Min(p => p.Y);
        var maxY = _ring.Max(p => p.Y);

        var n = Math.Max(2, (int)Math.Ceiling(Math.Sqrt(target)));

        var samples = new List<Point>();

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var p = new Point(minX + (i + 0.5) * (maxX - minX) / n, minY + (j + 0.5) * (maxY - minY) / n);

                if (Contains(p))
                    samples.Add(p);
            }
        }

        // a sliver thinner than the grid: its vertices stand in for it
        return samples.Count > 0 ? samples : _ring;
    }

    private static void CheckLatitude(double latitude)
    {
        if (double.IsNaN(latitude) || latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be in [−90, 90].");
    }

    /// <summary>Distance from unit vector <paramref name="p"/> to the great-circle arc a→b, km.</summary>
    private static double SegmentDistanceKm(double[] p, Point a, Point b)
    {
        var ua = ToUnit(a);
        var ub = ToUnit(b);

        var n = Cross(ua, ub);
        var nLength = Math.Sqrt(Dot(n, n));

        if (nLength < 1e-12)
            return GreatCircle.DistanceKm(FromUnit(p), a);

        n = new[] { n[0] / nLength, n[1] / nLength, n[2] / nLength };

        // the foot of the perpendicular lies on the arc when it is between a and b
        var pn = Dot(p, n);
        var foot = new[] { p[0] - pn * n[0], p[1] - pn * n[1], p[2] - pn * n[2] };

        if (Dot(Cross(ua, foot), n) >= 0 && Dot(Cross(foot, ub), n) >= 0)
            return Math.Abs(Math.Asin(Math.Max(-1, Math.Min(1, pn)))) * GreatCircle.MeanEarthRadiusKm;

        var location = FromUnit(p);

        return Math.Min(GreatCircle.DistanceKm(location, a), GreatCircle.DistanceKm(location, b));
    }

    private static double[] ToUnit(Point p)
    {
        var lat = p.Y * DegreesToRadians;
        var lon = p.X * DegreesToRadians;

        return new[] { Math.Cos(lat) * Math.Cos(lon), Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat) };
    }

    private static Point FromUnit(double[] v)
        => new(Math.Atan2(v[1], v[0]) / DegreesToRadians, Math.Asin(Math.Max(-1, Math.Min(1, v[2]))) / DegreesToRadians);

    private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

    private static double[] Cross(double[] a, double[] b)
        => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
}

using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>One part of a <see cref="MeasuredLine{T}"/>: its vertices and the distance along it to each.</summary>
public sealed class MeasuredPart<T> where T : IPoint, new()
{
    internal MeasuredPart(int index, List<T> vertices, double[] cumulative, bool isRing)
    {
        Index = index;
        Vertices = vertices;
        Cumulative = cumulative;
        IsRing = isRing;
    }

    /// <summary>The part's position in the geometry (line part, or ring number counted over all polygons).</summary>
    public int Index { get; }

    /// <summary>The vertices; for a ring the first vertex is repeated at the end, so the closing edge is measured.</summary>
    public IReadOnlyList<T> Vertices { get; }

    /// <summary>Distance along the part to each vertex (metres when geodetic, coordinate units otherwise).</summary>
    public IReadOnlyList<double> Cumulative { get; }

    public bool IsRing { get; }

    public double Length => Cumulative[Cumulative.Count - 1];

    public int SegmentCount => Vertices.Count - 1;

    /// <summary>The point at <paramref name="measure"/> along the part, interpolated in coordinates.</summary>
    public T PointAt(double measure)
    {
        measure = Math.Max(0, Math.Min(Length, measure));

        var s = SegmentAt(measure);
        var length = Cumulative[s + 1] - Cumulative[s];
        var f = length <= 0 ? 0 : (measure - Cumulative[s]) / length;

        return new T { X = Vertices[s].X + f * (Vertices[s + 1].X - Vertices[s].X), Y = Vertices[s].Y + f * (Vertices[s + 1].Y - Vertices[s].Y) };
    }

    /// <summary>The stretch between two measures as a line string, in the part's coordinates.</summary>
    public Geometry<T> Extract(double from, double to, int srid)
    {
        if (to < from)
            (from, to) = (to, from);

        var points = new List<T> { PointAt(from) };

        for (int v = 1; v < Vertices.Count - 1; v++)
        {
            if (Cumulative[v] > from && Cumulative[v] < to)
                points.Add(Vertices[v]);
        }

        points.Add(PointAt(to));

        return Geometry<T>.Create(points, GeometryType.LineString, srid);
    }

    /// <summary>The index of the segment that holds <paramref name="measure"/>.</summary>
    public int SegmentAt(double measure)
    {
        int low = 0, high = Cumulative.Count - 2;

        while (low < high)
        {
            var mid = (low + high + 1) / 2;

            if (Cumulative[mid] <= measure)
                low = mid;
            else
                high = mid - 1;
        }

        return low;
    }
}

/// <summary>
/// A line, multi-line or polygon boundary prepared for measuring: its parts with the distance along each part to
/// every vertex. Geodetic WGS84 lengths are ellipsoidal metres; any other SRS is measured in its own units.
/// Vertices that are not finite are dropped, and parts with fewer than two usable vertices or no length are left
/// out.
/// </summary>
public sealed class MeasuredLine<T> where T : IPoint, new()
{
    private MeasuredLine(Geometry<T> geometry, IReadOnlyList<MeasuredPart<T>> parts, bool isGeodetic)
    {
        Geometry = geometry;
        Parts = parts;
        IsGeodetic = isGeodetic;
    }

    public Geometry<T> Geometry { get; }

    public IReadOnlyList<MeasuredPart<T>> Parts { get; }

    public bool IsGeodetic { get; }

    public int Srid => Geometry.Srid;

    public double Length => Parts.Sum(p => p.Length);

    public bool IsEmpty => Parts.Count == 0;

    /// <summary>The part with <see cref="MeasuredPart{T}.Index"/> <paramref name="index"/>, or null when it was left out.</summary>
    public MeasuredPart<T>? PartWithIndex(int index) => Parts.FirstOrDefault(p => p.Index == index);

    /// <summary>The parts of a line string or multi-line string.</summary>
    public static MeasuredLine<T> FromLine(Geometry<T> geometry, bool isGeodetic)
    {
        if (geometry is null)
            throw new ArgumentNullException(nameof(geometry));

        var parts = new List<MeasuredPart<T>>();

        var index = 0;

        foreach (var points in LinePartsOf(geometry))
        {
            AddPart(parts, index++, points, isRing: false, isGeodetic);
        }

        return new MeasuredLine<T>(geometry, parts, isGeodetic);
    }

    /// <summary>The rings of a polygon or multipolygon as closed parts, numbered over all polygons in order.</summary>
    public static MeasuredLine<T> FromRings(Geometry<T> geometry, bool isGeodetic)
    {
        if (geometry is null)
            throw new ArgumentNullException(nameof(geometry));

        var parts = new List<MeasuredPart<T>>();

        var index = 0;

        foreach (var polygon in PolygonMeasure.PolygonsOf(geometry))
        {
            foreach (var ring in PolygonMeasure.RingsOf(polygon))
            {
                var closed = new List<T>(ring) { ring[0] };

                AddPart(parts, index++, closed, isRing: true, isGeodetic);
            }
        }

        return new MeasuredLine<T>(geometry, parts, isGeodetic);
    }

    private static void AddPart(List<MeasuredPart<T>> parts, int index, IEnumerable<T> points, bool isRing, bool isGeodetic)
    {
        var vertices = points.Where(p => p is not null && IsFinite(p.X) && IsFinite(p.Y)).ToList();

        if (vertices.Count < 2)
            return;

        var cumulative = new double[vertices.Count];

        for (int v = 1; v < vertices.Count; v++)
            cumulative[v] = cumulative[v - 1] + SegmentLength(vertices[v - 1], vertices[v], isGeodetic);

        if (cumulative[cumulative.Length - 1] <= 0)
            return;

        parts.Add(new MeasuredPart<T>(index, vertices, cumulative, isRing));
    }

    private static IEnumerable<List<T>> LinePartsOf(Geometry<T> geometry)
    {
        if (geometry.IsNullOrEmpty())
            yield break;

        if (geometry.Geometries is { Count: > 0 })
        {
            foreach (var child in geometry.Geometries.Where(g => g is not null))
            {
                foreach (var part in LinePartsOf(child))
                    yield return part;
            }
        }
        else if (geometry.Points is { Count: > 0 })
        {
            yield return geometry.Points;
        }
    }

    private static double SegmentLength(T a, T b, bool isGeodetic)
    {
        if (!isGeodetic)
            return SpatialUtility.GetEuclideanLength(a, b);

        var length = SpatialUtility.GetEllipsoidalLength(a, b);

        return IsFinite(length) ? length : 0;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

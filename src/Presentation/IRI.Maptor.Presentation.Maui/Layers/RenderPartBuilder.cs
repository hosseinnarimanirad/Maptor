using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// Flattens a <see cref="Geometry{Point}"/> into drawable <see cref="RenderPart"/>s, mapping each
/// coordinate through a caller-supplied transform. GeoJSON layers pass the WGS84→WebMercator
/// projection; vector MBTiles layers pass identity because their features are already WebMercator.
/// The <c>track</c> callback receives every produced world coordinate so callers can
/// accumulate a bounding box. When a <c>feature</c> is given, every part is linked to it (and
/// added to its parts) and the feature's extent is set.
/// </summary>
internal static class RenderPartBuilder
{
    public static void Build(
        Geometry<Point> geometry,
        Func<Point, (double X, double Y)> toWorld,
        List<RenderPart> parts,
        Action<double, double> track,
        MapFeature? feature = null)
    {
        int first = parts.Count;

        BuildParts(geometry, toWorld, parts, track, feature);

        if (feature is null || parts.Count == first)
        {
            return;
        }

        double xMin = double.MaxValue, yMin = double.MaxValue, xMax = double.MinValue, yMax = double.MinValue;

        for (int i = first; i < parts.Count; i++)
        {
            var part = parts[i];
            feature.Parts.Add(part);
            xMin = Math.Min(xMin, part.XMin);
            yMin = Math.Min(yMin, part.YMin);
            xMax = Math.Max(xMax, part.XMax);
            yMax = Math.Max(yMax, part.YMax);
        }

        if (xMax >= xMin && yMax >= yMin)
        {
            feature.Extent = new BoundingBox(xMin, yMin, xMax, yMax);
        }
    }

    private static void BuildParts(
        Geometry<Point> geometry,
        Func<Point, (double X, double Y)> toWorld,
        List<RenderPart> parts,
        Action<double, double> track,
        MapFeature? feature)
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                parts.Add(MakePart(RenderKind.Point, new[] { geometry.GetAllPoints() }, toWorld, track, feature));
                break;

            case GeometryType.LineString:
                parts.Add(MakePart(RenderKind.Line, new[] { geometry.Points ?? new List<Point>() }, toWorld, track, feature));
                break;

            case GeometryType.MultiLineString:
                parts.Add(MakePart(RenderKind.Line, Rings(geometry), toWorld, track, feature));
                break;

            case GeometryType.Polygon:
                parts.Add(MakePart(RenderKind.Polygon, Rings(geometry), toWorld, track, feature));
                break;

            case GeometryType.MultiPolygon:
                foreach (var polygon in geometry.Geometries ?? new List<Geometry<Point>>())
                {
                    parts.Add(MakePart(RenderKind.Polygon, Rings(polygon), toWorld, track, feature));
                }

                break;

            case GeometryType.GeometryCollection:
                foreach (var sub in geometry.Geometries ?? new List<Geometry<Point>>())
                {
                    BuildParts(sub, toWorld, parts, track, feature);
                }

                break;
        }
    }

    private static IEnumerable<List<Point>> Rings(Geometry<Point> geometry)
        => (geometry.Geometries ?? new List<Geometry<Point>>()).Select(g => g.Points ?? new List<Point>());

    private static RenderPart MakePart(
        RenderKind kind,
        IEnumerable<List<Point>> rings,
        Func<Point, (double X, double Y)> toWorld,
        Action<double, double> track,
        MapFeature? feature)
    {
        var projectedRings = new List<(double[] X, double[] Y)>();

        double xMin = double.MaxValue, yMin = double.MaxValue, xMax = double.MinValue, yMax = double.MinValue;

        foreach (var ring in rings)
        {
            var count = ring.Count;
            var xs = new double[count];
            var ys = new double[count];

            for (int i = 0; i < count; i++)
            {
                var (x, y) = toWorld(ring[i]);
                xs[i] = x;
                ys[i] = y;
                track(x, y);

                if (x < xMin) xMin = x;
                if (y < yMin) yMin = y;
                if (x > xMax) xMax = x;
                if (y > yMax) yMax = y;
            }

            projectedRings.Add((xs, ys));
        }

        return new RenderPart
        {
            Kind = kind,
            Rings = projectedRings,
            Feature = feature,
            XMin = xMin,
            YMin = yMin,
            XMax = xMax,
            YMax = yMax,
        };
    }

    /// <summary>A part built directly from WebMercator rings (sketches drawn on the map).</summary>
    internal static RenderPart FromWorldRings(RenderKind kind, List<(double[] X, double[] Y)> rings)
    {
        double xMin = double.MaxValue, yMin = double.MaxValue, xMax = double.MinValue, yMax = double.MinValue;

        foreach (var (xs, ys) in rings)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                xMin = Math.Min(xMin, xs[i]);
                yMin = Math.Min(yMin, ys[i]);
                xMax = Math.Max(xMax, xs[i]);
                yMax = Math.Max(yMax, ys[i]);
            }
        }

        return new RenderPart { Kind = kind, Rings = rings, XMin = xMin, YMin = yMin, XMax = xMax, YMax = yMax };
    }
}

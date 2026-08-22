using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// Flattens a <see cref="Geometry{Point}"/> into drawable <see cref="RenderPart"/>s, mapping each
/// coordinate through a caller-supplied transform. GeoJSON layers pass the WGS84→WebMercator
/// projection; vector MBTiles layers pass identity because their features are already WebMercator.
/// The <paramref name="track"/> callback receives every produced world coordinate so callers can
/// accumulate a bounding box.
/// </summary>
internal static class RenderPartBuilder
{
    public static void Build(
        Geometry<Point> geometry,
        Func<Point, (double X, double Y)> toWorld,
        List<RenderPart> parts,
        Action<double, double> track)
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                parts.Add(MakePart(RenderKind.Point, new[] { geometry.GetAllPoints() }, toWorld, track));
                break;

            case GeometryType.LineString:
                parts.Add(MakePart(RenderKind.Line, new[] { geometry.Points ?? new List<Point>() }, toWorld, track));
                break;

            case GeometryType.MultiLineString:
                parts.Add(MakePart(RenderKind.Line, Rings(geometry), toWorld, track));
                break;

            case GeometryType.Polygon:
                parts.Add(MakePart(RenderKind.Polygon, Rings(geometry), toWorld, track));
                break;

            case GeometryType.MultiPolygon:
                foreach (var polygon in geometry.Geometries ?? new List<Geometry<Point>>())
                {
                    parts.Add(MakePart(RenderKind.Polygon, Rings(polygon), toWorld, track));
                }

                break;

            case GeometryType.GeometryCollection:
                foreach (var sub in geometry.Geometries ?? new List<Geometry<Point>>())
                {
                    Build(sub, toWorld, parts, track);
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
        Action<double, double> track)
    {
        var projectedRings = new List<(double[] X, double[] Y)>();

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
            }

            projectedRings.Add((xs, ys));
        }

        return new RenderPart { Kind = kind, Rings = projectedRings };
    }
}

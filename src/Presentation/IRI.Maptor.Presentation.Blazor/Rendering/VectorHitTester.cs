using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Presentation.Blazor.Rendering;

/// <summary>The feature under a click, and the layer it came from.</summary>
public sealed record MapFeatureHit(string LayerId, string LayerTitle, Feature<Point> Feature);

/// <summary>
/// Finds the feature under a screen point.
///
/// Tested in map units rather than screen pixels: the click's pixel tolerance is converted once
/// into a Web Mercator distance, so no geometry has to be projected to answer the question. The
/// search runs top-down through the draw order, because what the user clicked is whatever they
/// can see — the last thing painted.
/// </summary>
public static class VectorHitTester
{
    public static MapFeatureHit? HitTest(
        IEnumerable<MapVectorLayer> layers,
        MapViewport viewport,
        double screenX,
        double screenY,
        double tolerancePixels = 6)
    {
        var clickPoint = viewport.ScreenToWebMercator(screenX, screenY);

        var tolerance = tolerancePixels * viewport.MapUnitsPerPixel;

        // Reverse draw order: topmost layer wins, matching what is visually on top.
        var candidates = layers
            .Where(layer => layer.IsDrawnAt(viewport.ZoomLevel))
            .OrderByDescending(layer => layer.DrawOrder);

        foreach (var layer in candidates)
        {
            var features = layer.Features?.Features;

            if (features is null || features.Count == 0)
                continue;

            var category = layer.Features!.GeometryType.GetCategory();

            // Points are clicked at their drawn radius, which is bigger than the bare tolerance —
            // otherwise a symbol the user can plainly see is not clickable at its edge.
            var effectiveTolerance = category == GeometryCategory.Point
                ? tolerance + 4 * viewport.MapUnitsPerPixel
                : tolerance;

            // Cached, computed once per fetch — see MapVectorLayer.FeatureExtents for why this is
            // never Geometry.GetBoundingBox() in a per-feature loop.
            var extents = layer.FeatureExtents;

            for (var i = 0; i < features.Count; i++)
            {
                var box = extents[i];

                // Rejects the overwhelming majority of a national layer with four comparisons,
                // before any geometry is touched.
                if (double.IsNaN(box.XMin)
                    || clickPoint.X < box.XMin - effectiveTolerance
                    || clickPoint.X > box.XMax + effectiveTolerance
                    || clickPoint.Y < box.YMin - effectiveTolerance
                    || clickPoint.Y > box.YMax + effectiveTolerance)
                {
                    continue;
                }

                var feature = features[i];
                var geometry = feature.TheGeometry;

                if (geometry is null)
                    continue;

                if (IsHit(geometry, category, clickPoint, effectiveTolerance))
                    return new MapFeatureHit(layer.Id, layer.Title, feature);
            }
        }

        return null;
    }

    private static bool IsHit(Geometry<Point> geometry, GeometryCategory category, Point click, double tolerance)
    {
        // Indexed loops rather than LINQ throughout: this recurses over every part and vertex of a
        // candidate feature, and a closure plus an enumerator per part is real cost under WASM.
        if (geometry.Geometries is { Count: > 0 } parts)
        {
            for (var i = 0; i < parts.Count; i++)
            {
                if (IsHit(parts[i], category, click, tolerance))
                    return true;
            }

            return false;
        }

        var points = geometry.Points;

        if (points is null || points.Count == 0)
            return false;

        if (category == GeometryCategory.Point)
        {
            for (var i = 0; i < points.Count; i++)
            {
                if (Distance(points[i].X, points[i].Y, click.X, click.Y) <= tolerance)
                    return true;
            }

            return false;
        }

        // A polygon counts as hit anywhere inside it, and also near its outline — so a thin sliver
        // or a ring the click just misses is still selectable.
        if (category == GeometryCategory.Polygon && IsInsideRing(points, click))
            return true;

        for (var i = 0; i < points.Count - 1; i++)
        {
            if (DistanceToSegment(click, points[i], points[i + 1]) <= tolerance)
                return true;
        }

        return false;
    }

    private static bool IsInsideRing(List<Point> ring, Point click)
    {
        // Standard ray-casting crossing count.
        var isInside = false;

        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var yi = ring[i].Y;
            var yj = ring[j].Y;

            if (yi > click.Y == yj > click.Y)
                continue;

            var xIntersect = (ring[j].X - ring[i].X) * (click.Y - yi) / (yj - yi) + ring[i].X;

            if (click.X < xIntersect)
                isInside = !isInside;
        }

        return isInside;
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;

        var lengthSquared = dx * dx + dy * dy;

        if (lengthSquared <= double.Epsilon)
            return Distance(p.X, p.Y, a.X, a.Y);

        // Projection parameter of p onto ab, clamped to the segment.
        var t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);

        return Distance(p.X, p.Y, a.X + t * dx, a.Y + t * dy);
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}

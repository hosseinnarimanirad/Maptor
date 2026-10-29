using IRI.Maptor.Presentation.Maui.Layers;

namespace IRI.Maptor.Presentation.Maui.Controls;

/// <summary>A feature found under a tap by <see cref="MapViewer.HitTest"/>.</summary>
/// <param name="Layer">The layer the feature belongs to.</param>
/// <param name="Feature">The feature.</param>
/// <param name="DistancePixels">Distance from the tap to the feature's drawn geometry (0 inside a polygon).</param>
public sealed record MapHitResult(MapLayer Layer, MapFeature Feature, double DistancePixels);

/// <summary>
/// Finds the features drawn near a WebMercator location. Works on the layers' render parts, i.e.
/// on what the map shows: hidden layers and layers outside their zoom range are skipped.
/// </summary>
internal static class MapHitTester
{
    /// <param name="layers">The map's layers in draw order (the last is on top).</param>
    /// <param name="x">Tap location, WebMercator.</param>
    /// <param name="y">Tap location, WebMercator.</param>
    /// <param name="resolution">WebMercator units per pixel.</param>
    /// <param name="zoom">Current fractional zoom, for the layers' zoom ranges.</param>
    /// <param name="tolerancePixels">How far from a point or line a tap still counts.</param>
    /// <returns>Top layer first; within a layer, nearest first. One result per feature.</returns>
    public static List<MapHitResult> HitTest(
        IReadOnlyList<MapLayer> layers, double x, double y, double resolution, double zoom, double tolerancePixels)
    {
        var results = new List<MapHitResult>();

        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            if (!layer.IsVisibleAt(zoom) || layer is MbTilesRasterLayer)
            {
                continue;
            }

            // A point symbol counts wherever it is drawn, not only at its centre.
            double pointTolerance = (tolerancePixels + layer.PointSize / 2.0) * resolution;
            double tolerance = tolerancePixels * resolution;

            var best = new Dictionary<MapFeature, double>();

            foreach (var part in layer.Parts)
            {
                if (part.Feature is not { } feature)
                {
                    continue;
                }

                double reach = part.Kind == RenderKind.Point ? pointTolerance : tolerance;

                if (!part.Intersects(x - reach, y - reach, x + reach, y + reach))
                {
                    continue;
                }

                double distance = Distance(part, x, y);

                if (distance <= reach && (!best.TryGetValue(feature, out var previous) || distance < previous))
                {
                    best[feature] = distance;
                }
            }

            foreach (var (feature, distance) in best.OrderBy(b => b.Value))
            {
                results.Add(new MapHitResult(layer, feature, distance / resolution));
            }
        }

        return results;
    }

    /// <summary>World distance from (x, y) to the part; 0 inside a polygon.</summary>
    private static double Distance(RenderPart part, double x, double y)
    {
        switch (part.Kind)
        {
            case RenderKind.Point:
                {
                    double min = double.MaxValue;

                    foreach (var (xs, ys) in part.Rings)
                    {
                        for (int i = 0; i < xs.Length; i++)
                        {
                            min = Math.Min(min, Math.Sqrt((xs[i] - x) * (xs[i] - x) + (ys[i] - y) * (ys[i] - y)));
                        }
                    }

                    return min;
                }

            case RenderKind.Line:
                return DistanceToEdges(part, x, y, closed: false);

            case RenderKind.Polygon:
                return IsInside(part, x, y) ? 0 : DistanceToEdges(part, x, y, closed: true);

            default:
                return double.MaxValue;
        }
    }

    private static double DistanceToEdges(RenderPart part, double x, double y, bool closed)
    {
        double min = double.MaxValue;

        foreach (var (xs, ys) in part.Rings)
        {
            int n = xs.Length;

            if (n == 1)
            {
                min = Math.Min(min, Math.Sqrt((xs[0] - x) * (xs[0] - x) + (ys[0] - y) * (ys[0] - y)));
                continue;
            }

            int edges = closed ? n : n - 1;

            for (int i = 0; i < edges; i++)
            {
                int j = (i + 1) % n;
                min = Math.Min(min, DistanceToSegment(x, y, xs[i], ys[i], xs[j], ys[j]));
            }
        }

        return min;
    }

    private static double DistanceToSegment(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax;
        double dy = by - ay;
        double lengthSquared = dx * dx + dy * dy;

        double t = lengthSquared == 0 ? 0 : Math.Clamp(((px - ax) * dx + (py - ay) * dy) / lengthSquared, 0, 1);

        double cx = ax + t * dx - px;
        double cy = ay + t * dy - py;

        return Math.Sqrt(cx * cx + cy * cy);
    }

    // Even-odd over all rings, as the polygon is filled (holes count as outside).
    private static bool IsInside(RenderPart part, double x, double y)
    {
        bool inside = false;

        foreach (var (xs, ys) in part.Rings)
        {
            int n = xs.Length;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((ys[i] > y) != (ys[j] > y) && x < (xs[j] - xs[i]) * (y - ys[i]) / (ys[j] - ys[i]) + xs[i])
                {
                    inside = !inside;
                }
            }
        }

        return inside;
    }
}

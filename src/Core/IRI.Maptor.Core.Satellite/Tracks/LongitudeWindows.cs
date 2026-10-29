using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Satellite.Tracks;

/// <summary>
/// Cuts a polygon drawn with continuous ("unwrapped") longitudes into pieces that each lie inside
/// [−180, 180], so a shape crossing the 180° meridian draws as two clean halves.
/// </summary>
public static class LongitudeWindows
{
    /// <summary>
    /// Unwraps a ring's longitudes so consecutive points never differ by more than 180°, then clips
    /// it to each 360°-wide window it touches and shifts every piece back into [−180, 180].
    /// </summary>
    /// <param name="ring">An open ring (first point not repeated), X = longitude, Y = latitude.</param>
    public static List<List<Point>> Split(IReadOnlyList<Point> ring)
    {
        if (ring is null)
            throw new ArgumentNullException(nameof(ring));

        var result = new List<List<Point>>();

        if (ring.Count < 3)
            return result;

        var unwrapped = Unwrap(ring);

        var minX = unwrapped.Min(p => p.X);
        var maxX = unwrapped.Max(p => p.X);

        var firstWindow = (int)Math.Floor((minX + 180.0) / 360.0);
        var lastWindow = (int)Math.Floor((maxX + 180.0) / 360.0);

        for (int k = firstWindow; k <= lastWindow; k++)
        {
            var west = -180.0 + 360.0 * k;
            var east = west + 360.0;

            var piece = Clip(Clip(unwrapped, west, keepEast: true), east, keepEast: false);

            if (piece.Count >= 3)
                result.Add(piece.Select(p => new Point(p.X - 360.0 * k, p.Y)).ToList());
        }

        return result;
    }

    public static List<Point> Unwrap(IReadOnlyList<Point> points)
    {
        var result = new List<Point>(points.Count) { new Point(points[0].X, points[0].Y) };

        for (int i = 1; i < points.Count; i++)
        {
            var previous = result[i - 1].X;

            var x = points[i].X;

            while (x - previous > 180) x -= 360;
            while (x - previous < -180) x += 360;

            result.Add(new Point(x, points[i].Y));
        }

        return result;
    }

    /// <summary>One Sutherland–Hodgman pass against the vertical line X = <paramref name="edge"/>.</summary>
    private static List<Point> Clip(List<Point> polygon, double edge, bool keepEast)
    {
        var output = new List<Point>();

        if (polygon.Count == 0)
            return output;

        bool Inside(Point p) => keepEast ? p.X >= edge : p.X <= edge;

        Point Cross(Point a, Point b)
        {
            var t = (edge - a.X) / (b.X - a.X);

            return new Point(edge, a.Y + t * (b.Y - a.Y));
        }

        for (int i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var previous = polygon[(i + polygon.Count - 1) % polygon.Count];

            if (Inside(current))
            {
                if (!Inside(previous))
                    output.Add(Cross(previous, current));

                output.Add(current);
            }
            else if (Inside(previous))
            {
                output.Add(Cross(previous, current));
            }
        }

        return output;
    }
}

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Topology.FixTypes;

/// <summary>
/// Geometry work the fix types share: walking the parts of a geometry, nearest places in metres, segment
/// intersections and the box a fix changes. Distances are in metres in a <see cref="LocalMetricFrame"/>.
/// </summary>
internal static class FixGeometry
{
    /// <summary>The leaf geometries (lines, rings, points) in depth-first order; a clone has the same order.</summary>
    public static List<Geometry<Point>> Leaves(Geometry<Point> geometry)
    {
        var leaves = new List<Geometry<Point>>();

        void Visit(Geometry<Point> g)
        {
            if (g.Points is not null)
                leaves.Add(g);
            else if (g.Geometries is not null)
                foreach (var child in g.Geometries)
                    Visit(child);
        }

        Visit(geometry);

        return leaves;
    }

    public static Point? FirstPoint(Geometry<Point>? geometry)
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            return null;

        var first = Leaves(geometry).SelectMany(l => l.Points!).FirstOrDefault();

        return first is null ? null : new Point(first.X, first.Y);
    }

    public static double Metres(LocalMetricFrame frame, Point point)
    {
        var (east, north) = frame.ToMetres(point.X, point.Y);

        return Math.Sqrt(east * east + north * north);
    }

    /// <summary>The nearest place on any segment to the frame's origin, in metres.</summary>
    public static (int Leaf, int Index, double East, double North, double Distance)? NearestSegment(List<Geometry<Point>> leaves, LocalMetricFrame frame)
    {
        (int, int, double, double, double)? best = null;
        var bestDistance = double.PositiveInfinity;

        for (int l = 0; l < leaves.Count; l++)
        {
            var points = leaves[l].Points!;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                var (ax, ay) = frame.ToMetres(points[i].X, points[i].Y);
                var (bx, by) = frame.ToMetres(points[i + 1].X, points[i + 1].Y);

                var dx = bx - ax;
                var dy = by - ay;
                var length2 = dx * dx + dy * dy;

                var t = length2 == 0 ? 0 : Math.Max(0, Math.Min(1, (-ax * dx - ay * dy) / length2));

                var qx = ax + t * dx;
                var qy = ay + t * dy;
                var distance = Math.Sqrt(qx * qx + qy * qy);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (l, i, qx, qy, distance);
                }
            }
        }

        return best;
    }

    /// <summary>The nearest vertex to the frame's origin, in metres.</summary>
    public static (int Leaf, int Index, double Distance)? NearestVertex(List<Geometry<Point>> leaves, LocalMetricFrame frame)
    {
        (int, int, double)? best = null;
        var bestDistance = double.PositiveInfinity;

        for (int l = 0; l < leaves.Count; l++)
        {
            var points = leaves[l].Points!;

            for (int i = 0; i < points.Count; i++)
            {
                var distance = Metres(frame, points[i]);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (l, i, distance);
                }
            }
        }

        return best;
    }

    /// <summary>The line end (first or last vertex of a part with two or more points) nearest to the frame's origin.</summary>
    public static (int Leaf, bool IsStart, double Distance)? NearestEnd(List<Geometry<Point>> leaves, LocalMetricFrame frame)
    {
        (int, bool, double)? best = null;
        var bestDistance = double.PositiveInfinity;

        for (int l = 0; l < leaves.Count; l++)
        {
            var points = leaves[l].Points!;

            if (points.Count < 2)
                continue;

            foreach (var isStart in new[] { true, false })
            {
                var distance = Metres(frame, isStart ? points[0] : points[points.Count - 1]);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (l, isStart, distance);
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Where segment p0→p1 meets segment q0→q1, as the fractions along each (0..1); null when they are parallel or
    /// do not meet. Coordinates are metres.
    /// </summary>
    public static (double T, double U)? Intersect((double X, double Y) p0, (double X, double Y) p1, (double X, double Y) q0, (double X, double Y) q1)
    {
        var rx = p1.X - p0.X;
        var ry = p1.Y - p0.Y;
        var sx = q1.X - q0.X;
        var sy = q1.Y - q0.Y;

        var denominator = rx * sy - ry * sx;

        if (Math.Abs(denominator) < 1e-12)
            return null;

        var qpx = q0.X - p0.X;
        var qpy = q0.Y - p0.Y;

        var t = (qpx * sy - qpy * sx) / denominator;
        var u = (qpx * ry - qpy * rx) / denominator;

        const double slack = 1e-9;

        return t >= -slack && t <= 1 + slack && u >= -slack && u <= 1 + slack ? (t, u) : null;
    }

    /// <summary>Where a ray from <paramref name="origin"/> along <paramref name="direction"/> (a unit vector) first meets segment q0→q1: the distance along the ray, or null.</summary>
    public static double? RayHit((double X, double Y) origin, (double X, double Y) direction, (double X, double Y) q0, (double X, double Y) q1)
    {
        var sx = q1.X - q0.X;
        var sy = q1.Y - q0.Y;

        var denominator = direction.X * sy - direction.Y * sx;

        if (Math.Abs(denominator) < 1e-12)
            return null;

        var qpx = q0.X - origin.X;
        var qpy = q0.Y - origin.Y;

        var s = (qpx * sy - qpy * sx) / denominator;
        var u = (qpx * direction.Y - qpy * direction.X) / denominator;

        return s >= 0 && u >= -1e-9 && u <= 1 + 1e-9 ? s : null;
    }

    public static (double X, double Y) ToMetres(LocalMetricFrame frame, Point point) => frame.ToMetres(point.X, point.Y);

    public static Point FromMetres(LocalMetricFrame frame, double east, double north)
    {
        var (x, y) = frame.FromMetres(east, north);

        return new Point(x, y);
    }

    public static BoundingBox Box(IEnumerable<Point> points)
    {
        var list = points.ToList();

        return new BoundingBox(list.Min(p => p.X), list.Min(p => p.Y), list.Max(p => p.X), list.Max(p => p.Y));
    }

    public static BoundingBox Box(IEnumerable<Geometry<Point>> geometries)
        => Box(geometries.SelectMany(g => Leaves(g)).SelectMany(l => l.Points!));
}

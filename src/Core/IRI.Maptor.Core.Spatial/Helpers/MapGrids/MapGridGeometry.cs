using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// The geometry the grid schemes share: finding a view's range inside a projection's plane, and
/// cutting a sampled line down to what is actually on screen.
/// </summary>
/// <remarks>
/// Everything here works in <em>geodetic</em> degrees and converts to Web Mercator only at the
/// very end. That is deliberate: the view, the UTM zone strips and the polar limits are all
/// naturally expressed in degrees, and clipping there means one clipper serves every scheme.
/// </remarks>
internal static class MapGridGeometry
{
    internal static Point ToWebMercator(double longitude, double latitude)
        => MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));

    internal static List<Point> ToWebMercator(List<Point> geodetic)
    {
        var result = new List<Point>(geodetic.Count);

        foreach (var point in geodetic)
        {
            result.Add(ToWebMercator(point.X, point.Y));
        }

        return result;
    }

    /// <summary>
    /// The range a geodetic view occupies in some projection's plane, found by sampling the view's
    /// four edges.
    /// </summary>
    /// <remarks>
    /// Edges rather than corners, and edges rather than the interior, for the same reason
    /// <c>MgrsGridHelper.GetUtmBounds</c> does it: a projection maps the boundary of a region to
    /// the boundary of its image, so the extremes of x and y are always on an edge — but they are
    /// rarely at a corner, because a parallel bows. The result is padded slightly so a line that
    /// only just enters the view is not missed to sampling error; anything genuinely outside is
    /// clipped away later at no cost.
    /// </remarks>
    /// <returns><see cref="BoundingBox.NaN"/> when the projection cannot represent any sampled point.</returns>
    internal static BoundingBox PlaneBounds(Func<Point, Point> forward, BoundingBox geodeticView, int samplesPerEdge)
    {
        samplesPerEdge = Math.Max(2, samplesPerEdge);

        double xMin = double.MaxValue, xMax = double.MinValue, yMin = double.MaxValue, yMax = double.MinValue;

        var any = false;

        for (var i = 0; i <= samplesPerEdge; i++)
        {
            var t = (double)i / samplesPerEdge;

            var longitude = geodeticView.XMin + geodeticView.Width * t;
            var latitude = geodeticView.YMin + geodeticView.Height * t;

            var samples = new[]
            {
                new Point(longitude, geodeticView.YMin),
                new Point(longitude, geodeticView.YMax),
                new Point(geodeticView.XMin, latitude),
                new Point(geodeticView.XMax, latitude),
            };

            foreach (var sample in samples)
            {
                var projected = forward(sample);

                if (projected is null || !IsFinite(projected))
                    continue;

                any = true;

                xMin = Math.Min(xMin, projected.X);
                xMax = Math.Max(xMax, projected.X);
                yMin = Math.Min(yMin, projected.Y);
                yMax = Math.Max(yMax, projected.Y);
            }
        }

        if (!any || xMin > xMax || yMin > yMax)
            return BoundingBox.NaN;

        var padX = Math.Max((xMax - xMin) * 0.02, 1e-9);
        var padY = Math.Max((yMax - yMin) * 0.02, 1e-9);

        return new BoundingBox(xMin - padX, yMin - padY, xMax + padX, yMax + padY);
    }

    /// <summary>
    /// Roughly how much ground the view spans, in metres, along its longer side.
    /// </summary>
    /// <remarks>
    /// A spherical estimate, good to well under a percent — which is far more accuracy than
    /// choosing between ladder steps a factor of two apart needs. It exists because the alternative,
    /// measuring the view inside a transverse Mercator plane, diverges badly once the view is wider
    /// than the zone that plane belongs to. Same estimate <c>MgrsGridHelper</c> uses.
    /// </remarks>
    internal static double GroundSpanInMetres(BoundingBox geodeticView)
    {
        var latitude = Math.Min(Math.Max(geodeticView.Center.Y, -90.0), 90.0);

        var width = geodeticView.Width * 111320.0 * Math.Cos(latitude * Math.PI / 180.0);

        var height = geodeticView.Height * 110574.0;

        return Math.Max(Math.Abs(width), Math.Abs(height));
    }

    internal static bool IsFinite(Point point)
        => !double.IsNaN(point.X) && !double.IsNaN(point.Y)
        && !double.IsInfinity(point.X) && !double.IsInfinity(point.Y);

    /// <summary>
    /// Cuts a sampled geodetic polyline down to the part inside <paramref name="box"/>, returning
    /// one list per surviving run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one place where a grid of lines is genuinely harder than a grid of cells. The
    /// MGRS overlay <em>clamps</em> a cell's vertices to its zone strip, which is fine for a
    /// polygon — the clamped vertices land on the boundary and the ring stays closed. Clamping a
    /// polyline instead draws a spurious segment running along the boundary meridian, so the line
    /// has to be cut and the crossing interpolated.
    /// </para>
    /// <para>
    /// Cutting in geodetic space treats each sampled segment as straight, which it is not; at the
    /// engine's default of 32 samples across a view the error is far below a pixel.
    /// </para>
    /// </remarks>
    internal static List<List<Point>> ClipToBox(List<Point> geodetic, BoundingBox box)
    {
        var parts = new List<List<Point>>();

        if (geodetic is null || geodetic.Count < 2)
            return parts;

        List<Point>? current = null;

        for (var i = 0; i < geodetic.Count - 1; i++)
        {
            var a = geodetic[i];
            var b = geodetic[i + 1];

            // A vertex the projection could not represent breaks the run rather than poisoning it.
            if (!IsFinite(a) || !IsFinite(b))
            {
                current = null;
                continue;
            }

            if (!ClipSegment(a, b, box, out var clippedStart, out var clippedEnd, out var t0, out var t1))
            {
                current = null;
                continue;
            }

            // A run continues only where this segment starts exactly where the last one ended;
            // t0 > 0 means the line re-entered the box here, which is a new run.
            if (current is null || t0 > 0)
            {
                current = new List<Point> { clippedStart };

                parts.Add(current);
            }

            current.Add(clippedEnd);

            if (t1 < 1)
                current = null;
        }

        parts.RemoveAll(part => part.Count < 2);

        return parts;
    }

    /// <summary>Liang-Barsky: the part of segment a-b inside the box, as points and as parameters along a-b.</summary>
    private static bool ClipSegment(Point a, Point b, BoundingBox box, out Point start, out Point end, out double t0, out double t1)
    {
        start = a;
        end = b;
        t0 = 0.0;
        t1 = 1.0;

        var dx = b.X - a.X;
        var dy = b.Y - a.Y;

        var p = new[] { -dx, dx, -dy, dy };
        var q = new[] { a.X - box.XMin, box.XMax - a.X, a.Y - box.YMin, box.YMax - a.Y };

        for (var i = 0; i < 4; i++)
        {
            if (p[i] == 0)
            {
                // Parallel to this edge: either wholly inside it or wholly outside.
                if (q[i] < 0)
                    return false;

                continue;
            }

            var r = q[i] / p[i];

            if (p[i] < 0)
            {
                if (r > t1)
                    return false;

                if (r > t0)
                    t0 = r;
            }
            else
            {
                if (r < t0)
                    return false;

                if (r < t1)
                    t1 = r;
            }
        }

        // An untouched end is handed back as the very same coordinates, not as a + 1·(b - a):
        // that expression is a rounding error away from b, and it would nudge every sampled vertex
        // in the grid off the line it was computed to sit on. Only a genuine cut is interpolated.
        start = t0 <= 0 ? a : new Point(a.X + dx * t0, a.Y + dy * t0);
        end = t1 >= 1 ? b : new Point(a.X + dx * t1, a.Y + dy * t1);

        return true;
    }

    /// <summary>
    /// Where a geodetic polyline first crosses a given latitude, or null if it never does. This is
    /// how a label finds its place on a line that bows.
    /// </summary>
    internal static Point? InterpolateAtLatitude(List<Point> geodetic, double latitude)
        => Interpolate(geodetic, latitude, byLatitude: true);

    /// <summary>Where a geodetic polyline first crosses a given longitude, or null if it never does.</summary>
    internal static Point? InterpolateAtLongitude(List<Point> geodetic, double longitude)
        => Interpolate(geodetic, longitude, byLatitude: false);

    private static Point? Interpolate(List<Point> geodetic, double target, bool byLatitude)
    {
        if (geodetic is null || geodetic.Count == 0)
            return null;

        for (var i = 0; i < geodetic.Count - 1; i++)
        {
            var a = geodetic[i];
            var b = geodetic[i + 1];

            var from = byLatitude ? a.Y : a.X;
            var to = byLatitude ? b.Y : b.X;

            if (double.IsNaN(from) || double.IsNaN(to))
                continue;

            if ((from - target) * (to - target) > 0)
                continue;

            if (from == to)
                return new Point(a.X, a.Y);

            var t = (target - from) / (to - from);

            return new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        return null;
    }
}

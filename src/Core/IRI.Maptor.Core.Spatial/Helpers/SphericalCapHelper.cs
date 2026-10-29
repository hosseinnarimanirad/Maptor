using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Helpers;

/// <summary>
/// Turns a spherical cap (every point within an angular radius of a centre on the sphere) into
/// longitude/latitude polygon rings that draw correctly on a cylindrical map such as Web Mercator.
/// </summary>
/// <remarks>
/// <para>
/// Used for the night side of the Earth (a cap around the anti-solar point) and for satellite
/// footprints. Drawing the cap's boundary circle directly breaks on a flat map in two ways: a cap that
/// covers a pole has a boundary that never closes around the cap, and a cap that straddles the
/// antimeridian wraps from one side of the map to the other. So instead the cap is built a meridian
/// at a time.
/// </para>
/// <para>
/// On each meridian the cap covers a single latitude interval (given the radius is at most 90°): a
/// polygon is the run of meridians that have one, its top edge the interval maxima from west to east
/// and its bottom edge the minima from east to west. Where a run starts or stops between two sampled
/// meridians, the edge meridian is found by bisection, so an oval cap closes on its true extreme
/// longitudes. A cap over a pole simply runs from −180° to 180° and is closed along
/// <c>maxLatitude</c>; a cap across the antimeridian comes back as two rings, one at each side of the map.
/// </para>
/// <para>The Earth is treated as a sphere: positions are good to about 0.2°, which is invisible at the
/// scales a cap is drawn at.</para>
/// </remarks>
public static class SphericalCapHelper
{
    private const double DegreesToRadians = Math.PI / 180.0;

    private const double RadiansToDegrees = 180.0 / Math.PI;

    private const int EdgeBisectionSteps = 40;

    /// <summary>
    /// The cap as longitude/latitude rings (X = longitude, Y = latitude, degrees), open — the first
    /// point is not repeated — with longitudes in [−180, 180].
    /// </summary>
    /// <param name="center">Centre of the cap (X = longitude, Y = latitude, degrees).</param>
    /// <param name="radiusDegrees">Angular radius, in (0, 90].</param>
    /// <param name="maxLatitude">Latitude the rings are clipped to, e.g. 85.05 for Web Mercator.</param>
    /// <param name="stepDegrees">Spacing of the sampled meridians.</param>
    public static List<List<Point>> ToGeographicRings(Point center, double radiusDegrees, double maxLatitude = 90.0, double stepDegrees = 1.0)
    {
        if (center is null)
            throw new ArgumentNullException(nameof(center));

        if (!(center.Y >= -90 && center.Y <= 90) || double.IsNaN(center.X) || double.IsInfinity(center.X))
            throw new ArgumentOutOfRangeException(nameof(center), "The centre must be a valid longitude/latitude in degrees.");

        if (!(radiusDegrees > 0 && radiusDegrees <= 90))
            throw new ArgumentOutOfRangeException(nameof(radiusDegrees), "The radius must be in (0, 90] degrees.");

        if (!(maxLatitude > 0 && maxLatitude <= 90))
            throw new ArgumentOutOfRangeException(nameof(maxLatitude), "The clip latitude must be in (0, 90] degrees.");

        if (!(stepDegrees > 0 && stepDegrees <= 10))
            throw new ArgumentOutOfRangeException(nameof(stepDegrees), "The step must be in (0, 10] degrees.");

        var cap = new Cap(center, radiusDegrees, maxLatitude);

        var columns = (int)Math.Ceiling(360.0 / stepDegrees);

        var step = 360.0 / columns;

        var rings = new List<List<Point>>();

        List<Column>? run = null;

        double previousLongitude = double.NaN;

        for (int i = 0; i <= columns; i++)
        {
            // Computed, not accumulated, so the last column is exactly 180
            var longitude = i == columns ? 180.0 : -180.0 + i * step;

            var interval = cap.GetInterval(longitude);

            if (interval is Column column)
            {
                if (run is null)
                {
                    run = new List<Column>();

                    if (i > 0)
                        run.Add(cap.FindEdge(longitude, previousLongitude));
                }

                run.Add(column);
            }
            else if (run is not null)
            {
                run.Add(cap.FindEdge(previousLongitude, longitude));

                AddRing(rings, run);

                run = null;
            }

            previousLongitude = longitude;
        }

        if (run is not null)
            AddRing(rings, run);

        return rings;
    }

    private static void AddRing(List<List<Point>> rings, List<Column> run)
    {
        var ring = new List<Point>(run.Count * 2);

        foreach (var column in run)
            AddDistinct(ring, column.Longitude, column.North);

        for (int i = run.Count - 1; i >= 0; i--)
            AddDistinct(ring, run[i].Longitude, run[i].South);

        // A run that ends on a tangent meridian brings its last point back onto the first
        if (ring.Count > 1 && AreSame(ring[0], ring[ring.Count - 1]))
            ring.RemoveAt(ring.Count - 1);

        if (ring.Count >= 3)
            rings.Add(ring);
    }

    private static void AddDistinct(List<Point> ring, double longitude, double latitude)
    {
        var point = new Point(longitude, latitude);

        if (ring.Count == 0 || !AreSame(ring[ring.Count - 1], point))
            ring.Add(point);
    }

    private static bool AreSame(Point a, Point b) => Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9;

    private readonly struct Column
    {
        public double Longitude { get; }

        public double South { get; }

        public double North { get; }

        public Column(double longitude, double south, double north)
        {
            Longitude = longitude;
            South = south;
            North = north;
        }
    }

    private sealed class Cap
    {
        private readonly double _centerLongitude;

        private readonly double _sinCenterLatitude;

        private readonly double _cosCenterLatitude;

        private readonly double _cosRadius;

        private readonly double _maxLatitude;

        public Cap(Point center, double radiusDegrees, double maxLatitude)
        {
            _centerLongitude = center.X;
            _sinCenterLatitude = Math.Sin(center.Y * DegreesToRadians);
            _cosCenterLatitude = Math.Cos(center.Y * DegreesToRadians);
            _cosRadius = Math.Cos(radiusDegrees * DegreesToRadians);
            _maxLatitude = maxLatitude;
        }

        /// <summary>
        /// The latitudes the cap covers on one meridian, or null. A point is inside when
        /// sinφ·sinφ₀ + cosφ·cosφ₀·cosΔλ ≥ cos r; writing the left side as R·cos(φ − α) gives the
        /// interval α ± acos(cos r / R) directly.
        /// </summary>
        public Column? GetInterval(double longitude)
        {
            var a = _sinCenterLatitude;

            var b = _cosCenterLatitude * Math.Cos((longitude - _centerLongitude) * DegreesToRadians);

            var r = Math.Sqrt(a * a + b * b);

            double south, north;

            if (r < 1e-15)
            {
                // Centre on the equator, meridian 90° away: every latitude is exactly 90° from it
                if (_cosRadius > 0)
                    return null;

                south = -90;
                north = 90;
            }
            else
            {
                var k = _cosRadius / r;

                if (k > 1)
                    return null;

                // r ≤ 90° makes k ≥ 0, so the arc is at most 180° wide: the part of it beyond ±90° lies on the
                // opposite meridian and a copy shifted by 360° could only touch ±90°, so no wrap is needed
                var halfWidth = Math.Acos(Math.Min(1.0, k)) * RadiansToDegrees;

                var alpha = Math.Atan2(a, b) * RadiansToDegrees;

                south = Math.Max(alpha - halfWidth, -90);
                north = Math.Min(alpha + halfWidth, 90);
            }

            south = Math.Max(south, -_maxLatitude);
            north = Math.Min(north, _maxLatitude);

            return south <= north ? new Column(longitude, south, north) : null;
        }

        /// <summary>The meridian between an inside and an outside one where the cap's coverage ends.</summary>
        public Column FindEdge(double insideLongitude, double outsideLongitude)
        {
            var inside = GetInterval(insideLongitude)!.Value;

            for (int i = 0; i < EdgeBisectionSteps; i++)
            {
                var middle = (insideLongitude + outsideLongitude) / 2;

                if (GetInterval(middle) is Column column)
                {
                    insideLongitude = middle;
                    inside = column;
                }
                else
                {
                    outsideLongitude = middle;
                }
            }

            return inside;
        }
    }
}

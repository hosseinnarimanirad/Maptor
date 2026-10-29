using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Tests.Astronomy;

public class SphericalCapHelperTest
{
    private const double MaxWebMercatorLatitude = 85.05;

    internal static double AngularDistance(Point a, Point b)
    {
        double rad = Math.PI / 180;

        var cos = Math.Sin(a.Y * rad) * Math.Sin(b.Y * rad) + Math.Cos(a.Y * rad) * Math.Cos(b.Y * rad) * Math.Cos((a.X - b.X) * rad);

        return Math.Acos(Math.Max(-1, Math.Min(1, cos))) / rad;
    }

    /// <summary>Even-odd ray casting in the plane of the ring's own coordinates.</summary>
    internal static bool IsInside(IReadOnlyList<Point> ring, double x, double y)
    {
        bool inside = false;

        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            if ((ring[i].Y > y) != (ring[j].Y > y)
                && x < (ring[j].X - ring[i].X) * (y - ring[i].Y) / (ring[j].Y - ring[i].Y) + ring[i].X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    [Fact]
    public void SmallCap_IsOneRingOnTheTrueCircle()
    {
        var center = new Point(50, 35);

        var rings = SphericalCapHelper.ToGeographicRings(center, 10, MaxWebMercatorLatitude);

        var ring = Assert.Single(rings);

        foreach (var point in ring)
            Assert.InRange(AngularDistance(center, point) - 10, -1e-6, 1e-6);

        // Its extreme meridians are the tangent ones: λ₀ ± asin(sin r / cos φ₀)
        var halfWidth = Math.Asin(Math.Sin(10 * Math.PI / 180) / Math.Cos(35 * Math.PI / 180)) * 180 / Math.PI;

        Assert.Equal(50 - halfWidth, ring.Min(p => p.X), 6);
        Assert.Equal(50 + halfWidth, ring.Max(p => p.X), 6);
        Assert.Equal(25, ring.Min(p => p.Y), 6);
        Assert.Equal(45, ring.Max(p => p.Y), 6);

        // Open ring: the first point is not repeated
        Assert.False(ring[0].X == ring[^1].X && ring[0].Y == ring[^1].Y);
    }

    [Fact]
    public void CapAcrossTheAntimeridian_IsSplitIntoOneRingPerSide()
    {
        var rings = SphericalCapHelper.ToGeographicRings(new Point(175, 0), 10, MaxWebMercatorLatitude);

        Assert.Equal(2, rings.Count);

        var west = rings.Single(r => r.Min(p => p.X) == -180);
        var east = rings.Single(r => r.Max(p => p.X) == 180);

        Assert.Equal(-175, west.Max(p => p.X), 6);
        Assert.Equal(165, east.Min(p => p.X), 6);
    }

    [Fact]
    public void CapOverAPole_SpansEveryMeridianAndClosesAtTheClipLatitude()
    {
        var ring = Assert.Single(SphericalCapHelper.ToGeographicRings(new Point(0, 70), 30, MaxWebMercatorLatitude));

        Assert.Equal(-180, ring.Min(p => p.X));
        Assert.Equal(180, ring.Max(p => p.X));
        Assert.Equal(MaxWebMercatorLatitude, ring.Max(p => p.Y), 9);

        // Nearest the centre's meridian it reaches 40°N; on the far side it starts at 80°N
        Assert.Equal(40, ring.Min(p => p.Y), 6);
    }

    [Fact]
    public void CapEntirelyBeyondTheClipLatitude_IsEmpty()
    {
        Assert.Empty(SphericalCapHelper.ToGeographicRings(new Point(10, 89), 2, MaxWebMercatorLatitude));
    }

    [Theory]
    [InlineData(50, 35, 10)]
    [InlineData(175, 0, 10)]
    [InlineData(-30, 70, 30)]
    [InlineData(120, -60, 45)]
    [InlineData(0, 0, 89.1667)]
    [InlineData(-100, 23.44, 72)]
    [InlineData(80, -23.44, 90)]
    public void PointsInsideTheRings_AreExactlyThePointsInsideTheCap(double longitude, double latitude, double radius)
    {
        var center = new Point(longitude, latitude);

        var rings = SphericalCapHelper.ToGeographicRings(center, radius, MaxWebMercatorLatitude);

        var random = new Random(1234);

        int checkedPoints = 0;

        for (int i = 0; i < 4000; i++)
        {
            var x = random.NextDouble() * 360 - 180;
            var y = (random.NextDouble() * 2 - 1) * MaxWebMercatorLatitude;

            var distance = AngularDistance(center, new Point(x, y));

            // Straight edges between sampled meridians cut the true boundary slightly; skip that seam
            if (Math.Abs(distance - radius) < 0.5)
                continue;

            checkedPoints++;

            Assert.True(distance < radius == rings.Any(r => IsInside(r, x, y)),
                FormattableString.Invariant($"({x:F3}, {y:F3}) is {distance:F3}° from the centre"));
        }

        Assert.True(checkedPoints > 3000);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        var center = new Point(0, 0);

        Assert.Throws<ArgumentNullException>(() => SphericalCapHelper.ToGeographicRings(null!, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => SphericalCapHelper.ToGeographicRings(new Point(0, 91), 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => SphericalCapHelper.ToGeographicRings(center, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SphericalCapHelper.ToGeographicRings(center, 91));
        Assert.Throws<ArgumentOutOfRangeException>(() => SphericalCapHelper.ToGeographicRings(center, 10, maxLatitude: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SphericalCapHelper.ToGeographicRings(center, 10, stepDegrees: 0));
    }
}

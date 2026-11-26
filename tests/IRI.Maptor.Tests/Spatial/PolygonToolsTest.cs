using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>The overlap measure and enclosed gaps, on the cases where <c>Geometry.Intersection</c> gives up.</summary>
public class PolygonToolsTest
{
    private static Geometry<Point> Wkt(string wkt) => Geometry<Point>.FromWkt(wkt, 0);

    private const string Square = "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))";

    private static OverlapMeasure<Point> Overlap(string first, string second) => PolygonOverlap.Measure(Wkt(first), Wkt(second), false, 1e-6);

    [Fact]
    public void ProperOverlap_WithSharedEdgeLines_IsMeasured()
    {
        var overlap = Overlap(Square, "POLYGON((5 0, 15 0, 15 10, 5 10, 5 0))");

        Assert.Equal(50, overlap.Area, 6);
        Assert.Equal(30, overlap.Perimeter, 6);
        Assert.Equal(7.5, overlap.Centroid!.X, 6);
        Assert.Equal(5, overlap.Centroid.Y, 6);
        Assert.Equal(100, overlap.FirstArea, 6);
    }

    [Theory]
    [InlineData("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))", 0)]                                    // shared edge
    [InlineData("POLYGON((10 10, 20 10, 20 20, 10 20, 10 10))", 0)]                                 // shared vertex
    [InlineData("POLYGON((9.99 0, 20 0, 20 10, 9.99 10, 9.99 0))", 0.1)]                             // sliver overlap
    [InlineData("POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))", 4)]                                            // inside
    [InlineData("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))", 100)]                                      // duplicate
    [InlineData("POLYGON((0 0, 0 10, 10 10, 10 0, 0 0))", 100)]                                      // duplicate drawn the other way
    [InlineData("POLYGON((50 50, 60 50, 60 60, 50 60, 50 50))", 0)]                                  // apart
    public void OverlapArea_IsRight(string other, double area)
    {
        Assert.Equal(area, Overlap(Square, other).Area, 6);
    }

    [Fact]
    public void PolygonInAHole_DoesNotOverlap()
    {
        var withHole = "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 8, 8 8, 8 2, 2 2))";

        Assert.Equal(0, Overlap(withHole, "POLYGON((3 3, 7 3, 7 7, 3 7, 3 3))").Area, 6);
        // A 4 × 4 corner over the polygon: 16, minus the 2 × 2 corner of the hole.
        Assert.Equal(12, Overlap(withHole, "POLYGON((0 0, 4 0, 4 4, 0 4, 0 0))").Area, 6);
    }

    [Fact]
    public void Overlap_IsTheSameEitherWayRound()
    {
        var a = "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))";
        var b = "POLYGON((3 -5, 8 -5, 8 4, 3 4, 3 -5))";

        Assert.Equal(Overlap(a, b).Area, Overlap(b, a).Area, 9);
        Assert.Equal(20, Overlap(a, b).Area, 6);
    }

    [Fact]
    public void EnclosedGap_BetweenFourPolygons_IsFound()
    {
        var polygons = new Geometry<Point>?[]
        {
            Wkt("POLYGON((0 0, 12 0, 12 10, 10 10, 10 12, 0 12, 0 0))"),
            Wkt("POLYGON((12 0, 22 0, 22 12, 12 12, 12 10, 12 0))"),
            Wkt("POLYGON((12 12, 22 12, 22 22, 10 22, 10 12, 12 12))"),
            Wkt("POLYGON((0 12, 10 12, 10 22, 0 22, 0 12))"),
        };

        var gap = Assert.Single(EnclosedGaps.Find(polygons, false, 0.01));

        Assert.Equal(4, gap.Area, 6);
        // The fourth polygon touches the gap only at a corner point, so three polygons bound it.
        Assert.Equal(new[] { 0, 1, 2 }, gap.PolygonIndices.ToArray());
    }

    [Fact]
    public void NoGap_WhenTheBlockIsFilled_OrTheHoleIsOnePolygons()
    {
        var tiled = new Geometry<Point>?[]
        {
            Wkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))"),
            Wkt("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))"),
        };

        Assert.Empty(EnclosedGaps.Find(tiled, false, 0.01));

        var courtyard = new Geometry<Point>?[]
        {
            Wkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 8, 8 8, 8 2, 2 2))"),
            Wkt("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))"),
        };

        Assert.Empty(EnclosedGaps.Find(courtyard, false, 0.01));
    }
}

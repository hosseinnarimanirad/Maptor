using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Tests.Spatial;

// Visvalingam and Whyatt (1993) against the published output of Visvalingam's own program (Visvalingam 2015, Appendix,
// see PublishedSimplificationData.KochIsland). A vertex survives the area threshold T exactly when |EA| > T, so for any
// T the expected output follows from the table; for 2 <= T < 8 it is the initiator square (Visvalingam 2015, p. 13/16).
// The implementation omits the optional "never below the last eliminated area" rule (Visvalingam 1993, p. 47; called
// implementation specific in Visvalingam 2015, p. 2/16), which only changes the elimination order below T.
public class VisvalingamWhyattTest
{
    private static readonly (double X, double Y, double EffectiveArea)[] KochIsland = PublishedSimplificationData.KochIsland;

    public static TheoryData<double> Thresholds => new() { 0, 0.25, 0.5, 1, 1.5, 2, 3, 7.9, 8 };

    private static SimplificationParameters Area(double t, bool isRing = false, bool retain3Points = false) => new() { AreaThreshold = t, IsRing = isRing, Retain3Points = retain3Points };

    private static List<Point> Expected(IEnumerable<(double X, double Y, double EffectiveArea)> vertices, double threshold)
        => vertices.Where(v => Math.Abs(v.EffectiveArea) > threshold).Select(v => new Point(v.X, v.Y)).ToList();

    // Open line: all 33 vertices, the closing vertex repeated; end points are never eliminated.
    [Theory]
    [MemberData(nameof(Thresholds))]
    public void OpenLine_KeepsExactlyTheVerticesWhosePublishedEffectiveAreaExceedsTheThreshold(double threshold)
    {
        var result = Simplifications.SimplifyByVisvalingamWhyatt(PublishedSimplificationData.KochIslandLine(), Area(threshold));

        Assert.Equal(Expected(KochIsland, threshold), result);
    }

    // Ring: stored without the repeated closing vertex (the convention of Geometry rings, see
    // TopologyUtility.IsPointOnRing), first vertex kept as the fixed start. Every other vertex, the last stored one
    // included, may be eliminated. Before 2026-10-06 an eliminated last vertex was appended again at the end.
    [Theory]
    [MemberData(nameof(Thresholds))]
    public void Ring_KeepsExactlyTheVerticesWhosePublishedEffectiveAreaExceedsTheThreshold(double threshold)
    {
        var ringVertices = KochIsland.Take(KochIsland.Length - 1).ToArray();

        var ring = ringVertices.Select(v => new Point(v.X, v.Y)).ToList();

        var result = Simplifications.SimplifyByVisvalingamWhyatt(ring, Area(threshold, isRing: true));

        Assert.Equal(Expected(ringVertices, threshold), result);
    }

    [Fact]
    public void Ring_AtThresholdTwo_RecoversTheInitiatorSquare()
    {
        var ring = PublishedSimplificationData.KochIslandLine().Take(KochIsland.Length - 1).ToList();

        var result = Simplifications.SimplifyByVisvalingamWhyatt(ring, Area(2, isRing: true));

        Assert.Equal(new List<Point> { new(0, 0), new(0, 4), new(4, 4), new(4, 0) }, result);
    }

    // Above the largest interior effective area (8) everything but the end points goes; Retain3Points stops the
    // elimination at three points instead.
    [Fact]
    public void OpenLine_Retain3Points_StopsAtThreePoints()
    {
        var line = PublishedSimplificationData.KochIslandLine();

        Assert.Equal(2, Simplifications.SimplifyByVisvalingamWhyatt(line, Area(8.5)).Count);

        var retained = Simplifications.SimplifyByVisvalingamWhyatt(line, Area(8.5, retain3Points: true));

        Assert.Equal(3, retained.Count);
        Assert.Equal(line.First(), retained.First());
        Assert.Equal(line.Last(), retained.Last());
    }

    // Geometry.Simplify marks the rings of a polygon itself (on a copy of the parameters)
    [Fact]
    public void Polygon_ThroughGeometrySimplify_IsSimplifiedAsARing()
    {
        var ring = PublishedSimplificationData.KochIslandLine().Take(KochIsland.Length - 1).ToList();

        var polygon = Geometry<Point>.Create(new List<Geometry<Point>> { Geometry<Point>.Create(ring, GeometryType.LineString) }, GeometryType.Polygon, 0);

        var parameters = Area(2);

        var result = polygon.Simplify(SimplificationType.VisvalingamWhyatt, parameters);

        Assert.Equal(new List<Point> { new(0, 0), new(0, 4), new(4, 4), new(4, 0) }, result.Geometries[0].Points);

        Assert.False(parameters.IsRing);
    }
}

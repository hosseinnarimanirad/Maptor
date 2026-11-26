using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

public class PositionClustererTest
{
    [Fact]
    public void TwoClosePoints_FormOneCluster_FarOnesNone()
    {
        var points = new Point?[] { new(0, 0), new(100, 0), new(0.3, 0), new(500, 500) };

        var cluster = Assert.Single(PositionClusterer.Cluster(points, 0.5, isGeodetic: false));

        Assert.Equal(new[] { 0, 2 }, cluster.Members);
        Assert.Equal(0.3, cluster.Diameter, 9);
        Assert.False(cluster.IsChain);
    }

    [Fact]
    public void PointsLinkedThroughEachOther_AreOneCluster_MarkedAsChain()
    {
        // 0 — 0.4 — 0.8 — 1.2: each within 0.5 of the next, the ends 1.2 apart (more than 2 × 0.5).
        var points = new Point?[] { new(0, 0), new(0.4, 0), new(0.8, 0), new(1.2, 0) };

        var cluster = Assert.Single(PositionClusterer.Cluster(points, 0.5, isGeodetic: false));

        Assert.Equal(4, cluster.Members.Count);
        Assert.Equal(1.2, cluster.Diameter, 9);
        Assert.True(cluster.IsChain);
    }

    [Fact]
    public void SeparateGroups_AreSeparateClusters_InOrderOfTheirFirstMember()
    {
        var points = new Point?[] { new(50, 0), new(0, 0), new(50.1, 0), new(0.1, 0) };

        var clusters = PositionClusterer.Cluster(points, 0.5, isGeodetic: false);

        Assert.Equal(new[] { new[] { 0, 2 }, new[] { 1, 3 } }, clusters.Select(c => c.Members.ToArray()).ToArray());
    }

    [Fact]
    public void ZeroDistance_LinksOnlyIdenticalPositions()
    {
        var points = new Point?[] { new(1, 1), new(1, 1), new(1, 1.000001) };

        var cluster = Assert.Single(PositionClusterer.Cluster(points, 0, isGeodetic: false));

        Assert.Equal(new[] { 0, 1 }, cluster.Members);
        Assert.Equal(0, cluster.Diameter);
    }

    [Fact]
    public void NullAndNonFinitePositions_AreSkipped()
    {
        var points = new Point?[] { null, new(double.NaN, 0), new(0, 0), new(0.1, 0) };

        var cluster = Assert.Single(PositionClusterer.Cluster(points, 0.5, isGeodetic: false));

        Assert.Equal(new[] { 2, 3 }, cluster.Members);
    }

    [Fact]
    public void Geodetic_IsMeasuredInMetres()
    {
        // At 35.7° N, 0.000005° of longitude is about 0.45 m and 0.00001° about 0.9 m.
        var points = new Point?[] { new(51.0, 35.7), new(51.000005, 35.7), new(51.1, 35.7), new(51.10001, 35.7) };

        var cluster = Assert.Single(PositionClusterer.Cluster(points, 0.5, isGeodetic: true));

        Assert.Equal(new[] { 0, 1 }, cluster.Members);
        Assert.InRange(cluster.Diameter, 0.44, 0.47);
    }

    [Fact]
    public void InputOrder_DoesNotChangeTheGroups()
    {
        var points = Enumerable.Range(0, 60).Select(i => (Point?)new Point(i % 6 * 10 + i / 6 * 0.01, 0)).ToArray();

        var forward = PositionClusterer.Cluster(points, 0.5, isGeodetic: false)
            .Select(c => string.Join(",", c.Members.Select(m => points[m]!.X))).OrderBy(s => s).ToArray();

        var reversed = points.Reverse().ToArray();

        var backward = PositionClusterer.Cluster(reversed, 0.5, isGeodetic: false)
            .Select(c => string.Join(",", c.Members.Select(m => reversed[m]!.X).OrderBy(x => x))).OrderBy(s => s).ToArray();

        Assert.Equal(6, forward.Length);
        Assert.Equal(forward, backward);
    }
}

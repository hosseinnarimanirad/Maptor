using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Tests.Spatial;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

// The E1 labelling (RdpLabelExperiment.Label) on a polygon ring: RDP with IsRing returns the ring from its first vertex
// without the closing vertex, and LRSimplificationTrainingData in ring mode labels every vertex but the first, the last
// stored one with the first as its next neighbour. Ring: the Koch island of Visvalingam (2015), 32 vertices.
public class RingTrainingDataTest
{
    private static readonly List<LRSimplificationFeatures> Features = new() { LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength };

    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    public void RingModeLabels_OfRingRdp_CoverEveryVertexButTheFirst(double tolerance)
    {
        var closed = PublishedSimplificationData.KochIslandLine();

        var ring = closed.Take(closed.Count - 1).ToList();

        var simplified = Simplifications.SimplifyByRamerDouglasPeucker(ring, new SimplificationParameters { DistanceThreshold = tolerance, IsRing = true });

        var records = LRSimplificationTrainingData<Point>.Create(ring, simplified, true, Features).Records;

        Assert.Equal(ring.Count - 1, records.Count);

        Assert.Equal(simplified.Count - 1, records.Count(r => r.IsRetained));
    }
}

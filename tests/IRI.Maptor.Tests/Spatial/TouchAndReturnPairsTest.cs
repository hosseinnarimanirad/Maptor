using IRI.Maptor.Core.Spatial.Analysis;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>The touch-and-return pairing shared by R23 and the analyses that list crossings.</summary>
public class TouchAndReturnPairsTest
{
    [Fact]
    public void TwoCrossingsClose_AlongBothLines_AreAPair()
    {
        var pair = Assert.Single(TouchAndReturnPairs.Find(new[] { (0, 10.0, 0, 5.0), (0, 10.3, 0, 5.2) }, 0.5));

        Assert.Equal((0, 1), (pair.First, pair.Second));
        Assert.Equal(0.3, pair.Along, 9);
    }

    [Fact]
    public void FarAlongTheOtherLine_IsNotAPair()
        => Assert.Empty(TouchAndReturnPairs.Find(new[] { (0, 10.0, 0, 5.0), (0, 10.3, 0, 8.0) }, 0.5));

    [Fact]
    public void ThreeCrossings_MakeOnePair_AndLeaveOneRealCrossing()
    {
        // In input order 2, 0, 1 along the line: the first two along it pair up, the third is a real crossing.
        var pairs = TouchAndReturnPairs.Find(new[] { (0, 10.4, 0, 5.4), (0, 10.0, 0, 5.0), (0, 10.2, 0, 5.2) }, 0.5);

        var pair = Assert.Single(pairs);
        Assert.Equal((1, 2), (pair.First, pair.Second));
    }

    [Fact]
    public void OnDifferentParts_IsNotAPair()
        => Assert.Empty(TouchAndReturnPairs.Find(new[] { (0, 10.0, 0, 5.0), (1, 0.1, 0, 5.1) }, 0.5));
}

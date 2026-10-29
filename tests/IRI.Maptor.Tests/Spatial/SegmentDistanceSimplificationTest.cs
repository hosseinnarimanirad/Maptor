using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Tests.Spatial;

// The thin-arm case of the LAS paper (Figures 9 and 10): an arm 1 unit wide and 20 long leaves a straight edge. With the
// distance to the infinite chord line (the published RDP/BOPW) the arm's tip lies on the extension of the chord and the
// arm is dropped; with the distance to the chord segment it is kept.
public class SegmentDistanceSimplificationTest
{
    // along the x axis, out along y = 0 to the tip at x = 30, back along y = 1, then on along y = 1 to x = 40
    private static List<Point> ThinArm() => new()
    {
        new Point(0, 0), new Point(10, 0), new Point(20, 0), new Point(30, 0),
        new Point(30, 1), new Point(20, 1), new Point(10, 1), new Point(5, 1),
        new Point(5, 2), new Point(40, 2),
    };

    private static SimplificationParameters Threshold(double t) => new() { DistanceThreshold = t, AreaThreshold = t * t };

    [Fact]
    public void BeforeOpeningWindow_WithLineDistance_DropsTheArm()
    {
        var result = Simplifications.SimplifyByBeforeOpeningWindow(ThinArm(), Threshold(2));

        Assert.DoesNotContain(result, p => p.X == 30);
    }

    [Fact]
    public void BeforeOpeningWindow_WithSegmentDistance_KeepsTheArm()
    {
        var result = Simplifications.SimplifyByBeforeOpeningWindowSegment(ThinArm(), Threshold(2));

        Assert.Contains(result, p => p.X == 30);
    }

    // a thin spike that runs past the end of the chord (0,0)-(10,0) out to x = 30 and back: every vertex is within
    // 0.5 of the chord's line, but the tip is 20 away from the chord segment
    private static List<Point> SpikeBeyondChord() => new()
    {
        new Point(0, 0), new Point(20, 0.5), new Point(30, 0), new Point(20, -0.5), new Point(10, 0),
    };

    [Fact]
    public void RamerDouglasPeucker_WithLineDistance_DropsASpikeBeyondTheChord_SegmentKeepsIt()
    {
        Assert.DoesNotContain(Simplifications.SimplifyByRamerDouglasPeucker(SpikeBeyondChord(), Threshold(1)), p => p.X == 30);

        Assert.Contains(Simplifications.SimplifyByRamerDouglasPeuckerSegment(SpikeBeyondChord(), Threshold(1)), p => p.X == 30);
    }

    [Fact]
    public void RamerDouglasPeuckerSegment_AgreesOnAStraightLine()
    {

        var straight = Enumerable.Range(0, 20).Select(i => new Point(i, 0.1 * (i % 2))).ToList();

        Assert.Equal(Simplifications.SimplifyByRamerDouglasPeucker(straight, Threshold(1)).Count,
                     Simplifications.SimplifyByRamerDouglasPeuckerSegment(straight, Threshold(1)).Count);
    }
}

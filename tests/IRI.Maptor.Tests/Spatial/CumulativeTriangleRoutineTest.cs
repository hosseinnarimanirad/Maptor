using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.Spatial;

// CTR, the Cumulative Triangle Routine of the ISPRS 2025 paper. SimplificationType.CumulativeTriangleRoutine
// dispatches to SimplifyByCumulativeTriangleRoutine, which is the variant that produced every published
// number (verified 2026-09-26 against the 2025-05-25 run: 21 of 21 compression values match; the
// SimplifyByCumulativeTriangleRoutine2 variant, which is what the paper's pseudo-code literally describes,
// matches 4 of 21). Keep these tests as the executable definition of the published algorithm.
public class CumulativeTriangleRoutineTest
{
    // a staircase: every triangle (i, i+1, i+2) has area 0.5
    private static List<Point> Staircase(int steps)
    {
        var points = new List<Point> { new(0, 0) };

        for (int i = 0; i < steps; i++)
        {
            points.Add(new Point(i + 1, i));
            points.Add(new Point(i + 1, i + 1));
        }

        return points;
    }

    [Fact]
    public void PublishedVariant_AccumulatesAreasUntilTheThresholdIsExceeded()
    {
        var points = Staircase(6); // 13 points, 11 triangles of area 0.5 each

        // threshold 1.2: the third consecutive triangle pushes the sum to 1.5 > 1.2, so every third
        // middle point is retained; after a retain the window starts at that middle point
        var simplified = Simplifications.SimplifyByCumulativeTriangleRoutine(points, new SimplificationParameters { AreaThreshold = 1.2 });

        Assert.Equal(points.First(), simplified.First());
        Assert.Equal(points.Last(), simplified.Last());
        Assert.Equal(new List<Point> { new(0, 0), new(2, 1), new(3, 3), new(5, 4), new(6, 6) }, simplified);
    }

    [Fact]
    public void PublishedVariant_KeepsEveryMiddlePointWhenOneTriangleExceedsTheThreshold()
    {
        var points = Staircase(3);

        var simplified = Simplifications.SimplifyByCumulativeTriangleRoutine(points, new SimplificationParameters { AreaThreshold = 0.4 });

        Assert.Equal(points.Take(points.Count - 1).Append(points.Last()).ToList(), simplified);
    }

    [Fact]
    public void PublishedVariant_RemovesEverythingBelowTheThreshold_AndKeepsTheEnds()
    {
        var points = Staircase(4);

        var simplified = Simplifications.SimplifyByCumulativeTriangleRoutine(points, new SimplificationParameters { AreaThreshold = 100 });

        Assert.Equal(new List<Point> { points.First(), points.Last() }, simplified);

        var withMiddle = Simplifications.SimplifyByCumulativeTriangleRoutine(points, new SimplificationParameters { AreaThreshold = 100, Retain3Points = true });

        Assert.Equal(3, withMiddle.Count);
    }

    [Fact]
    public void PublishedVariant_IsWhatTheEnumDispatchesTo()
    {
        var line = Geometry<Point>.Create(Staircase(6), GeometryType.LineString);

        var parameters = new SimplificationParameters { AreaThreshold = 1.2 };

        var viaEnum = line.Simplify(SimplificationType.CumulativeTriangleRoutine, parameters);

        Assert.Equal(Simplifications.SimplifyByCumulativeTriangleRoutine(line.Points!, parameters), viaEnum.Points);
    }

    [Fact]
    public void PseudoCodeVariant_RetainsTheLastPointToo_AndCompressesLess()
    {
        var points = Staircase(6);

        var parameters = new SimplificationParameters { AreaThreshold = 1.2 };

        var published = Simplifications.SimplifyByCumulativeTriangleRoutine(points, parameters);
        var pseudoCode = Simplifications.SimplifyByCumulativeTriangleRoutine2(points, parameters);

        Assert.Equal(new List<Point> { new(0, 0), new(2, 1), new(2, 2), new(4, 3), new(4, 4), new(6, 5), new(6, 6) }, pseudoCode);
        Assert.True(pseudoCode.Count > published.Count);
    }

    [Fact]
    public void FewerThanThreePoints_AreReturnedUnchanged()
    {
        var two = new List<Point> { new(0, 0), new(1, 1) };

        Assert.Same(two, Simplifications.SimplifyByCumulativeTriangleRoutine(two, new SimplificationParameters { AreaThreshold = 1 }));
        Assert.Empty(Simplifications.SimplifyByCumulativeTriangleRoutine(new List<Point>(), new SimplificationParameters { AreaThreshold = 1 }));
    }
}

using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Tests.Spatial;

// The angle routines (Tobler 1966: drop a point "whenever the direction of the line is not changed through a threshold
// angle", Douglas and Peucker 1973, p. 113), the cumulative Euclidean distance and the modified perpendicular distance
// routines, on published data (PublishedSimplificationData). AngleThreshold is cos² of the largest turn that may be
// dropped.
public class AngleAndCumulativeSimplificationTest
{
    private static double SquareCosine(double degrees) => Math.Pow(Math.Cos(degrees * Math.PI / 180), 2);

    // turning angle at p[i] in degrees, 0 for a straight run, 180 for a full reversal
    private static double TurnDegrees(List<Point> p, int i)
    {
        var a = Math.Atan2(p[i].Y - p[i - 1].Y, p[i].X - p[i - 1].X);

        var b = Math.Atan2(p[i + 1].Y - p[i].Y, p[i + 1].X - p[i].X);

        var turn = Math.Abs(b - a) * 180 / Math.PI;

        return turn > 180 ? 360 - turn : turn;
    }

    private static double Length(Point a, Point b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    // Ebisch (2002) Fig. 3 turns back by about 177 degrees at its middle vertex. cos² of that turn is 0.997, as large as
    // for a straight run, so until 2026-10-06 (cos² without its sign) the hairpin was dropped by every angle routine.
    [Fact]
    public void AngleRoutines_KeepTheHairpinOfEbischFigure3()
    {
        var line = PublishedSimplificationData.EbischFigure3();

        Assert.True(TurnDegrees(line, 1) > 170);

        var parameters = new SimplificationParameters { AngleThreshold = SquareCosine(10), AreaThreshold = double.MaxValue };

        Assert.Equal(line, Simplifications.SimplifyByAngle(line, parameters));
        Assert.Equal(line, Simplifications.SimplifyByCumulativeAngle(line, parameters));
        Assert.Equal(line, Simplifications.SimplifyByCumulativeAngleArea(line, parameters));
    }

    // SimplifyByAngle tests every interior point against its two original neighbours: the turn must exceed the
    // threshold angle (the simplify-js coastline has turns between 0 and 99 degrees)
    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(89)]
    public void Angle_Coast_KeepsExactlyThePointsTurningMoreThanTheThreshold(double degrees)
    {
        var input = PublishedSimplificationData.SimplifyJsPoints();

        var expected = Enumerable.Range(0, input.Count)
                                 .Where(i => i == 0 || i == input.Count - 1 || TurnDegrees(input, i) > degrees)
                                 .Select(i => input[i]).ToList();

        Assert.Equal(expected, Simplifications.SimplifyByAngle(input, new SimplificationParameters { AngleThreshold = SquareCosine(degrees) }));
    }

    // a point is kept once the length of the path walked since the last kept point exceeds the threshold (until
    // 2026-10-06 squared distances to the last kept point were summed)
    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(200)]
    public void CumulativeEuclideanDistance_Coast_KeepsAPointOnceThePathSinceTheLastKeptOneExceedsTheThreshold(double threshold)
    {
        var input = PublishedSimplificationData.SimplifyJsPoints();

        var expected = new List<Point> { input[0] };

        double path = 0;

        for (int i = 1; i < input.Count - 1; i++)
        {
            path += Length(input[i - 1], input[i]);

            if (path > threshold)
            {
                expected.Add(input[i]);
                path = 0;
            }
        }

        expected.Add(input.Last());

        Assert.Equal(expected, Simplifications.SimplifyByCumulativeEuclideanDistance(input, new SimplificationParameters { DistanceThreshold = threshold }));
    }

    // The modified perpendicular distance routine (no published source; the repository's own variant): for each end
    // point e, the middle point m = ceil((s + e) / 2) of the span from the last kept point s is kept when it is farther
    // than the threshold from the line s-e. Until 2026-10-06 the distance of e to the line s-m was measured instead.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ModifiedPerpendicularDistance_Coast_TestsTheMiddlePointOfTheSpan(double threshold)
    {
        var input = PublishedSimplificationData.SimplifyJsPoints();

        var expected = new List<Point> { input[0] };

        for (int s = 0, e = 2; e < input.Count; e++)
        {
            var m = (int)Math.Ceiling((s + e) / 2.0);

            double dx = input[e].X - input[s].X, dy = input[e].Y - input[s].Y;

            var distance = Math.Abs(dx * (input[m].Y - input[s].Y) - dy * (input[m].X - input[s].X)) / Math.Sqrt(dx * dx + dy * dy);

            if (distance > threshold)
            {
                expected.Add(input[m]);
                s = m;
            }
        }

        expected.Add(input.Last());

        Assert.Equal(expected, Simplifications.SimplifyByModifiedPerpendicularDistance(input, new SimplificationParameters { DistanceThreshold = threshold }));
    }
}

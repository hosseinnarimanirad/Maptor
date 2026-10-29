using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Tests.Spatial;

// The distance-based algorithms of Simplifications.cs checked on published data (PublishedSimplificationData) against
// (a) expected outputs published with a paper or a library, (b) ports of the psimpl reference implementation
// (psimpl.h, Elmar de Koning, MPL 1.1, http://psimpl.sourceforge.net) and (c) the properties the source papers define.
//
// Distance: every algorithm here measures the perpendicular distance to the infinite line through the chord, as Ramer
// (1972, p. 250) and Douglas and Peucker (1973, pp. 116-117) do; psimpl uses the segment distance for Lang and the
// perpendicular-distance routine, so the ports below use the line distance (author's decision of 2026-10-06: Lang 1969
// is not available, keep the line). Ties: the code keeps a point when its distance exceeds the tolerance (d > tol);
// psimpl's RW and radial routines treat d >= tol as outside. The two differ only when a distance equals the tolerance
// exactly, which the real-valued simplify-js coastline never produces at the tolerances used.
public class DistanceSimplificationTest
{
    public static TheoryData<double> CoastTolerances => new() { 1, 2, 5, 10, 20 };

    private static SimplificationParameters Distance(double t) => new() { DistanceThreshold = t, AreaThreshold = t * t };

    private static List<Point> Coast() => PublishedSimplificationData.SimplifyJsPoints();

    #region Independent geometry (not the helpers under test)

    private static double LineDistance(Point a, Point b, Point p)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;

        var length = Math.Sqrt(dx * dx + dy * dy);

        return length == 0 ? Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y)) : Math.Abs(dx * (p.Y - a.Y) - dy * (p.X - a.X)) / length;
    }

    private static double Length(Point a, Point b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    // indices of the output points in the input; fails unless the output is an ordered subsequence with both end points
    private static List<int> KeptIndices(List<Point> input, List<Point> output)
    {
        var indices = new List<int>();

        int next = 0;

        foreach (var p in output)
        {
            while (next < input.Count && !input[next].Equals(p))
                next++;

            Assert.True(next < input.Count, $"output point {p} is not in order in the input");

            indices.Add(next++);
        }

        Assert.Equal(0, indices.First());
        Assert.Equal(input.Count - 1, indices.Last());

        return indices;
    }

    // the error bound RDP, NOPW, BOPW and Lang guarantee: every dropped point lies within the tolerance of the line
    // through the two kept points around it
    private static void AssertEveryDroppedPointWithinTolerance(List<Point> input, List<int> kept, double tolerance)
    {
        for (int k = 0; k < kept.Count - 1; k++)
            for (int i = kept[k] + 1; i < kept[k + 1]; i++)
                Assert.True(LineDistance(input[kept[k]], input[kept[k + 1]], input[i]) <= tolerance,
                    $"point {i} is {LineDistance(input[kept[k]], input[kept[k + 1]], input[i])} from chord {kept[k]}-{kept[k + 1]}");
    }

    // first point of the window (start, end) farther than the tolerance from the chord, or -1
    private static int FirstViolation(List<Point> p, int start, int end, double tolerance)
    {
        for (int i = start + 1; i < end; i++)
            if (LineDistance(p[start], p[end], p[i]) > tolerance)
                return i;

        return -1;
    }

    #endregion

    #region Ramer-Douglas-Peucker

    // JTS TestSimplify.xml (modules/tests/src/test/resources/testxml/general, EPL-2.0/EDL-1.0), simplifyDP cases on
    // lines. Every projection falls inside its chord, so the line and the segment distance agree on them.
    public static TheoryData<double[], double, double[]> JtsLineCases => new()
    {
        { new double[] { 10, 10, 20, 21, 30, 30 }, 10, new double[] { 10, 10, 30, 30 } },
        { new double[] { 0, 5, 1, 5, 2, 5, 5, 5 }, 10, new double[] { 0, 5, 5, 5 } },
        { new double[] { 10, 60, 39, 50, 70, 60, 90, 50 }, 10, new double[] { 10, 60, 90, 50 } },
        { new double[] { 35, 55, 46, 55 }, 10, new double[] { 35, 55, 46, 55 } },
        { new double[] { 10, 40, 40, 30, 70, 40, 90, 30 }, 10, new double[] { 10, 40, 90, 30 } },
        { new double[] { 10, 10, 10, 30, 30, 30 }, 10, new double[] { 10, 10, 10, 30, 30, 30 } },
    };

    // fhirschmann/rdp tests.py (MIT), the reference for the infinite-line distance (its pldist is |cross| / |chord|);
    // rdp's default epsilon is 0
    public static TheoryData<double[], double, double[]> RdpPackageCases => new()
    {
        { new double[] { 0, 0, 4, 4 }, 0, new double[] { 0, 0, 4, 4 } },                               // test_two
        { new double[] { 0, 0, 1, 0, 2, 0, 3, 0, 4, 0 }, 0, new double[] { 0, 0, 4, 0 } },               // test_hor
        { new double[] { 0, 0, 0, 1, 0, 2, 0, 3, 0, 4 }, 0, new double[] { 0, 0, 0, 4 } },               // test_ver
        { new double[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 }, 0, new double[] { 0, 0, 4, 4 } },               // test_diag
        { new double[] { 0, 0, 5, 1, 10, 1 }, 0, new double[] { 0, 0, 5, 1, 10, 1 } },                   // test_eps0
        { new double[] { 0, 0, 5, 1, 10, 1 }, 1, new double[] { 0, 0, 10, 1 } },                         // test_eps1
        { new double[] { 5, 0, 4, 0, 3, 0, 3, 1, 3, 2 }, 0, new double[] { 5, 0, 3, 0, 3, 2 } },         // test_L0
        { new double[] { 0, 0, 2, 2, 4, 4 }, 0, new double[] { 0, 0, 4, 4 } },                           // test_nn
    };

    private static List<Point> ToPoints(double[] xy) => Enumerable.Range(0, xy.Length / 2).Select(i => new Point(xy[2 * i], xy[2 * i + 1])).ToList();

    [Theory]
    [MemberData(nameof(JtsLineCases))]
    public void RamerDouglasPeucker_JtsLineCases(double[] input, double tolerance, double[] expected)
    {
        Assert.Equal(ToPoints(expected), Simplifications.SimplifyByRamerDouglasPeucker(ToPoints(input), Distance(tolerance)));

        Assert.Equal(ToPoints(expected), Simplifications.SimplifyByRamerDouglasPeuckerSegment(ToPoints(input), Distance(tolerance)));
    }

    [Theory]
    [MemberData(nameof(RdpPackageCases))]
    public void RamerDouglasPeucker_RdpPackageCases(double[] input, double tolerance, double[] expected)
    {
        Assert.Equal(ToPoints(expected), Simplifications.SimplifyByRamerDouglasPeucker(ToPoints(input), Distance(tolerance)));
    }

    // simplify-js default = radial distance pass + Douglas-Peucker with the segment distance; the same two steps here
    [Fact]
    public void RadialDistanceThenSegmentRamerDouglasPeucker_ReproducesSimplifyJsFixture()
    {
        var radial = Simplifications.SimplifyByEuclideanDistance(Coast(), Distance(5));

        var result = Simplifications.SimplifyByRamerDouglasPeuckerSegment(radial, Distance(5));

        Assert.Equal(PublishedSimplificationData.SimplifyJsExpectedAtTolerance5(), result);
    }

    // Ebisch (2002) Fig. 3: with the line distance (Ramer, Douglas-Peucker) the middle vertex is dropped, with the
    // segment distance (Ebisch's reading) it is kept
    [Fact]
    public void RamerDouglasPeucker_EbischFigure3_LineDropsSegmentKeepsTheMiddleVertex()
    {
        var line = PublishedSimplificationData.EbischFigure3();

        var tolerance = Distance(PublishedSimplificationData.EbischFigure3Tolerance);

        Assert.Equal(new List<Point> { line[0], line[2] }, Simplifications.SimplifyByRamerDouglasPeucker(line, tolerance));

        Assert.Equal(line, Simplifications.SimplifyByRamerDouglasPeuckerSegment(line, tolerance));
    }

    // Douglas and Peucker (1973, p. 117): split at the point farthest from the chord while that distance exceeds the
    // tolerance; "in the case of closed loops, where the first and the last point do not define a line", the distance
    // from that point is used instead (LineDistance returns it for a zero-length chord)
    private static List<int> DouglasPeuckerDefinition(List<Point> input, double tolerance)
    {
        var expected = new SortedSet<int> { 0, input.Count - 1 };

        void Split(int first, int last)
        {
            int farthest = -1;
            double max = 0;

            for (int i = first + 1; i < last; i++)
            {
                var d = LineDistance(input[first], input[last], input[i]);

                if (d > max) { max = d; farthest = i; }
            }

            if (max > tolerance)
            {
                expected.Add(farthest);
                Split(first, farthest);
                Split(farthest, last);
            }
        }

        Split(0, input.Count - 1);

        return expected.ToList();
    }

    [Theory]
    [MemberData(nameof(CoastTolerances))]
    public void RamerDouglasPeucker_Coast_MatchesTheRecursiveDefinition(double tolerance)
    {
        var input = Coast();

        var kept = KeptIndices(input, Simplifications.SimplifyByRamerDouglasPeucker(input, Distance(tolerance)));

        Assert.Equal(DouglasPeuckerDefinition(input, tolerance), kept);

        AssertEveryDroppedPointWithinTolerance(input, kept, tolerance);
    }

    // The Koch island of Visvalingam (2015) as a closed line (first point = last point): the first split is at the
    // vertex farthest from (0, 0), i.e. (3, 5) at index 14; until 2026-10-06 the code split at the middle index 16
    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    public void RamerDouglasPeucker_ClosedKochIsland_SplitsAtThePointFarthestFromTheStart(double tolerance)
    {
        var input = PublishedSimplificationData.KochIslandLine();

        var expected = DouglasPeuckerDefinition(input, tolerance);

        Assert.Equal(expected, KeptIndices(input, Simplifications.SimplifyByRamerDouglasPeucker(input, Distance(tolerance))));

        Assert.Equal(expected, KeptIndices(input, Simplifications.SimplifyByRamerDouglasPeuckerSegment(input, Distance(tolerance))));
    }

    // The same island as a ring stored without its closing vertex (IsRing, the convention of polygon rings): the result
    // is that of the closed line without the closing vertex, for both variants, directly and through Geometry.Simplify
    // on a polygon (which sets IsRing on its own copy of the parameters)
    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RamerDouglasPeucker_KochIslandRing_IsDividedLikeTheClosedLine(double tolerance)
    {
        var closed = PublishedSimplificationData.KochIslandLine();

        var ring = closed.Take(closed.Count - 1).ToList();

        var expected = DouglasPeuckerDefinition(closed, tolerance).SkipLast(1).Select(i => closed[i]).ToList();

        var ringParameters = Distance(tolerance);

        ringParameters.IsRing = true;

        Assert.Equal(expected, Simplifications.SimplifyByRamerDouglasPeucker(ring, ringParameters));

        Assert.Equal(expected, Simplifications.SimplifyByRamerDouglasPeuckerSegment(ring, ringParameters));

        var polygon = Geometry<Point>.Create(new List<Geometry<Point>> { Geometry<Point>.Create(ring, GeometryType.LineString) }, GeometryType.Polygon, 0);

        var callerParameters = Distance(tolerance);

        Assert.Equal(expected, polygon.Simplify(SimplificationType.RamerDouglasPeucker, callerParameters).Geometries[0].Points);

        Assert.False(callerParameters.IsRing);
    }

    // without IsRing the ring is an open line from its first to its last stored vertex, both of which are kept
    [Fact]
    public void RamerDouglasPeucker_KochIslandRing_WithoutIsRing_IsAnOpenLine()
    {
        var closed = PublishedSimplificationData.KochIslandLine();

        var ring = closed.Take(closed.Count - 1).ToList();

        var result = Simplifications.SimplifyByRamerDouglasPeucker(ring, Distance(2));

        Assert.Equal(DouglasPeuckerDefinition(ring, 2).Select(i => ring[i]).ToList(), result);

        Assert.Equal(ring.Last(), result.Last());
    }

    // a closed line all of whose points lie within the tolerance of its start collapses to its two (equal) end points
    [Fact]
    public void RamerDouglasPeucker_ClosedKochIsland_AboveTheLargestDistanceFromTheStart_KeepsOnlyTheEnds()
    {
        var input = PublishedSimplificationData.KochIslandLine();

        Assert.Equal(new List<Point> { input.First(), input.Last() }, Simplifications.SimplifyByRamerDouglasPeucker(input, Distance(6)));

        Assert.Contains(input[14], Simplifications.SimplifyByRamerDouglasPeucker(input, Distance(5.8)));
    }

    #endregion

    #region Opening window (Meratnia and de By 2004)

    // No reference implementation of NOPW/BOPW exists; the tests check what the paper defines (p. 769, SPT pseudo-code
    // p. 772-773): the window grows from the anchor while every point between anchor and float is within the
    // tolerance; at the first float that leaves a point outside, NOPW cuts at the first such point (the "data point
    // causing the threshold violation") and BOPW at the point just before the float; the cut point is the next anchor.

    [Theory]
    [MemberData(nameof(CoastTolerances))]
    public void NormalOpeningWindow_Coast_CutsAtTheFirstViolatingPoint(double tolerance)
    {
        var input = Coast();

        var kept = KeptIndices(input, Simplifications.SimplifyByNormalOpeningWindow(input, Distance(tolerance)));

        AssertEveryDroppedPointWithinTolerance(input, kept, tolerance);

        for (int k = 0; k < kept.Count - 2; k++)
        {
            int anchor = kept[k], end = anchor + 2;

            while (FirstViolation(input, anchor, end, tolerance) < 0)
                end++;

            Assert.Equal(FirstViolation(input, anchor, end, tolerance), kept[k + 1]);
        }
    }

    [Theory]
    [MemberData(nameof(CoastTolerances))]
    public void BeforeOpeningWindow_Coast_CutsJustBeforeTheFirstViolatingFloat(double tolerance)
    {
        var input = Coast();

        var kept = KeptIndices(input, Simplifications.SimplifyByBeforeOpeningWindow(input, Distance(tolerance)));

        AssertEveryDroppedPointWithinTolerance(input, kept, tolerance);

        for (int k = 0; k < kept.Count - 2; k++)
        {
            int anchor = kept[k], end = anchor + 2;

            while (FirstViolation(input, anchor, end, tolerance) < 0)
                end++;

            Assert.Equal(end - 1, kept[k + 1]);
        }
    }

    #endregion

    #region psimpl ports: Reumann-Witkam, Lang, perpendicular distance, radial distance

    // psimpl ReumannWitkam: strip along the line L(key, key + 1); the first point outside makes the point before it the
    // new key and the strip turns to L(new key, that point). Distance to the infinite line, as in psimpl.
    private static List<int> PsimplReumannWitkam(List<Point> p, double tol)
    {
        var keys = new List<int> { 0 };

        int p0 = 0, p1 = 1;

        for (int j = 2; j < p.Count; j++)
        {
            if (LineDistance(p[p0], p[p1], p[j]) <= tol)
                continue;

            keys.Add(j - 1);
            p0 = j - 1;
            p1 = j;
        }

        keys.Add(p.Count - 1);

        return keys;
    }

    // psimpl Lang: window of lookAhead points; shrink it by one until every point in between is within the tolerance
    // (psimpl measures to the segment; here the line, see the class comment)
    private static List<int> PsimplLang(List<Point> p, double tol, int lookAhead)
    {
        var keys = new List<int> { 0 };

        int current = 0, next = Math.Min(p.Count - 1, lookAhead);

        while (current < p.Count - 1)
        {
            if (Enumerable.Range(current + 1, next - current - 1).All(i => LineDistance(p[current], p[next], p[i]) < tol))
            {
                keys.Add(next);
                current = next;
                next = Math.Min(p.Count - 1, current + lookAhead);
            }
            else
            {
                next--;
            }
        }

        return keys;
    }

    // psimpl PerpendicularDistance (one pass): test v[i+1] against the line v[i]-v[i+2]; a removed vertex makes v[i+2]
    // the next key, so at most every other vertex is removed (psimpl measures to the segment; here the line)
    private static List<int> PsimplPerpendicularDistance(List<Point> p, double tol)
    {
        var keys = new List<int> { 0 };

        int i = 0;

        while (i + 2 < p.Count)
        {
            if (LineDistance(p[i], p[i + 2], p[i + 1]) > tol)
            {
                keys.Add(i + 1);
                i += 1;
            }
            else
            {
                keys.Add(i + 2);
                i += 2;
            }
        }

        if (keys.Last() != p.Count - 1)
            keys.Add(p.Count - 1);

        return keys;
    }

    // psimpl RadialDistance: a vertex closer than the tolerance to the current key is removed
    private static List<int> PsimplRadialDistance(List<Point> p, double tol)
    {
        var keys = new List<int> { 0 };

        for (int i = 1; i < p.Count - 1; i++)
            if (Length(p[keys.Last()], p[i]) > tol)
                keys.Add(i);

        keys.Add(p.Count - 1);

        return keys;
    }

    [Theory]
    [MemberData(nameof(CoastTolerances))]
    public void ReumannWitkam_Coast_MatchesPsimpl(double tolerance)
    {
        var input = Coast();

        Assert.Equal(PsimplReumannWitkam(input, tolerance), KeptIndices(input, Simplifications.SimplifyByReumannWitkam(input, Distance(tolerance))));
    }

    [Theory]
    [InlineData(1, 8)]
    [InlineData(2, 8)]
    [InlineData(5, 8)]
    [InlineData(10, 8)]
    [InlineData(5, 3)]
    [InlineData(5, 20)]
    public void Lang_Coast_MatchesPsimpl(double tolerance, int lookAhead)
    {
        var input = Coast();

        var parameters = new SimplificationParameters { DistanceThreshold = tolerance, LookAhead = lookAhead };

        var kept = KeptIndices(input, Simplifications.SimplifyByLang(input, parameters));

        Assert.Equal(PsimplLang(input, tolerance, lookAhead), kept);

        AssertEveryDroppedPointWithinTolerance(input, kept, tolerance);
    }

    // the default look-ahead (max(3, n / 10)) is computed per call and not written back into the parameters, which
    // Geometry.Simplify shares between all rings and parts of a geometry (fixed 2026-10-06)
    [Fact]
    public void Lang_DefaultLookAhead_DoesNotChangeTheSharedParameters()
    {
        var parameters = new SimplificationParameters { DistanceThreshold = 0.5 };

        var koch = PublishedSimplificationData.KochIslandLine();

        var kochAlone = Simplifications.SimplifyByLang(koch, new SimplificationParameters { DistanceThreshold = 0.5 });

        Simplifications.SimplifyByLang(Coast(), parameters);

        Assert.Null(parameters.LookAhead);

        Assert.Equal(kochAlone, Simplifications.SimplifyByLang(koch, parameters));
    }

    [Theory]
    [MemberData(nameof(CoastTolerances))]
    public void PerpendicularDistance_Coast_MatchesPsimpl(double tolerance)
    {
        var input = Coast();

        Assert.Equal(PsimplPerpendicularDistance(input, tolerance), KeptIndices(input, Simplifications.SimplifyByPerpendicularDistance(input, Distance(tolerance))));
    }

    [Theory]
    [MemberData(nameof(CoastTolerances))]
    public void RadialDistance_Coast_MatchesPsimpl(double tolerance)
    {
        var input = Coast();

        Assert.Equal(PsimplRadialDistance(input, tolerance), KeptIndices(input, Simplifications.SimplifyByEuclideanDistance(input, Distance(tolerance))));
    }

    #endregion

    #region n-th point and random point (Tobler 1966, as described by McMaster 1987 and Shi and Cheung 2006)

    // "every nth coordinate pair (2nd, 4th, 6th ...) is retained" (McMaster 1987), plus both end points
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(99)]
    [InlineData(150)]
    public void NthPoint_Coast_KeepsEveryNthPointAndTheEnds(int n)
    {
        var input = Coast();

        var expected = Enumerable.Range(0, input.Count).Where(i => i == 0 || i % n == 0 || i == input.Count - 1).ToList();

        Assert.Equal(expected, KeptIndices(input, Simplifications.SimplifyByNthPoint(input, new SimplificationParameters { N = (uint)n })));
    }

    // "for every fixed number of consecutive points along the line, ... one random point among a set of these
    // consecutive points [is] retained" (Shi and Cheung 2006, p. 28): sets 1..N, N+1..2N, ... of the interior points
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(98)]
    [InlineData(150)]
    public void RandomPointSelection_Coast_KeepsOneRandomPointPerSetOfNConsecutivePoints(int n)
    {
        var input = Coast();

        for (int repeat = 0; repeat < 20; repeat++)
        {
            var kept = KeptIndices(input, Simplifications.SimplifyByRandomPointSelection(input, new SimplificationParameters { N = (uint)n }));

            var interior = kept.Skip(1).Take(kept.Count - 2).ToList();

            Assert.Equal(Enumerable.Range(0, (input.Count - 2 + n - 1) / n).ToList(), interior.Select(i => (i - 1) / n).ToList());
        }
    }

    [Fact]
    public void NthPointAndRandomPoint_WithoutN_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Simplifications.SimplifyByNthPoint(Coast(), new SimplificationParameters()));

        Assert.Throws<ArgumentOutOfRangeException>(() => Simplifications.SimplifyByRandomPointSelection(Coast(), new SimplificationParameters()));
    }

    #endregion
}

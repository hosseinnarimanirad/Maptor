using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;

using Xunit;

namespace IRI.Maptor.Tests.Topology;

/// <summary>R20–R23, R40 and R41 on small synthetic layers (projected, metres).</summary>
public class LineAndPolygonRuleTypesTest
{
    private static Feature<Point> F(int id, string wkt) => new(Geometry<Point>.FromWkt(wkt, 0)) { Id = id };

    private static Feature<Point> P(int id, double x, double y)
        => F(id, string.Format(CultureInfo.InvariantCulture, "POINT({0} {1})", x, y));

    private static RuleParameters Parameters(params (string Name, object Value)[] values)
        => new(values.ToDictionary(v => v.Name, v => v.Value));

    private static async Task<IReadOnlyList<TopologyIssue>> CheckAsync(TopologyRule rule, params (string Layer, GeometryType Type, Feature<Point>[] Features)[] layers)
    {
        var ruleSet = new TopologyRuleSet("t", 1, layers.Select(l => new TopologyLayer(l.Layer, l.Type)), new[] { rule });

        var result = await new ValidationRunner(StandardRuleTypes.CreateCatalog())
            .RunAsync(ruleSet, TopologyCheckScope.WholeData, layers.Select(l => new FeatureListSource(l.Layer, 0, l.Features)));

        return result.Issues;
    }

    private static int Count(IReadOnlyList<TopologyIssue> issues, string kind) => issues.Count(i => i.Kind == kind);

    #region R20 line ends must connect

    private static Feature<Point>[] Routes() => new[]
    {
        F(1, "LINESTRING(0 0, 100 0)"),
        F(2, "LINESTRING(100.3 0, 200 0)"),     // starts 0.3 m after route 1 ends: unsnapped
        F(3, "LINESTRING(50 -10, 50 3)"),       // crosses route 1 and ends 3 m past it: overshoot
        F(4, "LINESTRING(30 2, 30 20)"),        // starts 2 m short of route 1: undershoot
        F(5, "LINESTRING(70 0, 70 -20)"),       // starts on route 1's body: connected
    };

    [Fact]
    public async Task LineEnds_EachKindIsFound()
    {
        var issues = await CheckAsync(new TopologyRule("ends", LineEndsConnectRuleType.TypeId, "Route"), ("Route", GeometryType.LineString, Routes()));

        var unsnapped = Assert.Single(issues, i => i.Kind == LineEndsConnectRuleType.UnsnappedEnd);
        Assert.Equal(new[] { "Route:1", "Route:2" }, unsnapped.Finding.Features.Select(f => f.ToString()).ToArray());
        Assert.Equal(0.3, unsnapped.Finding.Value!.Value, 6);

        var overshoot = Assert.Single(issues, i => i.Kind == LineEndsConnectRuleType.Overshoot);
        Assert.Equal("Route:3", overshoot.Finding.Features[0].ToString());
        Assert.Equal(3, overshoot.Finding.Value!.Value, 6);

        var undershoot = Assert.Single(issues, i => i.Kind == LineEndsConnectRuleType.Undershoot);
        Assert.Equal("Route:4", undershoot.Finding.Features[0].ToString());
        Assert.Equal(2, undershoot.Finding.Value!.Value, 6);

        // Route 1 start, route 2 end, route 3 start, route 4 end, route 5 end.
        Assert.Equal(5, Count(issues, LineEndsConnectRuleType.FreeEnd));
        Assert.DoesNotContain(issues, i => i.Kind == LineEndsConnectRuleType.FreeEnd && i.Finding.IsAbout("Route", 5) && i.Finding.Discriminator == "part 0 start");
    }

    [Fact]
    public async Task LineEnds_AtAPointOfAnEndCoverLayer_AreConnected()
    {
        var rule = new TopologyRule("ends", LineEndsConnectRuleType.TypeId, "Route",
            parameters: Parameters((LineEndsConnectRuleType.EndCoverLayersParameter, "Tower")));

        var issues = await CheckAsync(rule,
            ("Route", GeometryType.LineString, Routes()),
            ("Tower", GeometryType.Point, new[] { P(1, 0, 0.2), P(2, 200, 0) }));

        Assert.Equal(3, Count(issues, LineEndsConnectRuleType.FreeEnd));
    }

    [Fact]
    public async Task LineEnds_MeetingALineOfAConnectToLayer_AreConnected()
    {
        var routes = new[] { F(1, "LINESTRING(0 0, 100 0)") };
        var other = new[] { F(9, "LINESTRING(100 -10, 100 10)") };

        var without = await CheckAsync(new TopologyRule("ends", LineEndsConnectRuleType.TypeId, "Route"),
            ("Route", GeometryType.LineString, routes), ("Cable", GeometryType.LineString, other));

        var with = await CheckAsync(new TopologyRule("ends", LineEndsConnectRuleType.TypeId, "Route",
                parameters: Parameters((LineEndsConnectRuleType.ConnectToParameter, "Cable"))),
            ("Route", GeometryType.LineString, routes), ("Cable", GeometryType.LineString, other));

        Assert.Equal(2, Count(without, LineEndsConnectRuleType.FreeEnd));
        Assert.Equal(1, Count(with, LineEndsConnectRuleType.FreeEnd));
        Assert.DoesNotContain(with, i => i.Finding.Discriminator == "part 0 end");
    }

    [Fact]
    public async Task LineEnds_LinkedThroughEachOther_AreAChain()
    {
        var routes = new[]
        {
            F(1, "LINESTRING(500 0, 500 -50)"),
            F(2, "LINESTRING(500.4 0, 500.4 50)"),
            F(3, "LINESTRING(500.8 0, 500.8 -50)"),
            F(4, "LINESTRING(501.2 0, 501.2 50)"),
        };

        var issues = await CheckAsync(new TopologyRule("ends", LineEndsConnectRuleType.TypeId, "Route"), ("Route", GeometryType.LineString, routes));

        var chain = Assert.Single(issues, i => i.Kind == LineEndsConnectRuleType.Chain);
        Assert.Equal(4, chain.Finding.Features.Count);
        Assert.Equal(IssueSeverity.Error, chain.Severity);
    }

    #endregion

    #region R21 parts of a line must join

    private static Task<IReadOnlyList<TopologyIssue>> PartsAsync(string wkt)
        => CheckAsync(new TopologyRule("parts", PartsJoinRuleType.TypeId, "Route"), ("Route", GeometryType.MultiLineString, new[] { F(1, wkt) }));

    [Fact]
    public async Task Parts_JoinedInOrder_AreFine()
        => Assert.Empty(await PartsAsync("MULTILINESTRING((0 0, 10 0), (10 0, 20 0), (20 0, 30 0))"));

    [Fact]
    public async Task Parts_WithAGap_ReportTheGap()
    {
        var gap = Assert.Single(await PartsAsync("MULTILINESTRING((0 0, 10 0), (12 0, 20 0))"));

        Assert.Equal(PartsJoinRuleType.Gap, gap.Kind);
        Assert.Equal(2, gap.Finding.Value!.Value, 6);
    }

    [Fact]
    public async Task Parts_FarApart_AreDetached()
    {
        var issues = await PartsAsync("MULTILINESTRING((0 0, 10 0), (200 0, 210 0))");

        Assert.Equal(2, Count(issues, PartsJoinRuleType.DetachedPart));
        Assert.Equal(0, Count(issues, PartsJoinRuleType.Gap));
    }

    [Fact]
    public async Task Parts_MeetingThreeAtOnePlace_AreABranch()
    {
        var issues = await PartsAsync("MULTILINESTRING((0 0, 10 0), (10 0, 20 0), (10 0, 10 10))");

        Assert.Equal(PartsJoinRuleType.Branch, Assert.Single(issues).Kind);
    }

    [Fact]
    public async Task Parts_StoredOutOfOrder_AreReported()
    {
        var issues = await PartsAsync("MULTILINESTRING((10 0, 20 0), (0 0, 10 0), (20 0, 30 0))");

        Assert.Equal(PartsJoinRuleType.OutOfOrder, Assert.Single(issues).Kind);
    }

    [Fact]
    public async Task Parts_StoredBackwards_AreNotOutOfOrder()
        => Assert.Empty(await PartsAsync("MULTILINESTRING((20 0, 30 0), (10 0, 20 0), (0 0, 10 0))"));

    [Fact]
    public async Task Part_DrawnAgainstTheOthers_IsReversed()
    {
        var issue = Assert.Single(await PartsAsync("MULTILINESTRING((0 0, 10 0), (20 0, 10 0), (20 0, 30 0))"));

        Assert.Equal(PartsJoinRuleType.ReversedPart, issue.Kind);
        Assert.Equal("part 1", issue.Finding.Discriminator);
    }

    #endregion

    #region R22 duplicate lines, R23 touch and return

    [Fact]
    public async Task DuplicateLines_SameVerticesEitherWay_AreWholeLineDuplicates()
    {
        var lines = new[]
        {
            F(1, "LINESTRING(0 0, 50 0, 100 10)"),
            F(2, "LINESTRING(100 10, 50 0.005, 0 0)"),    // the same, drawn the other way, 5 mm off
            F(3, "LINESTRING(0 0, 50 0.02, 100 10)"),     // 2 cm off at one vertex: not a duplicate
        };

        var issue = Assert.Single(await CheckAsync(new TopologyRule("dup", DistinctLinesRuleType.TypeId, "Route"), ("Route", GeometryType.LineString, lines)));

        Assert.Equal(DistinctLinesRuleType.WholeLine, issue.Kind);
        Assert.Equal(new[] { "Route:1", "Route:2" }, issue.Finding.Features.Select(f => f.ToString()).ToArray());
    }

    [Fact]
    public async Task DuplicateLines_SharedStretch_OnlyWhenSwitchedOn()
    {
        var lines = new[] { F(1, "LINESTRING(0 0, 100 0)"), F(2, "LINESTRING(50 0.005, 200 0.005)") };

        Assert.Empty(await CheckAsync(new TopologyRule("dup", DistinctLinesRuleType.TypeId, "Route"), ("Route", GeometryType.LineString, lines)));

        var issue = Assert.Single(await CheckAsync(
            new TopologyRule("dup", DistinctLinesRuleType.TypeId, "Route", parameters: Parameters((DistinctLinesRuleType.SharedStretchParameter, 1))),
            ("Route", GeometryType.LineString, lines)));

        Assert.Equal(DistinctLinesRuleType.SharedStretch, issue.Kind);
        Assert.InRange(issue.Finding.Value!.Value, 49.9, 50.1);
    }

    [Fact]
    public async Task TouchAndReturn_IsFound_ButASingleCrossingIsNot()
    {
        var lines = new[]
        {
            F(1, "LINESTRING(0 0, 100 0)"),
            F(2, "LINESTRING(0 -1, 49.9 -1, 50 0.1, 50.2 0.1, 50.3 -1, 100 -1)"),   // dips across route 1 for 0.2 m
            F(3, "LINESTRING(70 -5, 70 5)"),                                         // one real crossing
        };

        var issue = Assert.Single(await CheckAsync(new TopologyRule("touch", TouchAndReturnRuleType.TypeId, "Route"), ("Route", GeometryType.LineString, lines)));

        Assert.Equal(new[] { "Route:1", "Route:2" }, issue.Finding.Features.Select(f => f.ToString()).ToArray());
        Assert.InRange(issue.Finding.Value!.Value, 0.1, 0.5);
    }

    #endregion

    #region R40 overlaps, R41 gaps

    private const string A = "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))";

    private static Task<IReadOnlyList<TopologyIssue>> PolygonsAsync(string ruleType, RuleParameters? parameters, params string[] polygons)
        => CheckAsync(new TopologyRule("poly", ruleType, "Parcel", parameters: parameters),
            ("Parcel", GeometryType.Polygon, polygons.Select((p, i) => F(i + 1, p)).ToArray()));

    [Theory]
    [InlineData("POLYGON((5 0, 15 0, 15 10, 5 10, 5 0))", PolygonsNoOverlapRuleType.Overlap, 50)]
    [InlineData("POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))", PolygonsNoOverlapRuleType.Inside, 4)]
    [InlineData("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))", PolygonsNoOverlapRuleType.Duplicate, 100)]
    [InlineData("POLYGON((9.8 0, 20 0, 20 10, 9.8 10, 9.8 0))", PolygonsNoOverlapRuleType.ThinOverlap, 2)]
    public async Task Overlaps_AreFoundAndNamed(string other, string kind, double area)
    {
        var issue = Assert.Single(await PolygonsAsync(PolygonsNoOverlapRuleType.TypeId, null, A, other));

        Assert.Equal(kind, issue.Kind);
        Assert.Equal(area, issue.Finding.Value!.Value, 6);
        Assert.Equal(ValueUnit.SquareMetres, issue.Finding.ValueUnit);
    }

    [Theory]
    [InlineData("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))")]          // neighbours sharing an edge
    [InlineData("POLYGON((9.995 0, 20 0, 20 10, 9.995 10, 9.995 0))")] // a boundary drawn twice, 5 mm apart
    public async Task NotOverlaps_AreNotReported(string other)
        => Assert.Empty(await PolygonsAsync(PolygonsNoOverlapRuleType.TypeId, null, A, other));

    [Fact]
    public async Task Sliver_BetweenNeighbours_IsFound()
    {
        var issue = Assert.Single(await PolygonsAsync(PolygonsNoGapRuleType.TypeId, null, A, "POLYGON((10.3 0, 20 0, 20 10, 10.3 10, 10.3 0))"));

        Assert.Equal(PolygonsNoGapRuleType.Sliver, issue.Kind);
        Assert.InRange(issue.Finding.Value!.Value, 9.9, 10.5);
        Assert.Equal(0.3, (double)issue.Finding.MessageArguments[0], 6);
    }

    [Theory]
    [InlineData("POLYGON((15 0, 25 0, 25 10, 15 10, 15 0))")]          // a street, wider than the distance
    [InlineData("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))")]          // a shared edge
    [InlineData("POLYGON((9.7 0, 20 0, 20 10, 9.7 10, 9.7 0))")]       // an overlap, not a gap
    public async Task NotSlivers_AreNotReported(string other)
        => Assert.Empty(await PolygonsAsync(PolygonsNoGapRuleType.TypeId, null, A, other));

    [Fact]
    public async Task EnclosedGap_OnlyWhenSwitchedOn()
    {
        var parcels = new[]
        {
            "POLYGON((0 0, 12 0, 12 10, 10 10, 10 12, 0 12, 0 0))",
            "POLYGON((12 0, 22 0, 22 12, 12 12, 12 10, 12 0))",
            "POLYGON((12 12, 22 12, 22 22, 10 22, 10 12, 12 12))",
            "POLYGON((0 12, 10 12, 10 22, 0 22, 0 12))",
        };

        Assert.Empty(await PolygonsAsync(PolygonsNoGapRuleType.TypeId, null, parcels));

        var gap = Assert.Single(await PolygonsAsync(PolygonsNoGapRuleType.TypeId, Parameters((PolygonsNoGapRuleType.EnclosedGapsParameter, 1)), parcels));

        Assert.Equal(PolygonsNoGapRuleType.EnclosedGap, gap.Kind);
        Assert.Equal(4, gap.Finding.Value!.Value, 6);
        Assert.Equal(3, gap.Finding.Features.Count);   // the fourth parcel touches the gap only at a corner
    }

    #endregion

    [Fact]
    public async Task UnrepairableGeometries_AreLeftToTheGeometryRule()
    {
        var lines = new[] { F(1, "LINESTRING(0 0, 100 0)"), new Feature<Point> { Id = 2 } };

        var issues = await CheckAsync(new TopologyRule("ends", LineEndsConnectRuleType.TypeId, "Route"), ("Route", GeometryType.LineString, lines));

        Assert.All(issues, i => Assert.True(i.Finding.IsAbout("Route", 1)));
    }
}

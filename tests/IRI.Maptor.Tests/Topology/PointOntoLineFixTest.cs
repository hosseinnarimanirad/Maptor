using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.FixTypes;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;

using Xunit;

using static IRI.Maptor.Tests.Topology.TopologyTestKit;

namespace IRI.Maptor.Tests.Topology;

/// <summary>Step 5.1: the "near a line" fix (D62 who moves, D63 fix distance) and the refused-fix rule (FR-5.6).</summary>
public class PointOntoLineFixTest
{
    private static Feature<Point> Line(int id, params (double X, double Y)[] points)
        => new(Geometry<Point>.Create(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.LineString, 0)) { Id = id };

    private static TopologyRuleSet Rules(int towerRank, int routeRank, double fixDistance = 5)
        => new("t", 1,
            new[] { new TopologyLayer("Tower", GeometryType.Point, towerRank), new TopologyLayer("Route", GeometryType.LineString, routeRank) },
            new[]
            {
                new TopologyRule("t.onRoute", PointOnLineRuleType.TypeId, "Tower", "Route",
                    new RuleParameters(new Dictionary<string, object> { ["distance"] = 0.5, ["nearDistance"] = 50, ["fixDistance"] = fixDistance })),
            });

    private sealed class Case
    {
        public Case(TopologyRuleSet ruleSet, Feature<Point>[] towers, Feature<Point>[] routes)
        {
            RuleSet = ruleSet;
            Sources = new ITopologyFeatureSource[] { new FeatureListSource("Tower", 0, towers), new FeatureListSource("Route", 0, routes) };
            Features = towers.Select(t => (Key: ("Tower", (long)t.Id), t))
                .Concat(routes.Select(r => (Key: ("Route", (long)r.Id), r)))
                .ToDictionary(p => p.Key, p => p.Item2);
        }

        public TopologyRuleSet RuleSet { get; }

        public IReadOnlyList<ITopologyFeatureSource> Sources { get; }

        public Dictionary<(string, long), Feature<Point>> Features { get; }

        public async Task<IReadOnlyList<TopologyIssue>> CheckAsync()
            => (await new ValidationRunner(StandardRuleTypes.CreateCatalog()).RunAsync(RuleSet, TopologyCheckScope.WholeData, Sources)).Issues;

        public TopologyFixOffer Propose(TopologyIssue issue)
            => new PointOntoLineFix().Propose(new TopologyFixContext(RuleSet, RuleSet.FindRule(issue.RuleId)!, issue.Finding, Features, isGeodetic: false)).Single();

        public Task<TopologyFixVerdict> VerdictAsync(TopologyIssue issue, TopologyFixProposal proposal)
            => new TopologyFixChecker(StandardRuleTypes.CreateCatalog()).CheckAsync(RuleSet, issue, proposal, Sources);
    }

    private static (double X, double Y)[] PointsOf(Geometry<Point> geometry) => geometry.Points!.Select(p => (p.X, p.Y)).ToArray();

    [Fact]
    public async Task TowerRanksHigher_WithAVertexNearby_TheVertexMovesOntoTheTower()
    {
        var c = new Case(Rules(towerRank: 2, routeRank: 1),
            new[] { PointFeature(1, 0, 0), PointFeature(2, 100, 0.8), PointFeature(3, 200, 0) },
            new[] { Line(10, (0, 0), (100, 0), (200, 0)) });

        var issue = Assert.Single(await c.CheckAsync());
        var offer = c.Propose(issue);

        Assert.True(offer.IsOffered);
        Assert.Equal(PointOntoLineFix.MoveLineVertex, offer.Proposal!.Variant);

        var edit = Assert.Single(offer.Proposal.Edits);
        Assert.Equal(("Route", 10L), (edit.EntityName, edit.FeatureId));
        Assert.Equal(new[] { (0.0, 0.0), (100.0, 0.8), (200.0, 0.0) }, PointsOf(edit.After!));
        Assert.Equal(new[] { (0.0, 0.0), (100.0, 0.0), (200.0, 0.0) }, PointsOf(edit.Before));   // the data itself is not touched
        Assert.Equal(0.8, (double)offer.Proposal.MessageArguments[1], 9);   // (point, distance, line)

        Assert.True((await c.VerdictAsync(issue, offer.Proposal)).IsAccepted);
    }

    [Fact]
    public async Task TowerRanksHigher_MidSegment_AVertexIsAddedAtTheTower()
    {
        var c = new Case(Rules(towerRank: 2, routeRank: 1),
            new[] { PointFeature(1, 0, 0), PointFeature(2, 100, 0.8), PointFeature(3, 200, 0) },
            new[] { Line(10, (0, 0), (200, 0)) });

        var issue = Assert.Single(await c.CheckAsync());
        var proposal = c.Propose(issue).Proposal!;

        Assert.Equal(PointOntoLineFix.InsertLineVertex, proposal.Variant);
        Assert.Equal(new[] { (0.0, 0.0), (100.0, 0.8), (200.0, 0.0) }, PointsOf(proposal.Edits[0].After!));
        Assert.True((await c.VerdictAsync(issue, proposal)).IsAccepted);
    }

    [Theory]
    [InlineData(1, 1)]   // equal ranks: the point moves
    [InlineData(1, 2)]   // the line ranks higher
    public async Task OtherwiseThePointMovesOntoTheLine(int towerRank, int routeRank)
    {
        var c = new Case(Rules(towerRank, routeRank),
            new[] { PointFeature(1, 0, 0), PointFeature(2, 100, 0.8), PointFeature(3, 200, 0) },
            new[] { Line(10, (0, 0), (200, 0)) });

        var issue = Assert.Single(await c.CheckAsync());
        var proposal = c.Propose(issue).Proposal!;

        Assert.Equal(PointOntoLineFix.MovePoint, proposal.Variant);

        var edit = Assert.Single(proposal.Edits);
        Assert.Equal(("Tower", 2L), (edit.EntityName, edit.FeatureId));
        Assert.Equal(100, edit.After!.Points![0].X, 9);
        Assert.Equal(0, edit.After.Points[0].Y, 9);
        Assert.True((await c.VerdictAsync(issue, proposal)).IsAccepted);
    }

    [Fact]
    public async Task FartherThanTheFixDistance_NoFixIsOffered()
    {
        var c = new Case(Rules(towerRank: 2, routeRank: 1),
            new[] { PointFeature(1, 0, 0), PointFeature(2, 100, 8), PointFeature(3, 200, 0) },
            new[] { Line(10, (0, 0), (200, 0)) });

        var issue = Assert.Single(await c.CheckAsync());
        Assert.Equal(PointOnLineRuleType.NearLine, issue.Kind);

        var offer = c.Propose(issue);

        Assert.False(offer.IsOffered);
        Assert.Equal(FixReasons.BeyondFixDistance, offer.Reason);
        Assert.Equal(8, (double)offer.ReasonArguments[0], 9);
        Assert.Equal(5, (double)offer.ReasonArguments[1], 9);
    }

    [Fact]
    public async Task AFixThatPullsTheLineOffAnotherTower_IsRefused()
    {
        // Tower 4 stands on the route's middle vertex; tower 2 is 1.3 m from that vertex. Moving the vertex to tower 2
        // would leave tower 4 off the route: a new error, as severe as the one being fixed.
        var c = new Case(Rules(towerRank: 2, routeRank: 1),
            new[] { PointFeature(1, 0, 0), PointFeature(2, 101, 0.8), PointFeature(3, 200, 0), PointFeature(4, 100, 0) },
            new[] { Line(10, (0, 0), (100, 0), (200, 0)) });

        var issue = Assert.Single(await c.CheckAsync());
        var proposal = c.Propose(issue).Proposal!;

        Assert.Equal(PointOntoLineFix.MoveLineVertex, proposal.Variant);

        var verdict = await c.VerdictAsync(issue, proposal);

        Assert.False(verdict.IsAccepted);
        Assert.Equal(FixReasons.CreatesIssue, verdict.Reason);
        Assert.True(verdict.CausedIssue!.Finding.IsAbout("Tower", 4));
    }

    [Fact]
    public async Task AFixThatLeavesTheIssue_IsRefused()
    {
        var c = new Case(Rules(towerRank: 1, routeRank: 1),
            new[] { PointFeature(1, 0, 0), PointFeature(2, 100, 0.8), PointFeature(3, 200, 0) },
            new[] { Line(10, (0, 0), (200, 0)) });

        var issue = Assert.Single(await c.CheckAsync());

        var tower = c.Features[("Tower", 2)];
        var halfway = new FeatureEdit("Tower", 2, tower.TheGeometry, Geometry<Point>.Create(100, 0.6, 0));
        var proposal = new TopologyFixProposal("test", "halfway", issue.Finding, new[] { halfway }, new BoundingBox(99, -1, 101, 1));

        var verdict = await c.VerdictAsync(issue, proposal);

        Assert.Equal(FixReasons.DoesNotResolve, verdict.Reason);
    }

    [Fact]
    public void TheCatalog_KnowsWhichFixSuitsWhichKind()
    {
        var catalog = StandardRuleTypes.CreateCatalog();

        Assert.Equal(PointOntoLineFix.TypeId, Assert.Single(catalog.FixesFor(PointOnLineRuleType.TypeId, PointOnLineRuleType.NearLine)).Id);
        Assert.Empty(catalog.FixesFor(PointOnLineRuleType.TypeId, PointOnLineRuleType.FarFromLines));
        Assert.Throws<ArgumentException>(() => catalog.RegisterFix(new PointOntoLineFix()));
    }

    [Fact]
    public void ANegativeFixDistance_IsASettingProblem()
    {
        var problems = RuleSetValidator.Validate(Rules(2, 1, fixDistance: -1), StandardRuleTypes.CreateCatalog());

        Assert.Contains(problems, p => p.Contains("fix distance"));
    }
}

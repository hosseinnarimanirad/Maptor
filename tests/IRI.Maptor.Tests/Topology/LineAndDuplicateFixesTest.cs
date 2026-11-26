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

/// <summary>Step 5.2: snap, trim, extend (D64) and delete-a-duplicate fixes, each checked again (FR-5.6).</summary>
public class LineAndDuplicateFixesTest
{
    private static Feature<Point> Line(int id, params (double X, double Y)[] points)
        => new(Geometry<Point>.Create(points.Select(p => new Point(p.X, p.Y)).ToList(), GeometryType.LineString, 0)) { Id = id };

    private static readonly TopologyRuleCatalog Catalog = StandardRuleTypes.CreateCatalog();

    private static TopologyRuleSet Rules(params TopologyRule[] rules)
        => new("t", 1, new[] { new TopologyLayer("Route", GeometryType.LineString), new TopologyLayer("Tower", GeometryType.Point) }, rules);

    private static TopologyRule EndsRule()
        => new("t.ends", LineEndsConnectRuleType.TypeId, "Route",
            parameters: new RuleParameters(new Dictionary<string, object> { ["distance"] = 0.5, ["searchDistance"] = 5, ["maxOvershoot"] = 5 }));

    /// <summary>Checks, finds the issue, asks the catalogue's fix types for it, and checks each offer again.</summary>
    private static async Task<(TopologyIssue Issue, List<(TopologyFixOffer Offer, TopologyFixVerdict? Verdict)> Offers)> FixAsync(
        TopologyRuleSet ruleSet, Feature<Point>[] routes, Feature<Point>[] towers, System.Func<TopologyIssue, bool> pick)
    {
        var sources = new ITopologyFeatureSource[] { new FeatureListSource("Route", 0, routes), new FeatureListSource("Tower", 0, towers) };

        var result = await new ValidationRunner(Catalog).RunAsync(ruleSet, TopologyCheckScope.WholeData, sources);
        var issue = Assert.Single(result.Issues, i => pick(i));

        var rule = ruleSet.FindRule(issue.RuleId)!;
        var features = routes.Select(r => (Key: ("Route", (long)r.Id), r)).Concat(towers.Select(t => (Key: ("Tower", (long)t.Id), t))).ToDictionary(p => p.Key, p => p.Item2);

        var offers = Catalog.FixesFor(rule.RuleTypeId, issue.Kind)
            .SelectMany(f => f.Propose(new TopologyFixContext(ruleSet, rule, issue.Finding, features, isGeodetic: false)))
            .ToList();

        var checker = new TopologyFixChecker(Catalog);
        var checkedOffers = new List<(TopologyFixOffer, TopologyFixVerdict?)>();

        foreach (var offer in offers)
            checkedOffers.Add((offer, offer.IsOffered ? await checker.CheckAsync(ruleSet, issue, offer.Proposal!, sources) : null));

        return (issue, checkedOffers);
    }

    private static (double X, double Y)[] PointsOf(Geometry<Point> geometry) => geometry.Points!.Select(p => (System.Math.Round(p.X, 9), System.Math.Round(p.Y, 9))).ToArray();

    [Fact]
    public async Task UnsnappedEnds_MeetAtTheFirstEnd()
    {
        var routes = new[] { Line(1, (0, 0), (100, 0)), Line(2, (100.3, 0), (200, 0)) };

        var (_, offers) = await FixAsync(Rules(EndsRule()), routes, new Feature<Point>[0], i => i.Kind == LineEndsConnectRuleType.UnsnappedEnd);

        var (offer, verdict) = Assert.Single(offers);
        Assert.Equal(SnapLineEndsFix.Snap, offer.Proposal!.Variant);

        var edit = Assert.Single(offer.Proposal.Edits);
        Assert.Equal(2, edit.FeatureId);
        Assert.Equal(new[] { (100.0, 0.0), (200.0, 0.0) }, PointsOf(edit.After!));
        Assert.Equal(0.3, (double)offer.Proposal.MessageArguments[1], 6);
        Assert.True(verdict!.IsAccepted);
    }

    [Fact]
    public async Task Overshoot_IsCutBackToTheCrossing()
    {
        var routes = new[] { Line(1, (0, 0), (100, 0)), Line(2, (98, -50), (98, 50)) };

        var (_, offers) = await FixAsync(Rules(EndsRule()), routes, new Feature<Point>[0], i => i.Kind == LineEndsConnectRuleType.Overshoot);

        var (offer, verdict) = Assert.Single(offers);
        Assert.Equal(TrimOvershootFix.Trim, offer.Proposal!.Variant);
        Assert.Equal(new[] { (0.0, 0.0), (98.0, 0.0) }, PointsOf(offer.Proposal.Edits[0].After!));
        Assert.Equal(2, (double)offer.Proposal.MessageArguments[1], 6);
        Assert.True(verdict!.IsAccepted);
    }

    [Fact]
    public async Task Overshoot_AtTheStartOfALine_IsCutBackToo()
    {
        var routes = new[] { Line(1, (100, 0), (0, 0)), Line(2, (98, -50), (98, 50)) };

        var (_, offers) = await FixAsync(Rules(EndsRule()), routes, new Feature<Point>[0], i => i.Kind == LineEndsConnectRuleType.Overshoot);

        Assert.Equal(new[] { (98.0, 0.0), (0.0, 0.0) }, PointsOf(offers[0].Offer.Proposal!.Edits[0].After!));
        Assert.True(offers[0].Verdict!.IsAccepted);
    }

    [Fact]
    public async Task Undershoot_GoesStraightOn_WhenThatReachesTheLine()
    {
        var routes = new[] { Line(1, (0, 0), (100, 0)), Line(2, (103, -50), (103, 50)) };

        var (_, offers) = await FixAsync(Rules(EndsRule()), routes, new Feature<Point>[0], i => i.Kind == LineEndsConnectRuleType.Undershoot);

        var (offer, verdict) = Assert.Single(offers);
        Assert.Equal(ExtendUndershootFix.ExtendAlong, offer.Proposal!.Variant);
        Assert.Equal(new[] { (0.0, 0.0), (103.0, 0.0) }, PointsOf(offer.Proposal.Edits[0].After!));
        Assert.True(verdict!.IsAccepted);
    }

    [Fact]
    public async Task Undershoot_JoinsTheNearestPoint_WhenStraightOnMisses()
    {
        // Straight on (east) never meets route 2, which starts 2 m north-east of the end.
        var routes = new[] { Line(1, (0, 0), (100, 0)), Line(2, (101, 2), (110, 20)) };

        var (_, offers) = await FixAsync(Rules(EndsRule()), routes, new Feature<Point>[0],
            i => i.Kind == LineEndsConnectRuleType.Undershoot && i.Finding.Features[0].FeatureId == 1);

        var (offer, verdict) = Assert.Single(offers);
        Assert.Equal(ExtendUndershootFix.JoinNearest, offer.Proposal!.Variant);
        Assert.Equal(new[] { (0.0, 0.0), (100.0, 0.0), (101.0, 2.0) }, PointsOf(offer.Proposal.Edits[0].After!));
        Assert.True(verdict!.IsAccepted);
    }

    [Fact]
    public async Task DuplicatePoints_OneChoicePerFeature_TheOthersDeleted()
    {
        var ruleSet = Rules(new TopologyRule("t.towers", DistinctPointsRuleType.TypeId, "Tower"));
        var towers = new[] { PointFeature(1, 10, 10), PointFeature(2, 10, 10), PointFeature(3, 50, 50) };

        var (_, offers) = await FixAsync(ruleSet, new Feature<Point>[0], towers, i => i.Kind == DistinctPointsRuleType.SamePlace);

        Assert.Equal(2, offers.Count);
        Assert.All(offers, o => Assert.Equal(DeleteDuplicateFix.Keep, o.Offer.Proposal!.Variant));
        Assert.All(offers, o => Assert.True(o.Verdict!.IsAccepted));

        Assert.Equal(1, ((FindingFeature)offers[0].Offer.Proposal!.MessageArguments[0]).FeatureId);
        var deleted = Assert.Single(offers[0].Offer.Proposal!.Edits);
        Assert.True(deleted.IsRemoval);
        Assert.Equal(2, deleted.FeatureId);

        Assert.Equal(1, Assert.Single(offers[1].Offer.Proposal!.Edits).FeatureId);
    }

    [Fact]
    public async Task DuplicateLines_KeepOne_DeleteTheOther()
    {
        var ruleSet = Rules(new TopologyRule("t.routes", DistinctLinesRuleType.TypeId, "Route"));
        var routes = new[] { Line(1, (0, 0), (100, 0)), Line(2, (100, 0), (0, 0)) };

        var (_, offers) = await FixAsync(ruleSet, routes, new Feature<Point>[0], i => i.Kind == DistinctLinesRuleType.WholeLine);

        Assert.Equal(2, offers.Count);
        Assert.All(offers, o => Assert.True(o.Verdict!.IsAccepted));
        Assert.True(Assert.Single(offers[0].Offer.Proposal!.Edits).IsRemoval);
    }

    [Fact]
    public void AChain_AndAFreeEnd_GetNoFix()
    {
        Assert.Empty(Catalog.FixesFor(DistinctPointsRuleType.TypeId, DistinctPointsRuleType.Chain));
        Assert.Empty(Catalog.FixesFor(LineEndsConnectRuleType.TypeId, LineEndsConnectRuleType.Chain));
        Assert.Empty(Catalog.FixesFor(LineEndsConnectRuleType.TypeId, LineEndsConnectRuleType.FreeEnd));
        Assert.Empty(Catalog.FixesFor(DistinctLinesRuleType.TypeId, DistinctLinesRuleType.SharedStretch));
    }
}

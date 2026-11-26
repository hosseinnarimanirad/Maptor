using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.FixTypes;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;

using Xunit;

namespace IRI.Maptor.Tests.Topology;

/// <summary>Step 5.3: cut an overlap out of one polygon, give a gap or sliver to one neighbour; each checked again.</summary>
public class PolygonFixesTest
{
    private static readonly TopologyRuleCatalog Catalog = StandardRuleTypes.CreateCatalog();

    private static Feature<Point> Parcel(int id, string wkt) => new(Geometry<Point>.FromWkt(wkt, 0)) { Id = id };

    private static string Box(double x0, double y0, double x1, double y1)
        => FormattableString.Invariant($"POLYGON(({x0} {y0}, {x1} {y0}, {x1} {y1}, {x0} {y1}, {x0} {y0}))");

    private static TopologyRuleSet Rules(bool enclosedGaps = false)
        => new("t", 1, new[] { new TopologyLayer("Parcel", GeometryType.Polygon) },
            new[]
            {
                new TopologyRule("t.overlap", PolygonsNoOverlapRuleType.TypeId, "Parcel"),
                new TopologyRule("t.gap", PolygonsNoGapRuleType.TypeId, "Parcel",
                    parameters: new RuleParameters(new Dictionary<string, object> { ["distance"] = 0.5, ["enclosedGaps"] = enclosedGaps ? 1 : 0 })),
            });

    private static double Area(Geometry<Point>? geometry) => geometry is null ? 0 : PolygonMeasure.Area(geometry, isGeodetic: false);

    private static async Task<List<(TopologyFixOffer Offer, TopologyFixVerdict? Verdict)>> FixAsync(TopologyRuleSet ruleSet, Feature<Point>[] parcels, string kind)
    {
        var sources = new ITopologyFeatureSource[] { new FeatureListSource("Parcel", 0, parcels) };

        var result = await new ValidationRunner(Catalog).RunAsync(ruleSet, TopologyCheckScope.WholeData, sources);
        var issue = Assert.Single(result.Issues, i => i.Kind == kind);

        var rule = ruleSet.FindRule(issue.RuleId)!;
        var features = parcels.ToDictionary(p => ("Parcel", (long)p.Id), p => p);

        var checker = new TopologyFixChecker(Catalog);
        var offers = new List<(TopologyFixOffer, TopologyFixVerdict?)>();

        foreach (var offer in Catalog.FixesFor(rule.RuleTypeId, issue.Kind).SelectMany(f => f.Propose(new TopologyFixContext(ruleSet, rule, issue.Finding, features, isGeodetic: false))))
            offers.Add((offer, offer.IsOffered ? await checker.CheckAsync(ruleSet, issue, offer.Proposal!, sources) : null));

        return offers;
    }

    [Fact]
    public async Task Overlap_CanBeCutOutOfEither_AndThenTheyShareAnEdge()
    {
        var parcels = new[] { Parcel(1, Box(0, 0, 10, 10)), Parcel(2, Box(8, 0, 18, 10)) };

        var offers = await FixAsync(Rules(), parcels, PolygonsNoOverlapRuleType.Overlap);

        Assert.Equal(2, offers.Count);
        Assert.All(offers, o => Assert.Equal(CutOverlapFix.CutFrom, o.Offer.Proposal!.Variant));
        Assert.All(offers, o => Assert.True(o.Verdict!.IsAccepted, o.Verdict.Reason));

        var cutFirst = Assert.Single(offers[0].Offer.Proposal!.Edits);
        Assert.Equal(1, cutFirst.FeatureId);
        Assert.Equal(80, Area(cutFirst.After), 6);

        Assert.Equal(2, Assert.Single(offers[1].Offer.Proposal!.Edits).FeatureId);
    }

    [Fact]
    public async Task ThinOverlap_IsCutToo()
    {
        var parcels = new[] { Parcel(1, Box(0, 0, 10, 10)), Parcel(2, Box(9.8, 0, 20, 10)) };

        var offers = await FixAsync(Rules(), parcels, PolygonsNoOverlapRuleType.ThinOverlap);

        Assert.Equal(98, Area(offers[0].Offer.Proposal!.Edits[0].After), 6);
        Assert.True(offers[0].Verdict!.IsAccepted);
    }

    [Fact]
    public async Task APolygonInside_CutsAHole_ButTheInnerOneCannotBeCut()
    {
        var parcels = new[] { Parcel(1, Box(0, 0, 10, 10)), Parcel(2, Box(2, 2, 4, 4)) };

        var offers = await FixAsync(Rules(), parcels, PolygonsNoOverlapRuleType.Inside);

        var outer = Assert.Single(offers, o => o.Offer.IsOffered);
        Assert.Equal(96, Area(outer.Offer.Proposal!.Edits[0].After), 6);
        Assert.True(outer.Verdict!.IsAccepted);

        var inner = Assert.Single(offers, o => !o.Offer.IsOffered);
        Assert.Equal(FixReasons.NothingLeft, inner.Offer.Reason);
        Assert.Equal(2, ((FindingFeature)inner.Offer.ReasonArguments[0]).FeatureId);
    }

    [Fact]
    public async Task DuplicatePolygons_KeepOne()
    {
        var parcels = new[] { Parcel(1, Box(0, 0, 10, 10)), Parcel(2, Box(0, 0, 10, 10)) };

        var offers = await FixAsync(Rules(), parcels, PolygonsNoOverlapRuleType.Duplicate);

        Assert.Equal(2, offers.Count);
        Assert.All(offers, o => Assert.Equal(DeleteDuplicateFix.PolygonsTypeId, o.Offer.FixTypeId));
        Assert.All(offers, o => Assert.True(o.Verdict!.IsAccepted));
    }

    [Fact]
    public async Task Sliver_CanBeGivenToEither_AndThenTheyShareAnEdge()
    {
        var parcels = new[] { Parcel(1, Box(0, 0, 10, 10)), Parcel(2, Box(10.3, 0, 20, 10)) };

        var offers = await FixAsync(Rules(), parcels, PolygonsNoGapRuleType.Sliver);

        Assert.Equal(2, offers.Count);
        Assert.All(offers, o => Assert.Equal(GiveGapFix.TakeSliver, o.Offer.Proposal!.Variant));
        Assert.All(offers, o => Assert.True(o.Verdict!.IsAccepted, o.Verdict.Reason + " " + o.Verdict.CausedIssue));

        var first = offers.Single(o => o.Offer.Proposal!.Edits[0].FeatureId == 1).Offer.Proposal!.Edits[0];
        Assert.Equal(103, Area(first.After), 6);
        Assert.Single(first.After!.Geometries!);   // one ring: no spike, no hole

        var second = offers.Single(o => o.Offer.Proposal!.Edits[0].FeatureId == 2).Offer.Proposal!.Edits[0];
        Assert.Equal(100, Area(second.After), 6);
    }

    [Fact]
    public async Task EnclosedGap_CanBeGivenToAnyNeighbour()
    {
        // Two L-shaped parcels around a 2 × 2 hole.
        var parcels = new[]
        {
            Parcel(1, "POLYGON((0 0, 10 0, 10 4, 4 4, 4 6, 0 6, 0 0))"),
            Parcel(2, "POLYGON((10 0, 10 10, 0 10, 0 6, 6 6, 6 4, 10 4, 10 0))"),
        };

        var offers = await FixAsync(Rules(enclosedGaps: true), parcels, PolygonsNoGapRuleType.EnclosedGap);

        Assert.Equal(2, offers.Count);
        Assert.All(offers, o => Assert.Equal(GiveGapFix.TakeGap, o.Offer.Proposal!.Variant));
        Assert.All(offers, o => Assert.True(o.Verdict!.IsAccepted, o.Verdict.Reason));

        var area1 = Area(parcels[0].TheGeometry);
        Assert.Equal(area1 + 4, Area(offers.Single(o => o.Offer.Proposal!.Edits[0].FeatureId == 1).Offer.Proposal!.Edits[0].After), 6);
    }

    [Fact]
    public void ThePolygonFixes_AreRegistered()
    {
        Assert.Equal(CutOverlapFix.TypeId, Assert.Single(Catalog.FixesFor(PolygonsNoOverlapRuleType.TypeId, PolygonsNoOverlapRuleType.Overlap)).Id);
        Assert.Equal(GiveGapFix.TypeId, Assert.Single(Catalog.FixesFor(PolygonsNoGapRuleType.TypeId, PolygonsNoGapRuleType.Sliver)).Id);
    }
}

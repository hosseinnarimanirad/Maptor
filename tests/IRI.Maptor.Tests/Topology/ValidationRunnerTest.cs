using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Rules;

using Xunit;

using static IRI.Maptor.Tests.Topology.TopologyTestKit;

namespace IRI.Maptor.Tests.Topology;

public class ValidationRunnerTest
{
    private static ValidationRunner Runner(params ITopologyRuleType[] ruleTypes) => new(new TopologyRuleCatalog(ruleTypes));

    private static FeatureListSource Source(string layer, int srid, params Feature<Point>[] features) => new(layer, srid, features);

    private static Task<TopologyCheckResult> RunAsync(ITopologyRuleType ruleType, TopologyRuleSet ruleSet, TopologyCheckScope scope, params ITopologyFeatureSource[] sources)
        => Runner(ruleType).RunAsync(ruleSet, scope, sources);

    [Fact]
    public async Task WholeData_ReportsEveryFinding_InRuleKindIdentityOrder()
    {
        var ruleSet = RuleSet(new TopologyRule("r", "everyFeature", "P"));

        var result = await RunAsync(new EveryFeatureRuleType(), ruleSet, TopologyCheckScope.WholeData,
            Source("P", 0, PointFeature(3, 3, 0), PointFeature(1, 1, 0), PointFeature(2, 2, 0)));

        Assert.Equal(
            new[] { "seen P:1", "seen P:2", "seen P:3", "even P:2" },
            result.Issues.Select(i => $"{i.Kind} {i.Finding.Features[0]}").ToArray());

        Assert.Equal(4, result.Summary.IssueCount);
        Assert.Equal(3, result.Summary.FeaturesRead["P"]);
        Assert.Equal(3, result.Summary.CountBySeverity[IssueSeverity.Warning]);
        Assert.Equal(1, result.Summary.CountByRuleAndKind[("r", "even")]);
    }

    [Fact]
    public async Task HiddenKinds_AreDropped_AndSeveritiesApplied()
    {
        var rule = new TopologyRule("r", "everyFeature", "P",
            severities: new Dictionary<string, IssueSeverity> { ["seen"] = IssueSeverity.Error },
            hiddenKinds: new[] { "even" });

        var result = await RunAsync(new EveryFeatureRuleType(), RuleSet(rule), TopologyCheckScope.WholeData,
            Source("P", 0, PointFeature(1, 1, 0), PointFeature(2, 2, 0)));

        Assert.Equal(2, result.Issues.Count);
        Assert.All(result.Issues, i => Assert.Equal("seen", i.Kind));
        Assert.All(result.Issues, i => Assert.Equal(IssueSeverity.Error, i.Severity));
        Assert.All(result.Issues, i => Assert.Equal(IssueSeverity.Warning, i.Finding.Severity));
    }

    [Fact]
    public async Task DisabledRules_DoNotRun()
    {
        var ruleType = new EveryFeatureRuleType();

        var result = await RunAsync(ruleType, RuleSet(new TopologyRule("r", "everyFeature", "P", enabled: false)), TopologyCheckScope.WholeData,
            Source("P", 0, PointFeature(1, 1, 0)));

        Assert.Empty(result.Issues);
        Assert.Equal(0, ruleType.Calls);
        Assert.Empty(result.Summary.RulesChecked);
    }

    [Fact]
    public async Task LayerScope_RunsOnlyTheRulesOfThatLayer()
    {
        var ruleSet = RuleSet(new TopologyRule("onP", "everyFeature", "P"), new TopologyRule("onQ", "everyFeature", "Q"));

        var result = await RunAsync(new EveryFeatureRuleType(), ruleSet, TopologyCheckScope.ForLayer("Q"),
            Source("P", 0, PointFeature(1, 1, 0)),
            Source("Q", 0, PointFeature(5, 5, 0)));

        Assert.Equal(new[] { "onQ" }, result.Summary.RulesChecked);
        Assert.All(result.Issues, i => Assert.Equal("onQ", i.RuleId));
        Assert.False(result.Summary.FeaturesRead.ContainsKey("P"));
    }

    [Fact]
    public async Task ExtentScope_LoadsNeighboursWithinReach_ButKeepsOnlyIssuesInside()
    {
        var ruleSet = RuleSet(new TopologyRule("r", "everyFeature", "P", hiddenKinds: new[] { "even" }));

        var result = await RunAsync(new EveryFeatureRuleType(reach: 5), ruleSet, TopologyCheckScope.ForExtent(new BoundingBox(0, 0, 10, 10)),
            Source("P", 0,
                PointFeature(1, 5, 5),     // inside
                PointFeature(3, 13, 5),    // 3 m outside: loaded as a neighbour, its issue dropped
                PointFeature(5, 40, 5)));  // far outside: not loaded

        Assert.Equal(2, result.Summary.FeaturesRead["P"]);
        Assert.Equal("P:1", Assert.Single(result.Issues).Finding.Features[0].ToString());
    }

    [Fact]
    public async Task ExtentScope_OnGeodeticData_GrowsTheBoxInMetres()
    {
        // At 35.7° N, 0.001° of longitude is about 90 m: inside a 100 m reach. 0.01° (about 900 m) is not.
        var ruleSet = RuleSet(new TopologyRule("r", "everyFeature", "P"));

        var result = await RunAsync(new EveryFeatureRuleType(reach: 100), ruleSet,
            TopologyCheckScope.ForExtent(new BoundingBox(51.0, 35.7, 51.01, 35.71)),
            Source("P", 4326,
                PointFeature(1, 51.005, 35.705, 4326),
                PointFeature(3, 51.011, 35.705, 4326),
                PointFeature(5, 51.02, 35.705, 4326)));

        Assert.Equal(2, result.Summary.FeaturesRead["P"]);
    }

    [Fact]
    public async Task FeaturesScope_KeepsOnlyIssuesAboutTheChosenFeatures()
    {
        var ruleSet = RuleSet(new TopologyRule("r", "everyFeature", "P"));

        var result = await RunAsync(new EveryFeatureRuleType(reach: 5), ruleSet, TopologyCheckScope.ForFeatures("P", new long[] { 2 }),
            Source("P", 0, PointFeature(1, 0, 0), PointFeature(2, 1, 0), PointFeature(9, 100, 0)));

        Assert.Equal(2, result.Summary.FeaturesRead["P"]);
        Assert.All(result.Issues, i => Assert.True(i.Finding.IsAbout("P", 2)));
        Assert.Equal(2, result.Issues.Count);
    }

    [Fact]
    public async Task PairRules_GetBothLayers_AndShareWorkWithinOneCheck()
    {
        var ruleType = new PairRuleType();
        var ruleSet = RuleSet(new TopologyRule("one", "pair", "P", "Q"), new TopologyRule("two", "pair", "Q", "P"));

        var result = await RunAsync(ruleType, ruleSet, TopologyCheckScope.WholeData,
            Source("P", 0, PointFeature(1, 0, 0)),
            Source("Q", 0, PointFeature(7, 0, 0), PointFeature(8, 1, 0)));

        Assert.Equal(4, result.Issues.Count);
        Assert.Equal(1, ruleType.SharedBuilds);
        Assert.All(result.Issues, i => Assert.Equal(2, i.Finding.Features.Count));
    }

    [Fact]
    public async Task SameIdentityTwice_IsOneIssue_TheFirstKept()
    {
        var result = await RunAsync(new TwiceRuleType(), RuleSet(new TopologyRule("r", "twice", "P")), TopologyCheckScope.WholeData,
            Source("P", 0, PointFeature(1, 0, 0)));

        Assert.Null(Assert.Single(result.Issues).Finding.Value);
    }

    [Fact]
    public async Task Progress_IsReportedForEachRule()
    {
        var progress = new RecordingProgress<TopologyCheckProgress>();
        var ruleSet = RuleSet(new TopologyRule("a", "everyFeature", "P"), new TopologyRule("b", "everyFeature", "Q"));

        await Runner(new EveryFeatureRuleType()).RunAsync(ruleSet, TopologyCheckScope.WholeData,
            new[] { Source("P", 0), Source("Q", 0) }, progress);

        Assert.Equal(new[] { "1/2: a", "2/2: b" }, progress.Reports.Select(p => p.ToString()).ToArray());
    }

    [Fact]
    public async Task Cancellation_StopsTheCheck()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Runner(new EveryFeatureRuleType()).RunAsync(RuleSet(new TopologyRule("r", "everyFeature", "P")), TopologyCheckScope.WholeData,
                new[] { Source("P", 0, PointFeature(1, 0, 0)) }, cancellationToken: cancel.Token));
    }

    [Theory]
    [InlineData(3857)]
    [InlineData(102100)]
    public async Task WebMercator_IsRefused(int srid)
    {
        await Assert.ThrowsAsync<TopologyCheckException>(() =>
            RunAsync(new EveryFeatureRuleType(), RuleSet(new TopologyRule("r", "everyFeature", "P")), TopologyCheckScope.WholeData,
                Source("P", srid)));
    }

    [Fact]
    public async Task LayersInDifferentSrs_AreRefused()
    {
        await Assert.ThrowsAsync<TopologyCheckException>(() =>
            RunAsync(new PairRuleType(), RuleSet(new TopologyRule("r", "pair", "P", "Q")), TopologyCheckScope.WholeData,
                Source("P", 4326), Source("Q", 32639)));
    }

    [Fact]
    public async Task MissingSource_IsRefused()
    {
        await Assert.ThrowsAsync<TopologyCheckException>(() =>
            RunAsync(new PairRuleType(), RuleSet(new TopologyRule("r", "pair", "P", "Q")), TopologyCheckScope.WholeData,
                Source("P", 0)));
    }

    [Fact]
    public async Task InvalidRuleSet_IsRefused_WithItsProblems()
    {
        var error = await Assert.ThrowsAsync<TopologyCheckException>(() =>
            RunAsync(new EveryFeatureRuleType(), RuleSet(new TopologyRule("r", "nope", "P")), TopologyCheckScope.WholeData,
                Source("P", 0)));

        Assert.Single(error.Problems);
    }

    [Fact]
    public async Task SameData_GivesTheSameList()
    {
        var ruleSet = RuleSet(new TopologyRule("r", "everyFeature", "P"));
        var features = Enumerable.Range(1, 50).Select(i => PointFeature(i, i, i % 7)).ToArray();

        var first = await RunAsync(new EveryFeatureRuleType(), ruleSet, TopologyCheckScope.WholeData, Source("P", 0, features));
        var second = await RunAsync(new EveryFeatureRuleType(), ruleSet, TopologyCheckScope.WholeData, Source("P", 0, features.Reverse().ToArray()));

        Assert.Equal(first.Issues.Select(i => i.IdentityHash), second.Issues.Select(i => i.IdentityHash));
    }

    [Fact]
    public async Task FeatureListSource_FiltersByBox()
    {
        var source = Source("P", 0, PointFeature(1, 1, 1), PointFeature(2, 50, 50), new Feature<Point>() { Id = 3 });

        var inBox = await source.GetFeaturesAsync(new BoundingBox(0, 0, 10, 10), Array.Empty<string>(), CancellationToken.None);
        var all = await source.GetFeaturesAsync(null, Array.Empty<string>(), CancellationToken.None);
        var byId = await source.GetFeaturesByIdsAsync(new long[] { 2, 99 }, Array.Empty<string>(), CancellationToken.None);

        Assert.Equal(new[] { 1 }, inBox.Select(f => f.Id).ToArray());
        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { 2 }, byId.Select(f => f.Id).ToArray());
    }
}

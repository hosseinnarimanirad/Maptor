using System.Collections.Generic;
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

using static IRI.Maptor.Tests.Topology.TopologyTestKit;

namespace IRI.Maptor.Tests.Topology;

public class DistinctPointsRuleTypeTest
{
    private static async Task<IReadOnlyList<TopologyIssue>> CheckAsync(double? distance, int srid, params Feature<Point>[] towers)
    {
        var parameters = distance is null ? null : new RuleParameters(new Dictionary<string, object> { ["distance"] = distance.Value });

        var ruleSet = new TopologyRuleSet("t", 1, new[] { new TopologyLayer("Tower", GeometryType.Point) },
            new[] { new TopologyRule("t.distinct", DistinctPointsRuleType.TypeId, "Tower", parameters: parameters) });

        var result = await new ValidationRunner(StandardRuleTypes.CreateCatalog())
            .RunAsync(ruleSet, TopologyCheckScope.WholeData, new[] { new FeatureListSource("Tower", srid, towers) });

        return result.Issues;
    }

    [Fact]
    public async Task PointsAtTheSamePlace_AreOneIssue_WithAllOfThem()
    {
        var issues = await CheckAsync(null, 0, PointFeature(7, 10, 10), PointFeature(3, 10, 10), PointFeature(5, 10.005, 10), PointFeature(9, 50, 50));

        var issue = Assert.Single(issues);

        Assert.Equal(DistinctPointsRuleType.SamePlace, issue.Kind);
        Assert.Equal(new[] { "Tower:3", "Tower:5", "Tower:7" }, issue.Finding.Features.Select(f => f.ToString()).ToArray());
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.Equal(ValueUnit.Metres, issue.Finding.ValueUnit);
    }

    [Fact]
    public async Task CloseButNotTheSamePlace_IsClose()
    {
        var issue = Assert.Single(await CheckAsync(0.5, 0, PointFeature(1, 0, 0), PointFeature(2, 0.3, 0)));

        Assert.Equal(DistinctPointsRuleType.Close, issue.Kind);
        Assert.Equal(0.3, issue.Finding.Value!.Value, 9);
    }

    [Fact]
    public async Task LinkedThroughEachOther_IsAChain_AndAnError()
    {
        var issue = Assert.Single(await CheckAsync(0.5, 0, PointFeature(1, 0, 0), PointFeature(2, 0.4, 0), PointFeature(3, 0.8, 0), PointFeature(4, 1.2, 0)));

        Assert.Equal(DistinctPointsRuleType.Chain, issue.Kind);
        Assert.Equal(IssueSeverity.Error, issue.Severity);
        Assert.Equal(4, issue.Finding.Features.Count);
    }

    [Fact]
    public async Task OnGeodeticData_TheDistanceIsInMetres()
    {
        // 0.00001° of longitude at 35.7° N is about 0.9 m: apart at 0.5 m, together at 1 m.
        var a = PointFeature(1, 51.0, 35.7, 4326);
        var b = PointFeature(2, 51.00001, 35.7, 4326);

        Assert.Empty(await CheckAsync(0.5, 4326, a, b));
        Assert.Single(await CheckAsync(1.0, 4326, a, b));
    }

    [Fact]
    public async Task TheIdentityFollowsTheGroup_NotTheInputOrder()
    {
        var first = Assert.Single(await CheckAsync(0.5, 0, PointFeature(1, 0, 0), PointFeature(2, 0.1, 0)));
        var second = Assert.Single(await CheckAsync(0.5, 0, PointFeature(2, 0.1, 0), PointFeature(1, 0, 0)));

        Assert.Equal(first.IdentityHash, second.IdentityHash);
    }
}

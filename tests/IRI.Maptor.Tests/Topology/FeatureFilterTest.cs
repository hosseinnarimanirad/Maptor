using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Core.Topology.RuleTypes;

using Xunit;

using static IRI.Maptor.Tests.Topology.TopologyTestKit;

namespace IRI.Maptor.Tests.Topology;

public class FeatureFilterTest
{
    private static Feature<Point> Line(int id, string wkt, string kind)
        => new(Geometry<Point>.FromWkt(wkt, 0), new Dictionary<string, object> { ["kind"] = kind }) { Id = id };

    private static readonly FeatureFilter Overhead = new("overhead", f => FeatureFilter.TextOf(f, "kind") == "overhead", "kind");

    [Fact]
    public async Task Filter_LimitsTheFeaturesARuleChecks()
    {
        var lines = new[] { Line(1, "LINESTRING(0 0, 100 0)", "overhead"), Line(2, "LINESTRING(0 50, 100 50)", "cable") };
        var points = new[] { PointFeature(1, 0, 0), PointFeature(2, 100, 0) };

        var ruleSet = new TopologyRuleSet("t", 1,
            new[] { new TopologyLayer("Line", GeometryType.LineString), new TopologyLayer("Pole", GeometryType.Point) },
            new[]
            {
                new TopologyRule("all", LineCarriesPointsRuleType.TypeId, "Line", "Pole"),
                new TopologyRule("overhead", LineCarriesPointsRuleType.TypeId, "Line", "Pole", filterA: Overhead),
            });

        var result = await new ValidationRunner(StandardRuleTypes.CreateCatalog())
            .RunAsync(ruleSet, TopologyCheckScope.WholeData, new[] { new FeatureListSource("Line", 0, lines), new FeatureListSource("Pole", 0, points) });

        // The cable line carries no poles: reported by the unfiltered rule only.
        Assert.Equal(new[] { "all" }, result.Issues.Select(i => i.RuleId).ToArray());
    }

    [Fact]
    public void Filter_SurvivesRuleCopies()
    {
        var rule = new TopologyRule("r", LineCarriesPointsRuleType.TypeId, "Line", "Pole", filterA: Overhead);

        Assert.Same(Overhead, rule.WithEnabled(false).FilterA);
        Assert.Same(Overhead, rule.FilterOf("Line"));
        Assert.Null(rule.FilterOf("Pole"));
    }
}

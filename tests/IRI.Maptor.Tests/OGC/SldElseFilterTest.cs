using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Ogc;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.OGC;

// 2026-10-22: the renderer ignored ElseFilter, so an "else" rule (no Filter) drew every feature a second
// time. SLD 1.0: an else rule applies to the features no other rule of the same style applies to.
public class SldElseFilterTest
{
    private static Rule ValueRule(string field, string value) => new()
    {
        Name = value,
        Filter = new OgcFilter { Predicate = new OgcPropertyIsEqualTo { Expressions = { new OgcPropertyName { Value = field }, new OgcLiteral { Value = value } } } },
        Symbolizers = { Line("#ff0000") },
    };

    private static LineSymbolizer Line(string color) => new()
    {
        Stroke = new Stroke { CssParameters = { new CssParameter { Name = SldHelper.CssParameter_Stroke, Value = color } } },
    };

    private static Feature<Point> F(object? material) => new(null!, new Dictionary<string, object> { ["Material"] = material! });

    [Fact]
    public void ElseRule_PassesOnlyFeaturesNoOtherRuleMatches()
    {
        var style = new FeatureTypeStyle
        {
            Rules =
            {
                ValueRule("Material", "PE"),
                ValueRule("Material", "Steel"),
                new Rule { Name = "other", ElseFilter = new ElseFilter(), Symbolizers = { Line("#999999") } },
            },
        };

        var symbolizers = style.Parse();
        var pe = symbolizers[0];
        var steel = symbolizers[1];
        var other = symbolizers[2];

        Assert.True(pe.IsFilterPassed(F("PE")));
        Assert.False(pe.IsFilterPassed(F("Copper")));

        Assert.False(other.IsFilterPassed(F("PE")));
        Assert.False(other.IsFilterPassed(F("Steel")));
        Assert.True(other.IsFilterPassed(F("Copper")));
        Assert.True(other.IsFilterPassed(F(null)));

        // every feature is drawn by exactly one rule
        foreach (var feature in new[] { F("PE"), F("Steel"), F("Copper"), F(null) })
            Assert.Equal(1, symbolizers.Count(s => s.IsFilterPassed(feature)));
    }

    [Fact]
    public void ElseRule_NextToARuleWithoutFilter_MatchesNothing()
    {
        var style = new FeatureTypeStyle
        {
            Rules =
            {
                new Rule { Name = "all", Symbolizers = { Line("#000000") } },
                new Rule { Name = "other", ElseFilter = new ElseFilter(), Symbolizers = { Line("#999999") } },
            },
        };

        var symbolizers = style.Parse();

        Assert.True(symbolizers[0].IsFilterPassed(F("anything")));
        Assert.False(symbolizers[1].IsFilterPassed(F("anything")));
    }

    [Fact]
    public void RulesWithoutElse_AreUnchanged()
    {
        var style = new FeatureTypeStyle { Rules = { ValueRule("Material", "PE"), new Rule { Name = "all", Symbolizers = { Line("#000000") } } } };

        var symbolizers = style.Parse();

        Assert.False(symbolizers[0].IsFilterPassed(F("Steel")));
        Assert.True(symbolizers[1].IsFilterPassed(F("Steel")));
    }
}

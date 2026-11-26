using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

using Xunit;

using static IRI.Maptor.Tests.Topology.TopologyTestKit;

namespace IRI.Maptor.Tests.Topology;

public class IssueIdentityTest
{
    [Fact]
    public void CanonicalLine_SortsFeaturesByLayerThenId()
    {
        var finding = Finding("near", "end", null, ("Tower", 3), ("Line", 7), ("Tower", 1));

        Assert.Equal("rule|near|Line:7;Tower:1;Tower:3|end", IssueIdentity.CanonicalLine(finding));
    }

    [Fact]
    public void FeatureOrder_DoesNotChangeTheIdentity()
    {
        var first = Finding("k", null, null, ("A", 1), ("B", 2));
        var second = Finding("k", null, null, ("B", 2), ("A", 1));

        Assert.Equal(IssueIdentity.Hash(first), IssueIdentity.Hash(second));
    }

    [Fact]
    public void ValueAndLocation_AreNotPartOfTheIdentity()
    {
        Assert.Equal(IssueIdentity.Hash(Finding("k", null, 30.0)), IssueIdentity.Hash(Finding("k", null, 2.0)));
    }

    [Fact]
    public void KindAndDiscriminator_ArePartOfTheIdentity()
    {
        var baseHash = IssueIdentity.Hash(Finding("k"));

        Assert.NotEqual(baseHash, IssueIdentity.Hash(Finding("other")));
        Assert.NotEqual(baseHash, IssueIdentity.Hash(Finding("k", "start")));
    }

    [Fact]
    public void Hash_Is64LowerCaseHexDigits()
    {
        var hash = IssueIdentity.Hash(Finding());

        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }
}

public class CheckComparerTest
{
    [Fact]
    public void Compare_SplitsNewStillThereAndGone()
    {
        var a = Issue(Finding("a"));
        var b = Issue(Finding("b"));
        var c = Issue(Finding("c"));

        var comparison = CheckComparer.Compare(new[] { a, b }, new[] { b, c });

        Assert.Equal(new[] { c }, comparison.New);
        Assert.Equal(new[] { b }, comparison.StillThere);
        Assert.Equal(new[] { a }, comparison.Gone);
        Assert.Equal(CheckChange.New, comparison.ChangeOf(c));
        Assert.Equal(CheckChange.StillThere, comparison.ChangeOf(b));
    }

    [Fact]
    public void Compare_UsesIdentity_NotTheObject()
    {
        var before = Issue(Finding("k", null, 30.0));
        var after = Issue(Finding("k", null, 2.0));

        var comparison = CheckComparer.Compare(new[] { before }, new[] { after });

        Assert.Empty(comparison.New);
        Assert.Empty(comparison.Gone);
    }
}

public class TopologyRuleTest
{
    private static TopologyRule Rule() => new TopologyRule("r", "everyFeature", "P");

    [Fact]
    public void SeverityOf_UsesTheRuleSettingOrTheDefault()
    {
        var rule = new TopologyRule("r", "t", "P", severities: new Dictionary<string, IssueSeverity> { ["seen"] = IssueSeverity.Error });

        Assert.Equal(IssueSeverity.Error, rule.SeverityOf("seen", IssueSeverity.Warning));
        Assert.Equal(IssueSeverity.Info, rule.SeverityOf("other", IssueSeverity.Info));
    }

    [Fact]
    public void WithSeverity_AlsoUnhidesTheKind()
    {
        var rule = Rule().WithHidden("seen", true);

        Assert.True(rule.IsHidden("seen"));

        var shown = rule.WithSeverity("seen", IssueSeverity.Error);

        Assert.False(shown.IsHidden("seen"));
        Assert.Equal(IssueSeverity.Error, shown.SeverityOf("seen", IssueSeverity.Info));
        Assert.True(rule.IsHidden("seen"));
    }

    [Fact]
    public void With_MethodsLeaveTheOriginalUnchanged()
    {
        var rule = Rule();

        var disabled = rule.WithEnabled(false);
        var tuned = rule.WithParameters(rule.Parameters.With("distance", 5));

        Assert.True(rule.Enabled);
        Assert.False(disabled.Enabled);
        Assert.Equal(5, tuned.Parameters.GetNumber("distance", 0));
        Assert.False(rule.Parameters.Has("distance"));
    }

    [Fact]
    public void Involves_LayerAOrB()
    {
        var rule = new TopologyRule("r", "pair", "P", "Q");

        Assert.True(rule.Involves("P"));
        Assert.True(rule.Involves("Q"));
        Assert.False(rule.Involves("R"));
    }

    [Fact]
    public void Parameters_ReadNumbersTextsAndLists()
    {
        var parameters = new RuleParameters(new Dictionary<string, object>
        {
            ["distance"] = 0.5,
            ["count"] = 2,
            ["field"] = "tower_num",
            ["layers"] = new[] { "Tower", "Substation" },
        });

        Assert.Equal(0.5, parameters.GetNumber("distance", 0));
        Assert.Equal(2, parameters.GetNumber("count", 0));
        Assert.Equal(7, parameters.GetNumber("missing", 7));
        Assert.Equal("tower_num", parameters.GetText("field"));
        Assert.Equal(new[] { "Tower", "Substation" }, parameters.GetTexts("layers"));
        Assert.Equal(new[] { "tower_num" }, parameters.GetTexts("field"));
        Assert.Empty(parameters.GetTexts("missing"));
    }
}

public class RuleSetValidatorTest
{
    private static readonly TopologyRuleCatalog Catalog = new(new ITopologyRuleType[] { new EveryFeatureRuleType(), new PairRuleType() });

    [Fact]
    public void ValidRuleSet_HasNoProblems()
    {
        var ruleSet = RuleSet(new TopologyRule("a", "everyFeature", "P"), new TopologyRule("b", "pair", "P", "Q"));

        Assert.Empty(RuleSetValidator.Validate(ruleSet, Catalog));
    }

    [Theory]
    [InlineData("unknownType")]
    [InlineData("unknownLayer")]
    [InlineData("missingB")]
    [InlineData("needlessB")]
    [InlineData("unknownKind")]
    [InlineData("duplicateId")]
    public void Mistakes_AreReported(string mistake)
    {
        var rules = mistake switch
        {
            "unknownType" => new[] { new TopologyRule("a", "nope", "P") },
            "unknownLayer" => new[] { new TopologyRule("a", "everyFeature", "Z") },
            "missingB" => new[] { new TopologyRule("a", "pair", "P") },
            "needlessB" => new[] { new TopologyRule("a", "everyFeature", "P", "Q") },
            "unknownKind" => new[] { new TopologyRule("a", "everyFeature", "P", hiddenKinds: new[] { "nope" }) },
            _ => new[] { new TopologyRule("a", "everyFeature", "P"), new TopologyRule("a", "everyFeature", "Q") },
        };

        Assert.Single(RuleSetValidator.Validate(RuleSet(rules), Catalog));
    }

    [Fact]
    public void Catalog_RefusesTheSameIdTwice()
    {
        var catalog = new TopologyRuleCatalog();

        catalog.Register(new EveryFeatureRuleType());

        Assert.Throws<ArgumentException>(() => catalog.Register(new EveryFeatureRuleType()));
    }
}

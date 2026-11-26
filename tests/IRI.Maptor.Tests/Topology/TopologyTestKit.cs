using System.Collections.Generic;
using System.Globalization;
using System.Threading;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Tests.Topology;

/// <summary>Synthetic features and fake rule types for testing the engine without any real rule.</summary>
internal static class TopologyTestKit
{
    public static Feature<Point> PointFeature(int id, double x, double y, int srid = 0)
    {
        var wkt = string.Format(CultureInfo.InvariantCulture, "POINT({0} {1})", x, y);

        return new Feature<Point>(Geometry<Point>.FromWkt(wkt, srid)) { Id = id };
    }

    public static TopologyLayer PointLayer(string name) => new TopologyLayer(name, GeometryType.Point);

    public static TopologyRuleSet RuleSet(params TopologyRule[] rules)
        => new TopologyRuleSet("test", 1, new[] { PointLayer("P"), PointLayer("Q") }, rules);

    public static TopologyFinding Finding(string kind = "k", string? discriminator = null, double? value = null, params (string Layer, long Id)[] features)
        => new TopologyFinding(
            "rule",
            kind,
            IssueSeverity.Warning,
            features.Length == 0 ? new[] { new FindingFeature("P", 1, FeatureRole.A) } : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(features, f => new FindingFeature(f.Layer, f.Id, FeatureRole.A))),
            value: value,
            discriminator: discriminator);

    public static TopologyIssue Issue(TopologyFinding finding) => new TopologyIssue(finding, IssueIdentity.Hash(finding), finding.Severity);
}

/// <summary>Reports every feature of layer A as "seen", and features with an even id also as "even".</summary>
internal sealed class EveryFeatureRuleType : TopologyRuleTypeBase
{
    private readonly double _reach;

    public EveryFeatureRuleType(string id = "everyFeature", double reach = 0)
        : base(id, false, ("seen", IssueSeverity.Warning), ("even", IssueSeverity.Info))
    {
        _reach = reach;
    }

    public int Calls { get; private set; }

    public override double ReachOf(TopologyRule rule) => _reach;

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        Calls++;

        foreach (var feature in context.FeaturesA)
        {
            yield return context.Finding("seen", new[] { context.FeatureA(feature) }, feature.TheGeometry);

            if (feature.Id % 2 == 0)
                yield return context.Finding("even", new[] { context.FeatureA(feature) }, feature.TheGeometry);
        }
    }
}

/// <summary>Reports each pair of an A and a B feature; builds one shared object per check.</summary>
internal sealed class PairRuleType : TopologyRuleTypeBase
{
    public PairRuleType() : base("pair", true, ("pair", IssueSeverity.Error))
    {
    }

    public int SharedBuilds { get; private set; }

    public override IReadOnlyCollection<string> FieldsOf(TopologyRule rule, FeatureRole role)
        => role == FeatureRole.A ? new[] { "name" } : new[] { "code" };

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        context.GetShared("pairs", () =>
        {
            SharedBuilds++;
            return new object();
        });

        foreach (var a in context.FeaturesA)
        {
            foreach (var b in context.FeaturesB)
                yield return context.Finding("pair", new[] { context.FeatureA(a), context.FeatureB(b) });
        }
    }
}

/// <summary>Reports the same finding twice.</summary>
internal sealed class TwiceRuleType : TopologyRuleTypeBase
{
    public TwiceRuleType() : base("twice", false, ("dup", IssueSeverity.Warning))
    {
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        foreach (var feature in context.FeaturesA)
        {
            yield return context.Finding("dup", new[] { context.FeatureA(feature) });
            yield return context.Finding("dup", new[] { context.FeatureA(feature) }, value: 99);
        }
    }
}

/// <summary>An <see cref="System.IProgress{T}"/> that records reports at once, on the calling thread.</summary>
internal sealed class RecordingProgress<T> : System.IProgress<T>
{
    public List<T> Reports { get; } = new();

    public void Report(T value) => Reports.Add(value);
}

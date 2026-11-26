using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R01, "points must not coincide": two or more points of layer A within <c>distance</c> of each other
/// (default: the rule set's same-place distance). One finding per group, not per pair, with every point of
/// the group. Kinds: <c>samePlace</c> when the whole group is within the same-place distance; <c>close</c>
/// when it is wider; <c>chain</c> when it is more than twice the distance across, because the points joined
/// through each other and a person must decide which really belong together.
/// </summary>
public sealed class DistinctPointsRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "distinctPoints";

    public const string SamePlace = "samePlace";
    public const string Close = "close";
    public const string Chain = "chain";

    public DistinctPointsRuleType()
        : base(TypeId, false, (SamePlace, IssueSeverity.Warning), (Close, IssueSeverity.Warning), (Chain, IssueSeverity.Error))
    {
    }

    public override double ReachOf(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, 0);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, 0) < 0 ? new[] { "its distance must not be negative." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var distance = context.Rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, context.SamePlace);

        var points = context.FeaturesA;
        var positions = points.Select(PointLineBuild.PointOf).ToList();

        foreach (var cluster in PositionClusterer.Cluster(positions, distance, context.IsGeodetic))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var members = cluster.Members.Select(i => points[i]).OrderBy(f => f.Id).ToList();

            var kind = cluster.IsChain ? Chain : cluster.Diameter <= context.SamePlace ? SamePlace : Close;

            yield return context.Finding(
                kind,
                members.Select(context.FeatureA).ToList(),
                members[0].TheGeometry,
                cluster.Diameter,
                ValueUnit.Metres);
        }
    }
}

using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Validation;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R00, "geometry must be valid": runs <see cref="GeometryValidator"/> on layer A and reports each problem it
/// finds, one finding per problem. The kinds are the <see cref="GeometryIssueKind"/> names. A problem the
/// validator always fixes by itself (a ring wound the wrong way) is information; every other problem is an
/// error.
/// </summary>
public sealed class GeometryValidRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "geometryValid";

    public GeometryValidRuleType()
        : base(TypeId, false, Enum.GetValues(typeof(GeometryIssueKind))
            .Cast<GeometryIssueKind>()
            .Select(k => (k.ToString(), k.IsFixedAutomatically() ? IssueSeverity.Info : IssueSeverity.Error))
            .ToArray())
    {
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var report = GeometryValidator.Validate(context.FeaturesA);

        foreach (var issue in report.Issues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var geometry = issue.Feature.TheGeometry;

            yield return context.Finding(
                issue.Kind.ToString(),
                new[] { context.FeatureA(issue.Feature) },
                geometry is null || geometry.IsNullOrEmpty() ? null : geometry,
                discriminator: Discriminator(issue.PartIndex, issue.RingIndex));
        }
    }

    /// <summary>Tells apart two problems of one kind in different parts or rings of one feature.</summary>
    private static string? Discriminator(int? partIndex, int? ringIndex)
        => partIndex is null && ringIndex is null ? null : $"part {partIndex?.ToString() ?? "-"} ring {ringIndex?.ToString() ?? "-"}";
}

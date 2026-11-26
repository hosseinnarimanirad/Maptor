using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R13, "point should be on one line": a point of layer A within <c>distance</c> (default 0.5 m) of two or
/// more lines of layer B. Usually correct (a junction), so it is information by default; an app may hide it.
/// </summary>
public sealed class PointOnOneLineRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "pointOnOneLine";

    public const string OnSeveralLines = "onSeveralLines";

    public PointOnOneLineRuleType()
        : base(TypeId, true, (OnSeveralLines, IssueSeverity.Info))
    {
    }

    public override double ReachOf(TopologyRule rule) => Distance(rule);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => Distance(rule) <= 0 ? new[] { "its distance must be positive." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var build = PointLineBuild.Get(context, pointsAreA: true, Distance(context.Rule));

        foreach (var issue in build.Topology.Issues.Where(i => i.Code == PointLineIssueCode.SharedPoint && i.PointIndex is not null))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = issue.PointIndex!.Value;
            var point = build.Points[index];
            var lines = build.Topology.GetLineIndices(index);

            var features = new[] { context.FeatureA(point) }
                .Concat(lines.Select(l => context.FeatureB(build.Lines[l])))
                .ToList();

            yield return context.Finding(OnSeveralLines, features, point.TheGeometry, lines.Count, ValueUnit.Count);
        }
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);
}

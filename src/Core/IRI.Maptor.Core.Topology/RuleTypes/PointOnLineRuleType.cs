using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R10, "point must be on line": a point of layer A farther than <c>distance</c> (default 0.5 m) from every
/// line of layer B. Kind <c>nearLine</c> when some line is within <c>nearDistance</c> (default 50 m), with
/// that line and the distance; <c>farFromLines</c> otherwise. <c>fixDistance</c> (default 5 m, D63) is how far the
/// "near a line" fix may move anything; farther issues are left to be edited by hand.
/// </summary>
public sealed class PointOnLineRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "pointOnLine";

    public const string NearLine = "nearLine";
    public const string FarFromLines = "farFromLines";

    public const string FixDistanceParameter = "fixDistance";
    public const double DefaultFixDistance = 5;

    public PointOnLineRuleType()
        : base(TypeId, true, (NearLine, IssueSeverity.Error), (FarFromLines, IssueSeverity.Error))
    {
    }

    public override double ReachOf(TopologyRule rule)
        => Math.Max(Distance(rule), NearDistance(rule));

    public override IReadOnlyList<string> Validate(TopologyRule rule)
    {
        var problems = new List<string>();

        if (Distance(rule) <= 0)
            problems.Add("its distance must be positive.");

        if (NearDistance(rule) < Distance(rule))
            problems.Add("its near distance must not be smaller than its distance.");

        if (FixDistance(rule) < 0)
            problems.Add("its fix distance must not be negative.");

        return problems;
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var distance = Distance(context.Rule);
        var nearDistance = NearDistance(context.Rule);

        var build = PointLineBuild.Get(context, pointsAreA: true, distance);
        var near = nearDistance > distance ? PointLineBuild.Get(context, pointsAreA: true, nearDistance) : null;

        foreach (var issue in build.Topology.Issues.Where(i => i.Code == PointLineIssueCode.OrphanPoint && i.PointIndex is not null))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = issue.PointIndex!.Value;
            var point = build.Points[index];

            var nearest = near?.Topology.GetLinesOfPoint(index)
                .Select(line => (Line: line, Offset: line.Stations[line.IndexOf(index)].Offset))
                .OrderBy(s => s.Offset)
                .ThenBy(s => s.Line.LineIndex)
                .FirstOrDefault();

            if (nearest?.Line is not null)
            {
                yield return context.Finding(
                    NearLine,
                    new[] { context.FeatureA(point), context.FeatureB(build.Lines[nearest.Value.Line.LineIndex]) },
                    point.TheGeometry,
                    nearest.Value.Offset,
                    ValueUnit.Metres);
            }
            else
            {
                yield return context.Finding(FarFromLines, new[] { context.FeatureA(point) }, point.TheGeometry);
            }
        }
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);

    private static double NearDistance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.NearDistanceParameter, PointLineBuild.DefaultNearDistance);

    /// <summary>How far the "near a line" fix may move anything, in metres.</summary>
    public static double FixDistance(TopologyRule rule) => rule.Parameters.GetNumber(FixDistanceParameter, DefaultFixDistance);
}

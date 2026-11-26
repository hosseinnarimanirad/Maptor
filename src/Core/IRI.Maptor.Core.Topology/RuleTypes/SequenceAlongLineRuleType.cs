using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R14, "sequence along line": the numbers in field <c>sequenceField</c> of the points of layer A must rise
/// along each line of layer B they stand on. The order is geometric; the numbers only choose the direction
/// and are checked against it. A point on two lines (a junction) is not checked, because it carries one
/// line's number only.
/// </summary>
public sealed class SequenceAlongLineRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "sequenceAlongLine";

    public const string Missing = "missing";
    public const string Duplicate = "duplicate";
    public const string OutOfOrder = "outOfOrder";
    public const string Gap = "gap";
    public const string DirectionUndecided = "directionUndecided";

    public SequenceAlongLineRuleType()
        : base(TypeId, true,
            (Missing, IssueSeverity.Warning),
            (Duplicate, IssueSeverity.Error),
            (OutOfOrder, IssueSeverity.Error),
            (Gap, IssueSeverity.Info),
            (DirectionUndecided, IssueSeverity.Warning))
    {
    }

    public override IReadOnlyCollection<string> FieldsOf(TopologyRule rule, FeatureRole role)
        => role == FeatureRole.A && SequenceField(rule) is string field ? new[] { field } : Array.Empty<string>();

    public override double ReachOf(TopologyRule rule) => Distance(rule);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
    {
        var problems = new List<string>();

        if (SequenceField(rule) is null)
            problems.Add("it needs the name of the sequence field.");

        if (Distance(rule) <= 0)
            problems.Add("its distance must be positive.");

        return problems;
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var build = PointLineBuild.Get(context, pointsAreA: true, Distance(context.Rule));

        foreach (var issue in build.Topology.Issues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = KindOf(issue.Code);

            if (kind is null || issue.LineIndex is null)
                continue;

            var line = build.Lines[issue.LineIndex.Value];

            if (kind == DirectionUndecided)
            {
                yield return context.Finding(kind, new[] { context.FeatureB(line) }, line.TheGeometry);
                continue;
            }

            if (issue.PointIndex is null)
                continue;

            var point = build.Points[issue.PointIndex.Value];

            var features = new List<FindingFeature> { context.FeatureA(point) };

            if (issue.OtherPointIndex is int other)
                features.Add(context.FeatureA(build.Points[other]));

            features.Add(context.FeatureB(line));

            yield return context.Finding(
                kind,
                features,
                point.TheGeometry,
                issue.Value,
                kind == Gap ? ValueUnit.Count : ValueUnit.None);
        }
    }

    private static string? KindOf(PointLineIssueCode code) => code switch
    {
        PointLineIssueCode.MissingSequence => Missing,
        PointLineIssueCode.DuplicateSequence => Duplicate,
        PointLineIssueCode.SequenceOutOfOrder => OutOfOrder,
        PointLineIssueCode.SequenceGap => Gap,
        PointLineIssueCode.DirectionUndecided => DirectionUndecided,
        _ => null,
    };

    private static string? SequenceField(TopologyRule rule)
        => rule.Parameters.GetText(PointLineBuild.SequenceFieldParameter) is { Length: > 0 } field && !string.IsNullOrWhiteSpace(field) ? field : null;

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);
}

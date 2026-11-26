using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R11, "line end must be covered by point": an end of a part of a line of layer A with no point of layer B
/// within <c>distance</c> of it along the line (default 0.5 m). Kinds <c>start</c> and <c>end</c>, in the
/// direction the line was drawn. Where two parts of one line meet, the ends are not checked unless
/// <c>interiorPartEnds</c> is 1.
/// </summary>
public sealed class LineEndCoveredRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "lineEndCovered";

    public const string Start = "start";
    public const string End = "end";

    public const string InteriorPartEndsParameter = "interiorPartEnds";

    public LineEndCoveredRuleType()
        : base(TypeId, true, (Start, IssueSeverity.Warning), (End, IssueSeverity.Warning))
    {
    }

    public override double ReachOf(TopologyRule rule) => Distance(rule);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => Distance(rule) <= 0 ? new[] { "its distance must be positive." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var distance = Distance(context.Rule);
        var checkInterior = context.Rule.Parameters.GetNumber(InteriorPartEndsParameter, 0) != 0;

        var build = PointLineBuild.Get(context, pointsAreA: false, distance);

        foreach (var issue in build.Topology.Issues.Where(i => i.Code == PointLineIssueCode.OpenLineEnd && i.LineIndex is not null && i.Location is not null))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = build.Lines[issue.LineIndex!.Value];
            var vertex = issue.Location!;
            var part = issue.PartIndex ?? 0;

            if (!checkInterior && MeetsAnotherPart(line.TheGeometry, part, vertex, distance, context.IsGeodetic))
                continue;

            var kind = (issue.Value ?? 0) == 0 ? Start : End;

            yield return context.Finding(
                kind,
                new[] { context.FeatureA(line) },
                Geometry<Point>.Create(vertex.X, vertex.Y, line.TheGeometry?.Srid ?? 0),
                discriminator: $"part {part}");
        }
    }

    /// <summary>True when the vertex lies within the distance of an end of another part of the same line.</summary>
    private static bool MeetsAnotherPart(Geometry<Point>? geometry, int partIndex, Point vertex, double distance, bool isGeodetic)
    {
        if (geometry?.Geometries is not { Count: > 1 } parts)
            return false;

        var frame = LocalMetricFrame.At(vertex, isGeodetic);

        for (int p = 0; p < parts.Count; p++)
        {
            if (p == partIndex || parts[p]?.Points is not { Count: > 0 } points)
                continue;

            foreach (var end in new[] { points[0], points[points.Count - 1] })
            {
                var (east, north) = frame.ToMetres(end);

                if (Math.Sqrt(east * east + north * north) <= distance)
                    return true;
            }
        }

        return false;
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);
}

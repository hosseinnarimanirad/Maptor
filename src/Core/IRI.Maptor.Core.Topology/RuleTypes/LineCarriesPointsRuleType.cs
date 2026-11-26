using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R12, "line must carry points": a usable line of layer A with fewer than <c>minimumPoints</c> points of
/// layer B on it (default 2), a point being on the line when it is within <c>distance</c> (default 0.5 m).
/// </summary>
public sealed class LineCarriesPointsRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "lineCarriesPoints";

    public const string TooFewPoints = "tooFewPoints";

    public const string MinimumPointsParameter = "minimumPoints";

    public LineCarriesPointsRuleType()
        : base(TypeId, true, (TooFewPoints, IssueSeverity.Warning))
    {
    }

    public override double ReachOf(TopologyRule rule) => Distance(rule);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
    {
        var problems = new List<string>();

        if (Distance(rule) <= 0)
            problems.Add("its distance must be positive.");

        if (MinimumPoints(rule) < 1)
            problems.Add("its minimum number of points must be at least 1.");

        return problems;
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var minimum = MinimumPoints(context.Rule);

        var build = PointLineBuild.Get(context, pointsAreA: false, Distance(context.Rule));

        foreach (var line in build.Topology.Lines.Where(l => l.IsValid && l.Stations.Count < minimum))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var feature = build.Lines[line.LineIndex];

            yield return context.Finding(TooFewPoints, new[] { context.FeatureA(feature) }, feature.TheGeometry, line.Stations.Count, ValueUnit.Count);
        }
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);

    private static int MinimumPoints(TopologyRule rule) => (int)rule.Parameters.GetNumber(MinimumPointsParameter, 2);
}

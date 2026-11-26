using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R23, "lines must not touch and return": two lines that cross twice within <c>distance</c> (0.5 m) of each
/// other along both, which is usually drawing noise rather than two real crossings. Crossings of a pair are taken
/// in order along the first line and paired; a crossing left on its own is a real crossing and is not reported.
/// The lines are those of layer A, or layer A against <c>otherLayer</c> when it is given. Information only.
/// </summary>
public sealed class TouchAndReturnRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "touchAndReturn";

    public const string TouchAndReturn = "touchAndReturn";

    public const string OtherLayerParameter = "otherLayer";

    public TouchAndReturnRuleType()
        : base(TypeId, false, (TouchAndReturn, IssueSeverity.Info))
    {
    }

    public override IReadOnlyCollection<string> ExtraLayersOf(TopologyRule rule)
        => rule.Parameters.GetText(OtherLayerParameter) is string other && other.Length > 0 && other != rule.LayerA ? new[] { other } : Array.Empty<string>();

    public override double ReachOf(TopologyRule rule) => Distance(rule);

    public override IReadOnlyList<string> Validate(TopologyRule rule)
        => Distance(rule) <= 0 ? new[] { "its distance must be positive." } : Array.Empty<string>();

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var rule = context.Rule;
        var distance = Distance(rule);

        var own = GeometryPreparer.Lines(context, rule.LayerA);
        var otherLayerName = ExtraLayersOf(rule).FirstOrDefault();
        var other = otherLayerName is null ? null : GeometryPreparer.Lines(context, otherLayerName);

        var crossings = other is null
            ? SegmentCrossings.Find(own.Lines, null, context.SamePlace)
            : SegmentCrossings.Find(own.Lines, other.Lines, context.SamePlace);

        foreach (var pair in crossings.GroupBy(c => (c.LineIndex, c.OtherLineIndex)).OrderBy(g => g.Key.LineIndex).ThenBy(g => g.Key.OtherLineIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ordered = pair.OrderBy(c => c.PartIndex).ThenBy(c => c.Measure).ToList();

            var ordinal = 0;

            for (int i = 0; i + 1 < ordered.Count; i++)
            {
                var first = ordered[i];
                var second = ordered[i + 1];

                if (first.PartIndex != second.PartIndex || first.OtherPartIndex != second.OtherPartIndex)
                    continue;

                var along = second.Measure - first.Measure;
                var alongOther = Math.Abs(second.OtherMeasure - first.OtherMeasure);

                if (along > distance || alongOther > distance)
                    continue;

                var lineFeature = own.Features[pair.Key.LineIndex];
                var otherFeature = (other ?? own).Features[pair.Key.OtherLineIndex];

                var features = new[]
                {
                    context.FeatureA(lineFeature),
                    other is null ? context.FeatureA(otherFeature) : context.FeatureOf(otherLayerName!, otherFeature),
                };

                var middle = Geometry<Point>.Create((first.Location.X + second.Location.X) / 2, (first.Location.Y + second.Location.Y) / 2, own.Lines[pair.Key.LineIndex].Srid);

                yield return context.Finding(TouchAndReturn, features, middle, along, ValueUnit.Metres, $"pair {ordinal++}");

                i++;
            }
        }
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);
}

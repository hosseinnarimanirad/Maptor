using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.RuleTypes;

/// <summary>
/// R21, "parts of a line must join": a multi-part line of layer A is expected to be one path, its parts meeting
/// end to end within <c>distance</c> (0.5 m). Kinds:
/// <list type="bullet">
/// <item><c>gap</c>: two parts whose nearest ends are farther apart than the distance but within <c>maxGap</c>
/// (50 m); one finding per gap that would join the line up.</item>
/// <item><c>detachedPart</c>: a part (or group of joined parts) farther than <c>maxGap</c> from all the others.</item>
/// <item><c>branch</c>: three or more part ends meet at one place, so the line is not a single path.</item>
/// <item><c>outOfOrder</c>: the parts form one path, but not in the order they are stored.</item>
/// <item><c>reversedPart</c>: along that path, a part is drawn against the others.</item>
/// </list>
/// Single-part lines are never reported.
/// </summary>
public sealed class PartsJoinRuleType : TopologyRuleTypeBase
{
    public const string TypeId = "partsJoin";

    public const string Gap = "gap";
    public const string DetachedPart = "detachedPart";
    public const string Branch = "branch";
    public const string OutOfOrder = "outOfOrder";
    public const string ReversedPart = "reversedPart";

    public const string MaxGapParameter = "maxGap";

    public PartsJoinRuleType()
        : base(TypeId, false,
            (Gap, IssueSeverity.Warning),
            (DetachedPart, IssueSeverity.Error),
            (Branch, IssueSeverity.Warning),
            (OutOfOrder, IssueSeverity.Info),
            (ReversedPart, IssueSeverity.Info))
    {
    }

    public override IReadOnlyList<string> Validate(TopologyRule rule)
    {
        var problems = new List<string>();

        if (Distance(rule) <= 0)
            problems.Add("its distance must be positive.");

        if (MaxGap(rule) < Distance(rule))
            problems.Add("its largest gap must not be smaller than its distance.");

        return problems;
    }

    public override IEnumerable<TopologyFinding> Check(TopologyRuleContext context, CancellationToken cancellationToken)
    {
        var distance = Distance(context.Rule);
        var maxGap = MaxGap(context.Rule);

        var layer = GeometryPreparer.Lines(context, context.Rule.LayerA);

        for (int l = 0; l < layer.Count; l++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = layer.Lines[l];

            if (line.Parts.Count < 2)
                continue;

            foreach (var finding in CheckLine(context, layer.Features[l], line, distance, maxGap))
                yield return finding;
        }
    }

    private static IEnumerable<TopologyFinding> CheckLine(TopologyRuleContext context, Feature<Point> feature, MeasuredLine<Point> line, double distance, double maxGap)
    {
        var isGeodetic = context.IsGeodetic;
        var parts = line.Parts;
        var srid = line.Srid;
        var reference = new[] { context.FeatureA(feature) };

        // Ends: 2p = start of part p, 2p + 1 = its end.
        var ends = parts.SelectMany(p => new[] { p.Vertices[0], p.Vertices[p.Vertices.Count - 1] }).ToList();

        double Between(int a, int b)
        {
            var (east, north) = LocalMetricFrame.At(ends[a], isGeodetic).ToMetres(ends[b]);
            return Math.Sqrt(east * east + north * north);
        }

        var hasBranch = false;

        foreach (var cluster in PositionClusterer.Cluster(ends.Select(e => (Point?)e).ToList(), distance, isGeodetic).Where(c => c.Members.Count >= 3))
        {
            hasBranch = true;

            var at = ends[cluster.Members[0]];

            yield return context.Finding(Branch, reference, Geometry<Point>.Create(at.X, at.Y, srid), cluster.Members.Count, ValueUnit.Count,
                $"ends {string.Join(",", cluster.Members.Select(m => $"{parts[m / 2].Index}{(m % 2 == 0 ? "s" : "e")}"))}");
        }

        // Parts joined within the distance.
        var group = Enumerable.Range(0, parts.Count).ToArray();

        int Root(int p)
        {
            while (group[p] != p)
                p = group[p] = group[group[p]];

            return p;
        }

        void Join(int a, int b)
        {
            var ra = Root(a);
            var rb = Root(b);

            if (ra != rb)
                group[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }

        var links = new List<(int EndA, int EndB, double Distance, bool IsGap)>();

        for (int a = 0; a < ends.Count; a++)
        {
            for (int b = a + 1; b < ends.Count; b++)
            {
                if (a / 2 == b / 2)
                    continue;

                var d = Between(a, b);

                if (d <= distance)
                {
                    Join(a / 2, b / 2);
                    links.Add((a, b, d, false));
                }
            }
        }

        // Groups of joined parts, and the nearest ends between each pair of groups.
        var groups = Enumerable.Range(0, parts.Count).GroupBy(Root).Select(g => g.ToList()).ToList();

        var hasDetached = false;

        if (groups.Count > 1)
        {
            var bridges = new List<(int GroupA, int GroupB, int EndA, int EndB, double Distance)>();

            for (int g = 0; g < groups.Count; g++)
            {
                for (int h = g + 1; h < groups.Count; h++)
                {
                    var best = groups[g].SelectMany(p => new[] { 2 * p, 2 * p + 1 })
                        .SelectMany(a => groups[h].SelectMany(p => new[] { 2 * p, 2 * p + 1 }).Select(b => (A: a, B: b, D: Between(a, b))))
                        .OrderBy(x => x.D)
                        .First();

                    bridges.Add((g, h, best.A, best.B, best.D));
                }
            }

            var detached = Enumerable.Range(0, groups.Count)
                .Where(g => bridges.Where(b => b.GroupA == g || b.GroupB == g).All(b => b.Distance > maxGap))
                .ToHashSet();

            foreach (var g in detached)
            {
                hasDetached = true;

                foreach (var p in groups[g])
                {
                    var nearest = bridges.Where(b => b.GroupA == g || b.GroupB == g).Min(b => b.Distance);

                    yield return context.Finding(DetachedPart, reference, parts[p].Extract(0, parts[p].Length, srid), nearest, ValueUnit.Metres, $"part {parts[p].Index}");
                }
            }

            // The shortest bridges that join the other groups into one (Kruskal).
            var joined = Enumerable.Range(0, groups.Count).ToArray();

            int GroupRoot(int g)
            {
                while (joined[g] != g)
                    g = joined[g] = joined[joined[g]];

                return g;
            }

            foreach (var bridge in bridges.Where(b => !detached.Contains(b.GroupA) && !detached.Contains(b.GroupB) && b.Distance <= maxGap).OrderBy(b => b.Distance))
            {
                var ra = GroupRoot(bridge.GroupA);
                var rb = GroupRoot(bridge.GroupB);

                if (ra == rb)
                    continue;

                joined[Math.Max(ra, rb)] = Math.Min(ra, rb);

                links.Add((bridge.EndA, bridge.EndB, bridge.Distance, true));

                var (a, b) = (ends[bridge.EndA], ends[bridge.EndB]);

                var first = Math.Min(parts[bridge.EndA / 2].Index, parts[bridge.EndB / 2].Index);
                var second = Math.Max(parts[bridge.EndA / 2].Index, parts[bridge.EndB / 2].Index);

                yield return context.Finding(Gap, reference, Geometry<Point>.Create((a.X + b.X) / 2, (a.Y + b.Y) / 2, srid), bridge.Distance, ValueUnit.Metres, $"parts {first}-{second}");
            }
        }

        if (hasBranch || hasDetached)
            yield break;

        // Walk the path the links make, if they make one.
        var walk = Walk(parts.Count, links);

        if (walk is null)
            yield break;

        var order = walk.Select(w => w.Part).ToList();
        var stored = Enumerable.Range(0, parts.Count).ToList();

        if (!order.SequenceEqual(stored) && !order.SequenceEqual(Enumerable.Reverse(stored)))
            yield return context.Finding(OutOfOrder, reference, line.Geometry);

        foreach (var step in walk.Where(w => w.Reversed))
            yield return context.Finding(ReversedPart, reference, parts[step.Part].Extract(0, parts[step.Part].Length, srid), discriminator: $"part {parts[step.Part].Index}");
    }

    /// <summary>
    /// The parts in path order with the direction each is walked, or null when the links do not make one path
    /// (an end with two links, a loop, or parts left over). The walking direction is the one most parts are drawn
    /// in; on a tie, the one that meets the first stored part first.
    /// </summary>
    private static List<(int Part, bool Reversed)>? Walk(int partCount, List<(int EndA, int EndB, double Distance, bool IsGap)> links)
    {
        var linkOf = new Dictionary<int, int>();

        foreach (var (a, b, _, _) in links)
        {
            if (linkOf.ContainsKey(a) || linkOf.ContainsKey(b))
                return null;

            linkOf[a] = b;
            linkOf[b] = a;
        }

        var free = Enumerable.Range(0, 2 * partCount).Where(e => !linkOf.ContainsKey(e)).ToList();

        if (free.Count == 0)
            return null;

        var startEnd = free[0];

        var walk = new List<(int Part, bool Reversed)>();

        var enter = startEnd;

        while (true)
        {
            var part = enter / 2;
            var reversed = enter % 2 == 1;

            walk.Add((part, reversed));

            var leave = reversed ? 2 * part : 2 * part + 1;

            if (!linkOf.TryGetValue(leave, out enter))
                break;

            if (walk.Count > partCount)
                return null;
        }

        if (walk.Count != partCount)
            return null;

        var backward = Enumerable.Reverse(walk).Select(w => (w.Part, Reversed: !w.Reversed)).ToList();

        var forwardCount = walk.Count(w => !w.Reversed);
        var backwardCount = backward.Count(w => !w.Reversed);

        if (backwardCount > forwardCount || (backwardCount == forwardCount && backward.FindIndex(w => w.Part == 0) < walk.FindIndex(w => w.Part == 0)))
            return backward;

        return walk;
    }

    private static double Distance(TopologyRule rule) => rule.Parameters.GetNumber(PointLineBuild.DistanceParameter, PointLineBuild.DefaultDistance);

    private static double MaxGap(TopologyRule rule) => rule.Parameters.GetNumber(MaxGapParameter, 50);
}

using System.Diagnostics;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Topology.Checking;

/// <summary>
/// Runs one check: works out which rules and layers the scope needs, loads each layer once, runs the rules
/// in the rule set's order, applies the rule set's kind settings, and sorts the issues. It does no geometry
/// and stores nothing.
/// <list type="bullet">
/// <item>Hidden kinds are dropped; other kinds get the rule's severity, or the rule type's default.</item>
/// <item>An extent or feature scope also loads the neighbours within the rules' reach, so that findings at
/// the edge are right; findings located outside the extent, or not about a chosen feature, are dropped.</item>
/// <item>Two findings with the same identity are one issue; the first is kept.</item>
/// <item>Issues are sorted by rule (rule set order), kind (rule type order), then identity, so the same data
/// gives the same list.</item>
/// </list>
/// </summary>
public sealed class ValidationRunner
{
    private readonly TopologyRuleCatalog _catalog;

    public ValidationRunner(TopologyRuleCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <param name="ruleSet">The rules to check.</param>
    /// <param name="scope">What the check covers.</param>
    /// <param name="sources">One source per layer the rules use (others are ignored).</param>
    /// <param name="progress">Told which rule is in work; may be null.</param>
    /// <param name="cancellationToken">Checked between loads and between rules.</param>
    /// <exception cref="TopologyCheckException">The rule set is invalid, a layer has no source, or the sources' SRS cannot be measured in metres.</exception>
    public async Task<TopologyCheckResult> RunAsync(
        TopologyRuleSet ruleSet,
        TopologyCheckScope scope,
        IEnumerable<ITopologyFeatureSource> sources,
        IProgress<TopologyCheckProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (ruleSet is null)
            throw new ArgumentNullException(nameof(ruleSet));

        if (scope is null)
            throw new ArgumentNullException(nameof(scope));

        if (sources is null)
            throw new ArgumentNullException(nameof(sources));

        var startedAt = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();

        var problems = RuleSetValidator.Validate(ruleSet, _catalog);

        if (problems.Count > 0)
            throw new TopologyCheckException(problems);

        var rules = ruleSet.Rules
            .Where(r => r.Enabled)
            .Where(r => scope.Kind != CheckScopeKind.Layer || r.Involves(scope.EntityName!))
            .ToList();

        var layers = rules.SelectMany(r => r.LayerB is null ? new[] { r.LayerA } : new[] { r.LayerA, r.LayerB }).Distinct().ToList();

        if (scope.Kind == CheckScopeKind.Features && !layers.Contains(scope.EntityName!))
            layers.Add(scope.EntityName!);

        var sourceOf = ResolveSources(sources, layers);

        var srid = CommonSrid(sourceOf.Values);
        var isGeodetic = srid == SridHelper.GeodeticWGS84;

        var reach = rules.Select(r => _catalog.Find(r.RuleTypeId)!.ReachOf(r)).DefaultIfEmpty(0).Max();
        reach = Math.Max(reach, ruleSet.SamePlace);

        var loadBox = await LoadBoxAsync(scope, sourceOf, reach, isGeodetic, cancellationToken).ConfigureAwait(false);

        var featuresOf = new Dictionary<string, IReadOnlyList<Feature<Point>>>(StringComparer.Ordinal);

        if (scope.Kind != CheckScopeKind.Features || loadBox is not null)
        {
            foreach (var layer in layers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fields = FieldsOf(rules, layer);

                featuresOf[layer] = await sourceOf[layer].GetFeaturesAsync(loadBox, fields, cancellationToken).ConfigureAwait(false);
            }
        }

        var shared = new Dictionary<string, object>(StringComparer.Ordinal);
        var collected = new List<(int RuleOrder, int KindOrder, string Line, TopologyIssue Issue)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int r = 0; r < rules.Count && featuresOf.Count > 0; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rule = rules[r];
            var ruleType = _catalog.Find(rule.RuleTypeId)!;

            progress?.Report(new TopologyCheckProgress(r, rules.Count, rule.Id));

            var context = new TopologyRuleContext(
                ruleSet,
                rule,
                ruleType,
                featuresOf[rule.LayerA],
                rule.LayerB is null ? null : featuresOf[rule.LayerB],
                isGeodetic,
                shared);

            foreach (var finding in ruleType.Check(context, cancellationToken))
            {
                if (rule.IsHidden(finding.Kind) || !IsInScope(finding, scope))
                    continue;

                var line = IssueIdentity.CanonicalLine(finding);
                var hash = IssueIdentity.Hash(finding);

                if (!seen.Add(hash))
                    continue;

                var kindOrder = IndexOf(ruleType.Kinds, finding.Kind);

                collected.Add((r, kindOrder, line, new TopologyIssue(finding, hash, rule.SeverityOf(finding.Kind, finding.Severity))));
            }
        }

        var issues = collected
            .OrderBy(c => c.RuleOrder)
            .ThenBy(c => c.KindOrder)
            .ThenBy(c => c.Line, StringComparer.Ordinal)
            .Select(c => c.Issue)
            .ToList();

        clock.Stop();

        var summary = new TopologyCheckSummary(
            ruleSet.Key,
            ruleSet.Version,
            scope,
            startedAt,
            clock.Elapsed,
            rules.Select(r => r.Id).ToList(),
            featuresOf.ToDictionary(p => p.Key, p => p.Value.Count, StringComparer.Ordinal),
            issues);

        return new TopologyCheckResult(issues, summary);
    }

    private static Dictionary<string, ITopologyFeatureSource> ResolveSources(IEnumerable<ITopologyFeatureSource> sources, IReadOnlyList<string> layers)
    {
        var byName = new Dictionary<string, ITopologyFeatureSource>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            if (source is not null && !byName.ContainsKey(source.EntityName))
                byName[source.EntityName] = source;
        }

        var missing = layers.Where(l => !byName.ContainsKey(l)).ToList();

        if (missing.Count > 0)
            throw new TopologyCheckException($"No source was given for layer(s): {string.Join(", ", missing)}.");

        return layers.ToDictionary(l => l, l => byName[l], StringComparer.Ordinal);
    }

    private static int CommonSrid(IEnumerable<ITopologyFeatureSource> sources)
    {
        var srids = sources.Select(s => s.Srid).Distinct().ToList();

        if (srids.Count > 1)
            throw new TopologyCheckException($"The layers of one check must share an SRS; found {string.Join(", ", srids)}.");

        var srid = srids.Count == 0 ? SridHelper.GeodeticWGS84 : srids[0];

        if (srid == SridHelper.WebMercator || srid == SridHelper.EsriWebMercator)
            throw new TopologyCheckException("Web Mercator is not measured in ground metres; convert the features to geodetic WGS84 (4326) first.");

        return srid;
    }

    /// <summary>The area to load: null for the whole layers, the scope grown by the reach otherwise.</summary>
    private static async Task<BoundingBox?> LoadBoxAsync(
        TopologyCheckScope scope,
        IReadOnlyDictionary<string, ITopologyFeatureSource> sourceOf,
        double reach,
        bool isGeodetic,
        CancellationToken cancellationToken)
    {
        switch (scope.Kind)
        {
            case CheckScopeKind.Extent:
                return Grow(scope.Extent!.Value, reach, isGeodetic);

            case CheckScopeKind.Features:
                var chosen = await sourceOf[scope.EntityName!]
                    .GetFeaturesByIdsAsync(scope.FeatureIds, Array.Empty<string>(), cancellationToken)
                    .ConfigureAwait(false);

                var boxes = chosen
                    .Where(f => f.TheGeometry is not null && !f.TheGeometry.IsNullOrEmpty())
                    .Select(f => f.TheGeometry.GetBoundingBox())
                    .ToList();

                return boxes.Count == 0 ? null : Grow(BoundingBox.GetMergedBoundingBox(boxes), reach, isGeodetic);

            default:
                return null;
        }
    }

    private static BoundingBox Grow(BoundingBox box, double metres, bool isGeodetic)
    {
        // Measured at the bottom and the top edge and the larger taken: a degree of longitude is shortest at
        // the latitude farthest from the equator, so the growth is enough on every side.
        var (halfWidth, halfHeight) = new[] { box.BottomLeft, box.TopLeft }
            .Select(p => LocalMetricFrame.At(p, isGeodetic).SearchHalfSize(metres))
            .Aggregate((a, b) => (Math.Max(a.HalfWidth, b.HalfWidth), Math.Max(a.HalfHeight, b.HalfHeight)));

        return new BoundingBox(box.XMin - halfWidth, box.YMin - halfHeight, box.XMax + halfWidth, box.YMax + halfHeight);
    }

    /// <summary>Every attribute the rules read from <paramref name="layer"/>, on either side.</summary>
    private IReadOnlyCollection<string> FieldsOf(IEnumerable<TopologyRule> rules, string layer)
        => rules.SelectMany(rule =>
            {
                var ruleType = _catalog.Find(rule.RuleTypeId)!;

                var a = rule.LayerA == layer ? ruleType.FieldsOf(rule, FeatureRole.A) : Enumerable.Empty<string>();
                var b = rule.LayerB == layer ? ruleType.FieldsOf(rule, FeatureRole.B) : Enumerable.Empty<string>();

                return a.Concat(b);
            })
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static bool IsInScope(TopologyFinding finding, TopologyCheckScope scope)
    {
        switch (scope.Kind)
        {
            case CheckScopeKind.Extent:
                if (finding.Location is null || finding.Location.IsNullOrEmpty())
                    return true;

                var anchor = finding.Location.GetBoundingBox().Center;
                var extent = scope.Extent!.Value;

                return anchor.X >= extent.XMin && anchor.X <= extent.XMax && anchor.Y >= extent.YMin && anchor.Y <= extent.YMax;

            case CheckScopeKind.Features:
                var chosen = new HashSet<long>(scope.FeatureIds);

                return finding.Features.Any(f => f.EntityName == scope.EntityName && chosen.Contains(f.FeatureId));

            default:
                return true;
        }
    }

    private static int IndexOf(IReadOnlyList<string> kinds, string kind)
    {
        for (int i = 0; i < kinds.Count; i++)
        {
            if (kinds[i] == kind)
                return i;
        }

        return int.MaxValue;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>One fix type's answer for one issue, after it was checked (FR-5.6) and the layers were asked.</summary>
public sealed class TopologyFixEvaluation
{
    public TopologyFixEvaluation(TopologyIssue issue, TopologyRule rule, TopologyFixOffer offer, TopologyFixVerdict? verdict, (string Reason, string? LayerName)? cannotApply)
    {
        Issue = issue;
        Rule = rule;
        Offer = offer;
        Verdict = verdict;
        CannotApply = cannotApply;
    }

    public TopologyIssue Issue { get; }

    public TopologyRule Rule { get; }

    public TopologyFixOffer Offer { get; }

    /// <summary>The checker's verdict; null when nothing was offered.</summary>
    public TopologyFixVerdict? Verdict { get; }

    /// <summary>Why the map layers cannot take the edits; null when they can.</summary>
    public (string Reason, string? LayerName)? CannotApply { get; }

    public bool CanApply => Offer.IsOffered && Verdict?.IsAccepted == true && CannotApply is null;
}

/// <summary>
/// The fixes of one Topology tab session (doc 05 §7): what each fix type offers for an issue, whether the result is
/// acceptable, applying it, and which issues have a fix pending until the next check. Holds no UI.
/// </summary>
public sealed class TopologyFixSession
{
    private readonly TopologyWorkspace _workspace;
    private readonly TopologyFixApplier _applier;
    private readonly TopologyFixChecker _checker;

    private readonly HashSet<string> _pending = new(StringComparer.Ordinal);
    private TopologyCheckResult? _pendingFor;

    public TopologyFixSession(TopologyWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _applier = new TopologyFixApplier(workspace);
        _checker = new TopologyFixChecker(workspace.Catalog);
    }

    public TopologyWorkspace Workspace => _workspace;

    /// <summary>Raised after a fix was applied.</summary>
    public event EventHandler? Applied;

    /// <summary>Whether any fix type suits the issue's kind.</summary>
    public bool HasFixFor(TopologyIssue issue)
    {
        var rule = _workspace.RuleSet.FindRule(issue.RuleId);

        return rule is not null && _workspace.Catalog.FixesFor(rule.RuleTypeId, issue.Kind).Count > 0;
    }

    /// <summary>Every fix type's answer for the issue, with the session's rules on the data as it is now.</summary>
    public async Task<IReadOnlyList<TopologyFixEvaluation>> EvaluateAsync(TopologyIssue issue, CancellationToken cancellationToken)
    {
        if (issue is null)
            throw new ArgumentNullException(nameof(issue));

        var ruleSet = _workspace.Runnable(_workspace.RuleSet, out _);
        var rule = ruleSet.FindRule(issue.RuleId);

        if (rule is null || !rule.Enabled)
            return Array.Empty<TopologyFixEvaluation>();

        var fixTypes = _workspace.Catalog.FixesFor(rule.RuleTypeId, issue.Kind);

        if (fixTypes.Count == 0)
            return Array.Empty<TopologyFixEvaluation>();

        var sources = _workspace.SourcesFor(ruleSet);
        var features = await FeaturesOfAsync(issue.Finding, sources, cancellationToken);
        var isGeodetic = sources.FirstOrDefault()?.Srid == SridHelper.GeodeticWGS84;

        var evaluations = new List<TopologyFixEvaluation>();

        foreach (var fixType in fixTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var offer = fixType.Propose(new TopologyFixContext(ruleSet, rule, issue.Finding, features, isGeodetic));

            TopologyFixVerdict? verdict = null;
            (string, string?)? cannotApply = null;

            if (offer.IsOffered)
            {
                verdict = await Task.Run(() => _checker.CheckAsync(ruleSet, issue, offer.Proposal!, sources, cancellationToken), cancellationToken);
                cannotApply = await _applier.CannotApplyAsync(offer.Proposal!);
            }

            evaluations.Add(new TopologyFixEvaluation(issue, rule, offer, verdict, cannotApply));
        }

        return evaluations;
    }

    /// <summary>Applies the fix as pending edits; the issue is "fix pending" until the next check.</summary>
    public async Task ApplyAsync(TopologyFixEvaluation evaluation)
    {
        if (evaluation is null)
            throw new ArgumentNullException(nameof(evaluation));

        if (!evaluation.CanApply)
            throw new InvalidOperationException("This fix cannot be applied.");

        await _applier.ApplyAsync(evaluation.Offer.Proposal!, TopologyMessages.RuleTitle(evaluation.Rule));

        if (!ReferenceEquals(_pendingFor, _workspace.Result))
        {
            _pending.Clear();
            _pendingFor = _workspace.Result;
        }

        _pending.Add(evaluation.Issue.IdentityHash);

        Applied?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>A fix for the issue was applied since the last check.</summary>
    public bool IsPending(TopologyIssue issue) => ReferenceEquals(_pendingFor, _workspace.Result) && _pending.Contains(issue.IdentityHash);

    /// <summary>The fixes applied since the last check.</summary>
    public int PendingCount => ReferenceEquals(_pendingFor, _workspace.Result) ? _pending.Count : 0;

    private static async Task<IReadOnlyDictionary<(string EntityName, long Id), Feature<Point>>> FeaturesOfAsync(
        TopologyFinding finding, IReadOnlyList<ITopologyFeatureSource> sources, CancellationToken cancellationToken)
    {
        var features = new Dictionary<(string, long), Feature<Point>>();

        foreach (var group in finding.Features.GroupBy(f => f.EntityName))
        {
            var source = sources.FirstOrDefault(s => s.EntityName == group.Key);

            if (source is null)
                continue;

            var found = await source.GetFeaturesByIdsAsync(group.Select(f => f.FeatureId).Distinct().ToList(), Array.Empty<string>(), cancellationToken);

            foreach (var feature in found)
                features[(group.Key, feature.Id)] = feature;
        }

        return features;
    }
}

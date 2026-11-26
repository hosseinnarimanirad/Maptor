using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;

namespace IRI.Maptor.Core.Topology.Fixes;

/// <summary>Whether a fix may be applied, and why not (FR-5.6).</summary>
public sealed class TopologyFixVerdict
{
    private TopologyFixVerdict(string? reason, TopologyIssue? causedIssue, IReadOnlyList<TopologyIssue> newIssues)
    {
        Reason = reason;
        CausedIssue = causedIssue;
        NewIssues = newIssues;
    }

    public static TopologyFixVerdict Accepted(IReadOnlyList<TopologyIssue> newIssues) => new(null, null, newIssues);

    public static TopologyFixVerdict Refused(string reason, TopologyIssue? causedIssue, IReadOnlyList<TopologyIssue> newIssues) => new(reason, causedIssue, newIssues);

    public bool IsAccepted => Reason is null;

    /// <summary><see cref="FixReasons.DoesNotResolve"/> or <see cref="FixReasons.CreatesIssue"/> when refused.</summary>
    public string? Reason { get; }

    /// <summary>For <see cref="FixReasons.CreatesIssue"/>: the first new issue at least as severe as the fixed one.</summary>
    public TopologyIssue? CausedIssue { get; }

    /// <summary>Every issue found after the fix that was not there before, milder ones included.</summary>
    public IReadOnlyList<TopologyIssue> NewIssues { get; }
}

/// <summary>
/// Checks a proposed fix before anyone applies it (FR-5.6): the rules run on the fix's area as the data is and as it
/// would be after the edits. The fix is refused when the fixed issue is still found, or when a new issue at least as
/// severe appears. It writes nothing.
/// </summary>
public sealed class TopologyFixChecker
{
    private readonly ValidationRunner _runner;

    public TopologyFixChecker(TopologyRuleCatalog catalog)
    {
        _runner = new ValidationRunner(catalog ?? throw new ArgumentNullException(nameof(catalog)));
    }

    /// <param name="ruleSet">The rules to run (those the issue was found with).</param>
    /// <param name="issue">The issue the fix is for.</param>
    /// <param name="proposal">The fix.</param>
    /// <param name="sources">The data as it is now, one source per layer the rules use.</param>
    /// <param name="cancellationToken">Cancels both checks.</param>
    public async Task<TopologyFixVerdict> CheckAsync(
        TopologyRuleSet ruleSet,
        TopologyIssue issue,
        TopologyFixProposal proposal,
        IReadOnlyList<ITopologyFeatureSource> sources,
        CancellationToken cancellationToken = default)
    {
        if (ruleSet is null)
            throw new ArgumentNullException(nameof(ruleSet));

        if (issue is null)
            throw new ArgumentNullException(nameof(issue));

        if (proposal is null)
            throw new ArgumentNullException(nameof(proposal));

        if (sources is null)
            throw new ArgumentNullException(nameof(sources));

        var scope = TopologyCheckScope.ForExtent(proposal.Area);

        var before = await _runner.RunAsync(ruleSet, scope, sources, null, cancellationToken).ConfigureAwait(false);

        var edited = sources.Select(s => (ITopologyFeatureSource)new EditedFeatureSource(s, proposal.Edits)).ToList();

        var after = await _runner.RunAsync(ruleSet, scope, edited, null, cancellationToken).ConfigureAwait(false);

        var existing = new HashSet<string>(before.Issues.Select(i => i.IdentityHash), StringComparer.Ordinal);

        var newIssues = after.Issues.Where(i => !existing.Contains(i.IdentityHash)).ToList();

        if (after.Issues.Any(i => i.IdentityHash == issue.IdentityHash))
            return TopologyFixVerdict.Refused(FixReasons.DoesNotResolve, null, newIssues);

        var worse = newIssues.FirstOrDefault(i => i.Severity >= issue.Severity);

        return worse is null
            ? TopologyFixVerdict.Accepted(newIssues)
            : TopologyFixVerdict.Refused(FixReasons.CreatesIssue, worse, newIssues);
    }
}

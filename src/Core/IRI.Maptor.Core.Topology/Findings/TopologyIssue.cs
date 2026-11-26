using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Core.Topology.Findings;

/// <summary>
/// A finding as a check reports it: with its identity and the severity the rule set gives its kind.
/// </summary>
public sealed class TopologyIssue
{
    public TopologyIssue(TopologyFinding finding, string identityHash, IssueSeverity severity)
    {
        Finding = finding ?? throw new ArgumentNullException(nameof(finding));
        IdentityHash = identityHash ?? throw new ArgumentNullException(nameof(identityHash));
        Severity = severity;
    }

    public TopologyFinding Finding { get; }

    /// <summary>See <see cref="IssueIdentity"/>.</summary>
    public string IdentityHash { get; }

    /// <summary>The effective severity: the rule set's setting for the kind, or the rule type's default.</summary>
    public IssueSeverity Severity { get; }

    public string RuleId => Finding.RuleId;

    public string Kind => Finding.Kind;

    public override string ToString() => $"{Severity}: {Finding}";
}

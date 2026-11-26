using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Findings;

namespace IRI.Maptor.Core.Topology.Checking;

/// <summary>Progress of a running check: which rule is in work.</summary>
public sealed class TopologyCheckProgress
{
    public TopologyCheckProgress(int ruleIndex, int ruleCount, string ruleId)
    {
        RuleIndex = ruleIndex;
        RuleCount = ruleCount;
        RuleId = ruleId;
    }

    /// <summary>Zero-based position of the rule in work.</summary>
    public int RuleIndex { get; }

    public int RuleCount { get; }

    public string RuleId { get; }

    public override string ToString() => $"{RuleIndex + 1}/{RuleCount}: {RuleId}";
}

/// <summary>Counts and timings of a finished check, for a panel and a log line; never feature values.</summary>
public sealed class TopologyCheckSummary
{
    public TopologyCheckSummary(
        string ruleSetKey,
        int ruleSetVersion,
        TopologyCheckScope scope,
        DateTime startedAtUtc,
        TimeSpan duration,
        IReadOnlyList<string> rulesChecked,
        IReadOnlyDictionary<string, int> featuresRead,
        IReadOnlyList<TopologyIssue> issues)
    {
        RuleSetKey = ruleSetKey;
        RuleSetVersion = ruleSetVersion;
        Scope = scope;
        StartedAtUtc = startedAtUtc;
        Duration = duration;
        RulesChecked = rulesChecked;
        FeaturesRead = featuresRead;
        IssueCount = issues.Count;
        CountBySeverity = issues.GroupBy(i => i.Severity).ToDictionary(g => g.Key, g => g.Count());
        CountByRuleAndKind = issues.GroupBy(i => (i.RuleId, i.Kind)).ToDictionary(g => g.Key, g => g.Count());
    }

    public string RuleSetKey { get; }

    public int RuleSetVersion { get; }

    public TopologyCheckScope Scope { get; }

    public DateTime StartedAtUtc { get; }

    public TimeSpan Duration { get; }

    /// <summary>The ids of the rules that ran, in order.</summary>
    public IReadOnlyList<string> RulesChecked { get; }

    /// <summary>Features loaded per layer, including neighbours just outside a partial scope.</summary>
    public IReadOnlyDictionary<string, int> FeaturesRead { get; }

    public int IssueCount { get; }

    public IReadOnlyDictionary<IssueSeverity, int> CountBySeverity { get; }

    public IReadOnlyDictionary<(string RuleId, string Kind), int> CountByRuleAndKind { get; }

    public override string ToString()
        => $"{RuleSetKey} v{RuleSetVersion}, {Scope}: {IssueCount} issues in {FeaturesRead.Values.Sum()} features, {Duration.TotalSeconds:0.0} s";
}

/// <summary>What a check found, sorted the same way every time.</summary>
public sealed class TopologyCheckResult
{
    public TopologyCheckResult(IReadOnlyList<TopologyIssue> issues, TopologyCheckSummary summary)
    {
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }

    public IReadOnlyList<TopologyIssue> Issues { get; }

    public TopologyCheckSummary Summary { get; }
}

/// <summary>A check could not start: a bad rule set, a missing layer, or data in the wrong SRS.</summary>
public sealed class TopologyCheckException : Exception
{
    public TopologyCheckException(string message) : base(message)
    {
    }

    public TopologyCheckException(IReadOnlyList<string> problems)
        : base(string.Join(Environment.NewLine, problems))
    {
        Problems = problems;
    }

    public IReadOnlyList<string> Problems { get; } = Array.Empty<string>();
}

namespace IRI.Maptor.Core.Topology.Findings;

/// <summary>How an issue changed from one check to the next.</summary>
public enum CheckChange
{
    /// <summary>Not in the previous check.</summary>
    New = 0,

    /// <summary>In both checks.</summary>
    StillThere = 1,

    /// <summary>In the previous check only: fixed, or the data changed.</summary>
    Gone = 2,
}

/// <summary>The result of comparing two checks of the same scope.</summary>
public sealed class CheckComparison
{
    private readonly HashSet<string> _previous;

    internal CheckComparison(IReadOnlyList<TopologyIssue> newIssues, IReadOnlyList<TopologyIssue> stillThere, IReadOnlyList<TopologyIssue> gone, HashSet<string> previous)
    {
        New = newIssues;
        StillThere = stillThere;
        Gone = gone;
        _previous = previous;
    }

    /// <summary>Issues of the current check that the previous one did not have.</summary>
    public IReadOnlyList<TopologyIssue> New { get; }

    /// <summary>Issues of the current check that the previous one had too.</summary>
    public IReadOnlyList<TopologyIssue> StillThere { get; }

    /// <summary>Issues of the previous check that the current one does not have.</summary>
    public IReadOnlyList<TopologyIssue> Gone { get; }

    /// <summary>New or still there, for an issue of the current check.</summary>
    public CheckChange ChangeOf(TopologyIssue current)
        => _previous.Contains(current.IdentityHash) ? CheckChange.StillThere : CheckChange.New;
}

/// <summary>
/// Compares two checks of the same scope by issue identity (<see cref="IssueIdentity"/>), to show what a fix
/// or an edit achieved. Comparing checks of different scopes reports issues outside the smaller scope as
/// gone or new; callers compare like with like.
/// </summary>
public static class CheckComparer
{
    public static CheckComparison Compare(IReadOnlyList<TopologyIssue> previous, IReadOnlyList<TopologyIssue> current)
    {
        if (previous is null)
            throw new ArgumentNullException(nameof(previous));

        if (current is null)
            throw new ArgumentNullException(nameof(current));

        var previousHashes = new HashSet<string>(previous.Select(i => i.IdentityHash), StringComparer.Ordinal);
        var currentHashes = new HashSet<string>(current.Select(i => i.IdentityHash), StringComparer.Ordinal);

        var newIssues = current.Where(i => !previousHashes.Contains(i.IdentityHash)).ToList();
        var stillThere = current.Where(i => previousHashes.Contains(i.IdentityHash)).ToList();
        var gone = previous.Where(i => !currentHashes.Contains(i.IdentityHash)).ToList();

        return new CheckComparison(newIssues, stillThere, gone, previousHashes);
    }
}

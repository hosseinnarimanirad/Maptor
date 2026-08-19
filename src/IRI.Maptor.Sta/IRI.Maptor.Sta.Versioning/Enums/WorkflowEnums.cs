namespace IRI.Maptor.Sta.Versioning;

/// <remarks>
/// There is no Draft value: draft sessions live client-side only, and the server row is
/// created at submission.
/// </remarks>
public enum SessionState : byte
{
    Submitted = 1,
    Withdrawn = 2,
}

public enum DecisionAction : byte
{
    SelectWinner = 1,
    RejectProposal = 2,
    CloseNoWinner = 3,
    Approve = 4,
    Return = 5,
    GroupProposals = 6,
}

public enum NotificationType : byte
{
    CompetitionJoined = 1,
    Committed = 2,
    Rejected = 3,
    ClosedNoWinner = 4,
    Returned = 5,
    Orphaned = 6,
}

public enum OverlapKind : byte
{
    /// <summary>Visible to reviewers/approvers only — never to editors.</summary>
    PendingVsPending = 1,

    /// <summary>Editor-facing advisory against committed (public) features.</summary>
    PendingVsLive = 2,
}

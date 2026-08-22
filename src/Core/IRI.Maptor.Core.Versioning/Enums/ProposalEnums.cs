namespace IRI.Maptor.Core.Versioning;

public enum ProposalChangeType : byte
{
    Create = 1,
    Update = 2,
    Delete = 3,
}

/// <remarks>
/// Numeric order is load-bearing: values &lt;= 2 are the "active" states, and the
/// database filtered unique indexes (one active proposal per editor+target, collision
/// lookup) filter on <c>[State] &lt;= 2</c>. Do not renumber.
/// </remarks>
public enum ProposalState : byte
{
    Submitted = 0,
    SelectedForApproval = 1,
    ProvisionallyRejected = 2,
    Committed = 3,
    Rejected = 4,
    Withdrawn = 5,
}

public enum WithdrawCause : byte
{
    User = 1,
    SessionWithdrawn = 2,
    Superseded = 3,
}

/// <remarks>
/// Editors must not be able to distinguish a provisionally rejected proposal from a
/// selected one before commit (rejection notifications are deferred until then), so both
/// map to <see cref="UnderReview"/>.
/// </remarks>
public enum EditorFacingStatus : byte
{
    PendingReview = 0,
    InCompetition = 1,
    UnderReview = 2,
    Committed = 3,
    Rejected = 4,
    Withdrawn = 5,
}

namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// Append-only audit of every review/approval action. Rows are never updated or deleted;
/// retention is indefinite.
/// </summary>
public class DecisionRecord
{
    public long Id { get; set; }

    public long CompetitionId { get; set; }
    public Competition? Competition { get; set; }

    /// <summary>Null for competition-level actions (Return, CloseNoWinner).</summary>
    public long? ProposalId { get; set; }
    public Proposal? Proposal { get; set; }

    public int ActorUserId { get; set; }

    public string ActorDisplayName { get; set; } = string.Empty;

    public DecisionAction Action { get; set; }

    /// <summary>Required for RejectProposal, CloseNoWinner, and Return.</summary>
    public string? Reason { get; set; }

    /// <summary>Set when the actor knowingly accepted/approved over a stale base.</summary>
    public bool IsStaleOverride { get; set; }

    public long? CommitBatchId { get; set; }
    public CommitBatch? CommitBatch { get; set; }

    public DateTime CreatedAt { get; set; }
}

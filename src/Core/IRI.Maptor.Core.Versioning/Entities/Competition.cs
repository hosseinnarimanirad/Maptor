namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// The decision unit: all proposals contending for one target. Resolution selects one
/// winner (losers become provisionally rejected until commit) or closes with no winner.
/// An approver return reopens it with every proposal back in play.
/// </summary>
public class Competition
{
    public long Id { get; set; }

    public int VersionedLayerId { get; set; }
    public VersionedLayer? Layer { get; set; }

    /// <summary>Null for manually-grouped create competitions (no shared live target).</summary>
    public long? TargetFeatureId { get; set; }

    public CompetitionKind Kind { get; set; }

    public CompetitionState State { get; set; }

    public long? WinnerProposalId { get; set; }
    public Proposal? Winner { get; set; }

    /// <summary>
    /// Queued-competition chain: while a competition on a target is Resolved (in
    /// approval), late submissions open a new competition pointing here; it cannot be
    /// resolved until the predecessor reaches a terminal state.
    /// </summary>
    public long? PredecessorCompetitionId { get; set; }
    public Competition? Predecessor { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public byte[] RowVersion { get; set; }

    public List<Proposal> Proposals { get; set; } = new();
}

namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// Spatial-overlap advisory computed once at submission and persisted so the reviewer
/// queue is cheap to render and dismissals are remembered. PendingVsPending rows surface
/// only to reviewers/approvers; PendingVsLive rows are the editor-facing advisory.
/// </summary>
public class OverlapSuggestion
{
    public long Id { get; set; }

    public long ProposalId { get; set; }
    public Proposal? Proposal { get; set; }

    public OverlapKind Kind { get; set; }

    /// <summary>The other pending proposal, for PendingVsPending.</summary>
    public long? OtherProposalId { get; set; }
    public Proposal? OtherProposal { get; set; }

    /// <summary>The committed feature, for PendingVsLive.</summary>
    public long? LiveFeatureId { get; set; }

    public DateTime ComputedAt { get; set; }

    public int? DismissedByUserId { get; set; }

    public string? DismissedByDisplayName { get; set; }

    public DateTime? DismissedAt { get; set; }
}

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// One proposed create/update/delete of one feature, carrying the full proposed state
/// (not a delta) plus the live RowVersion it was based on, so staleness is detectable.
/// Every submitted proposal belongs to exactly one competition (possibly of size 1).
/// </summary>
public class Proposal
{
    public long Id { get; set; }

    public long SessionId { get; set; }
    public VersionSession? Session { get; set; }

    /// <summary>Denormalized from the session for the one-active-proposal-per-editor+target index.</summary>
    public int EditorUserId { get; set; }

    public string EditorDisplayName { get; set; } = string.Empty;

    public int VersionedLayerId { get; set; }
    public VersionedLayer? Layer { get; set; }

    /// <summary>Null for creates — a create has no live target until commit.</summary>
    public long? TargetFeatureId { get; set; }

    /// <summary>Client-assigned identity (Feature.Key); a create's identity before commit.</summary>
    public Guid ClientKey { get; set; }

    public ProposalChangeType ChangeType { get; set; }

    /// <summary>Null for deletes.</summary>
    public Geometry<Point>? ProposedGeometry { get; set; }

    /// <summary>Canonical JSON (sorted keys, invariant culture); null for deletes.</summary>
    public string? ProposedAttributesJson { get; set; }

    /// <summary>Live RowVersion at authoring time; null for creates.</summary>
    public byte[]? BaseRowVersion { get; set; }

    public string SchemaSignatureAtSubmit { get; set; } = string.Empty;

    public long CompetitionId { get; set; }
    public Competition? Competition { get; set; }

    public ProposalState State { get; set; }

    public WithdrawCause? WithdrawCause { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public byte[] RowVersion { get; set; }
}

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// Copy-on-write past state: written inside the commit transaction, before the live row
/// is overwritten or deleted. Creates write no history row. A feature's timeline is the
/// live row plus its history rows walking backward; each hop links the proposal that
/// replaced it (author, session, competition, decisions all reachable from there).
/// </summary>
public class FeatureHistory
{
    public long Id { get; set; }

    public int VersionedLayerId { get; set; }
    public VersionedLayer? Layer { get; set; }

    public long FeatureId { get; set; }

    public Geometry<Point>? Geometry { get; set; }

    public string AttributesJson { get; set; } = string.Empty;

    /// <summary>The RowVersion this state had when it was replaced.</summary>
    public byte[] ReplacedRowVersion { get; set; }

    public long CommitBatchId { get; set; }
    public CommitBatch? CommitBatch { get; set; }

    public long WinningProposalId { get; set; }
    public Proposal? WinningProposal { get; set; }

    public DateTime SupersededAt { get; set; }
}

namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// A single-editor batch of proposals: the unit of submission and provenance only.
/// Decisions are per-feature (per competition), so a session may end partially accepted;
/// aggregate progress is derived from its proposals, never stored.
/// </summary>
public class VersionSession
{
    public long Id { get; set; }

    public int EditorUserId { get; set; }

    /// <summary>Stamped at submission; names in history are recorded facts, not lookups.</summary>
    public string EditorDisplayName { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public SessionState State { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? WithdrawnAt { get; set; }

    public byte[] RowVersion { get; set; }

    public List<Proposal> Proposals { get; set; } = new();
}

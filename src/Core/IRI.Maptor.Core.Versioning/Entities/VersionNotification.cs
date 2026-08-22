namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// In-app inbox item, aggregated as per-session digests (never one row per feature).
/// Fetched by manual refresh only — there is no polling or push in v1.
/// </summary>
public class VersionNotification
{
    public long Id { get; set; }

    public int RecipientUserId { get; set; }

    public NotificationType Type { get; set; }

    public long? SessionId { get; set; }

    public long? CompetitionId { get; set; }

    /// <summary>Digest content: counts, reasons, feature references.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}

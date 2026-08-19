namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// One inbox row — a per-session digest (N1–N5), normalized server-side from the stored
/// payload so clients never parse PayloadJson. Fetched by manual refresh only (D37).
/// </summary>
public class InboxItemDto
{
    public long Id { get; set; }

    public NotificationType Type { get; set; }

    public long? SessionId { get; set; }

    public long? CompetitionId { get; set; }

    /// <summary>Proposals or competitions aggregated in this digest.</summary>
    public int ItemCount { get; set; }

    /// <summary>Feature ids the digest references (creates without ids are omitted).</summary>
    public List<long> TargetFeatureIds { get; set; } = new();

    /// <summary>Distinct non-empty reasons carried by the digest (rejections, returns).</summary>
    public List<string> Reasons { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}

public class InboxMarkReadRequestDto
{
    /// <summary>Notification ids to mark read; ignored when <see cref="All"/> is set.</summary>
    public List<long> Ids { get; set; } = new();

    public bool All { get; set; }
}

public class InboxMarkReadResultDto
{
    public int MarkedCount { get; set; }
}

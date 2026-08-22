namespace IRI.Maptor.Core.Versioning;

/// <summary>One row of the approval queue (Resolved competitions with fresh gate flags).</summary>
public class ApprovalQueueItemDto
{
    public long CompetitionId { get; set; }

    public Guid LayerKey { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public long? TargetFeatureId { get; set; }

    public long WinnerProposalId { get; set; }

    public ProposalChangeType WinnerChangeType { get; set; }

    public string WinnerEditorDisplayName { get; set; } = string.Empty;

    /// <summary>Who resolved the competition (from the SelectWinner decision record).</summary>
    public string ReviewerDisplayName { get; set; } = string.Empty;

    public DateTime ResolvedAt { get; set; }

    public int ProposalCount { get; set; }

    /// <summary>Re-checked NOW against live — drift after the review needs a fresh override (D8).</summary>
    public bool IsStale { get; set; }

    /// <summary>Target vanished from live after resolution — only a return is possible.</summary>
    public bool IsOrphaned { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();
}

public class CommitItemDto
{
    public long CompetitionId { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>D8: required when the winner's base is stale at approval time; recorded.</summary>
    public bool StaleOverride { get; set; }
}

/// <summary>One commit batch — all items commit or none do (E9).</summary>
public class CommitRequestDto
{
    public List<CommitItemDto> Items { get; set; } = new();
}

public class CommitResultDto
{
    public long CommitBatchId { get; set; }

    public List<long> CommittedCompetitionIds { get; set; } = new();

    /// <summary>Tolerant-mapping notes (dropped fields etc., doc 03 §5.3) — informational.</summary>
    public List<string> Warnings { get; set; } = new();
}

public class ReturnRequestDto
{
    public long CompetitionId { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    public string Reason { get; set; } = string.Empty;
}

/// <summary>One past state of a feature (copy-on-write hop) with its replacement's provenance.</summary>
public class FeatureTimelineEntryDto
{
    public long HistoryId { get; set; }

    /// <summary>When this state stopped being live.</summary>
    public DateTime SupersededAt { get; set; }

    public byte[]? GeometryWkb { get; set; }

    public int Srid { get; set; }

    public Dictionary<string, object?> Attributes { get; set; } = new();

    public long CommitBatchId { get; set; }

    public string ApproverDisplayName { get; set; } = string.Empty;

    public long WinningProposalId { get; set; }

    /// <summary>Author of the change that replaced this state.</summary>
    public string EditorDisplayName { get; set; } = string.Empty;

    public ProposalChangeType ChangeType { get; set; }

    public string? SessionTitle { get; set; }
}

public class FeatureTimelineDto
{
    public Guid LayerKey { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public long FeatureId { get; set; }

    /// <summary>Null when the feature no longer exists (deleted).</summary>
    public LiveFeatureStateDto? Live { get; set; }

    /// <summary>Newest first; each entry is the state that the linked proposal replaced.</summary>
    public List<FeatureTimelineEntryDto> Entries { get; set; } = new();
}

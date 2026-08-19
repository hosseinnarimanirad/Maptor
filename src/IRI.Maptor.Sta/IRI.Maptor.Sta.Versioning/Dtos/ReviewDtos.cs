namespace IRI.Maptor.Sta.Versioning;

/// <summary>One row of the reviewer queue (Open competitions only; singletons included).</summary>
public class ReviewQueueItemDto
{
    public long CompetitionId { get; set; }

    public Guid LayerKey { get; set; }

    public string EntityName { get; set; } = string.Empty;

    /// <summary>Null for manually-grouped create competitions.</summary>
    public long? TargetFeatureId { get; set; }

    public CompetitionKind Kind { get; set; }

    /// <summary>Concurrency token for decisions — echo back in Select/Close requests (E4).</summary>
    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    public int ProposalCount { get; set; }

    public DateTime OldestSubmittedAt { get; set; }

    /// <summary>Any active proposal's base RowVersion no longer matches live.</summary>
    public bool IsStale { get; set; }

    /// <summary>The target feature no longer exists in live — reject-only (D23).</summary>
    public bool IsOrphaned { get; set; }

    /// <summary>Queued behind a Resolved predecessor (D19) — cannot be resolved yet.</summary>
    public bool IsBlockedByPredecessor { get; set; }

    public bool HasDelete { get; set; }

    public int UndismissedSuggestionCount { get; set; }

    public List<string> AuthorDisplayNames { get; set; } = new();

    /// <summary>The lone proposal's id when this is a singleton — manual grouping (D16) needs it.</summary>
    public long? SingleProposalId { get; set; }
}

/// <summary>Current live state of the target, for the compare view. Null when orphaned or create-only.</summary>
public class LiveFeatureStateDto
{
    public long FeatureId { get; set; }

    public byte[]? GeometryWkb { get; set; }

    public int Srid { get; set; }

    public Dictionary<string, object?> Attributes { get; set; } = new();

    public byte[]? RowVersion { get; set; }
}

public class ProposalCompareDto
{
    public long ProposalId { get; set; }

    public long SessionId { get; set; }

    public string? SessionTitle { get; set; }

    public string? SessionComment { get; set; }

    public int EditorUserId { get; set; }

    public string EditorDisplayName { get; set; } = string.Empty;

    public ProposalChangeType ChangeType { get; set; }

    public byte[]? GeometryWkb { get; set; }

    public int Srid { get; set; }

    public Dictionary<string, object?>? Attributes { get; set; }

    public bool IsStale { get; set; }

    public DateTime SubmittedAt { get; set; }
}

/// <summary>Everything the compare view needs: raw states — diffs are computed client-side.</summary>
public class CompetitionCompareDto
{
    public long CompetitionId { get; set; }

    public Guid LayerKey { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public long? TargetFeatureId { get; set; }

    public CompetitionKind Kind { get; set; }

    public CompetitionState State { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    public bool IsBlockedByPredecessor { get; set; }

    public bool IsOrphaned { get; set; }

    public LiveFeatureStateDto? Live { get; set; }

    public List<ProposalCompareDto> Proposals { get; set; } = new();
}

public class SelectWinnerRequestDto
{
    public long CompetitionId { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    public long WinnerProposalId { get; set; }

    /// <summary>Per-loser rejection reasons; <see cref="ReasonForAll"/> fills the gaps.</summary>
    public Dictionary<long, string>? LoserReasons { get; set; }

    public string? ReasonForAll { get; set; }

    /// <summary>D8: required (and recorded) when the winner's base is stale.</summary>
    public bool StaleOverride { get; set; }
}

public class CloseNoWinnerRequestDto
{
    public long CompetitionId { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    public Dictionary<long, string>? Reasons { get; set; }

    public string? ReasonForAll { get; set; }
}

/// <summary>Manual grouping (D16): merge active create-proposals of one layer into one competition.</summary>
public class GroupProposalsRequestDto
{
    public List<long> ProposalIds { get; set; } = new();
}

/// <summary>Result envelope for manual grouping (bare JSON numbers don't fit the typed HTTP client).</summary>
public class GroupResultDto
{
    public long CompetitionId { get; set; }
}

public class BulkAcceptItemDto
{
    public long CompetitionId { get; set; }

    public byte[] CompetitionRowVersion { get; set; } = Array.Empty<byte>();

    public bool StaleOverride { get; set; }
}

/// <summary>Bulk fast path (D40): singleton competitions only; per-item outcomes, failures don't abort the rest.</summary>
public class BulkAcceptRequestDto
{
    public List<BulkAcceptItemDto> Items { get; set; } = new();
}

public class BulkAcceptResultItemDto
{
    public long CompetitionId { get; set; }

    public bool Succeeded { get; set; }

    public long? WinnerProposalId { get; set; }

    public string? Error { get; set; }
}

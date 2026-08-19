namespace IRI.Maptor.Sta.Versioning;

/// <summary>One proposed change inside a session submission.</summary>
public class ProposalSubmitDto
{
    public Guid LayerKey { get; set; }

    public long? TargetFeatureId { get; set; }

    public Guid ClientKey { get; set; }

    public ProposalChangeType ChangeType { get; set; }

    /// <summary>WKB, same convention as FeatureDto.Shape; null for deletes.</summary>
    public byte[]? GeometryBytes { get; set; }

    public int Srid { get; set; }

    /// <summary>Attribute values; null for deletes. Serialized canonically server-side.</summary>
    public Dictionary<string, object?>? Attributes { get; set; }

    /// <summary>Live RowVersion the edit was based on; null for creates.</summary>
    public byte[]? BaseRowVersion { get; set; }
}

public class SessionSubmitDto
{
    public string? Title { get; set; }

    public string? Comment { get; set; }

    public List<ProposalSubmitDto> Proposals { get; set; } = new();
}

public class ProposalSubmitResultDto
{
    public Guid ClientKey { get; set; }

    public long ProposalId { get; set; }

    public EditorFacingStatus Status { get; set; }

    /// <summary>Total proposals in the competition, including this one.</summary>
    public int CompetitorCount { get; set; }

    /// <summary>Set when this submission auto-withdrew the editor's earlier proposal on the same target.</summary>
    public long? SupersededProposalId { get; set; }

    /// <summary>Editor-facing advisory: live features this geometry overlaps.</summary>
    public List<long> OverlappingLiveFeatureIds { get; set; } = new();
}

public class SessionSubmitResultDto
{
    public long SessionId { get; set; }

    public List<ProposalSubmitResultDto> Proposals { get; set; } = new();
}

/// <summary>Result of the on-demand per-feature pending-status check.</summary>
public class PendingStatusDto
{
    public int Count { get; set; }

    /// <summary>Display names of editors with pending proposals on the feature (content stays hidden).</summary>
    public List<string> Authors { get; set; } = new();

    public bool HasOwn { get; set; }
}

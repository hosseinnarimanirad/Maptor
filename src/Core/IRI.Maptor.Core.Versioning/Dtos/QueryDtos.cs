namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// Registry row as the client sees it. The client matches its layers by
/// <see cref="TableName"/> (LayerSetting has no Guids); <see cref="LayerKey"/> is the
/// stable key used in subsequent API calls.
/// </summary>
public class VersionedLayerInfoDto
{
    public Guid LayerKey { get; set; }

    public string EntityName { get; set; } = string.Empty;

    /// <summary>SQL table name, resolved from EF metadata server-side.</summary>
    public string TableName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsVersioningEnabled { get; set; }
}

/// <summary>
/// Own pending proposal with geometry, for the per-layer map overlay (doc 02 §5:
/// the map shows live truth; own pending work is a separate on-demand overlay).
/// Deletes carry no proposed geometry and are not included.
/// </summary>
public class MyLayerPendingFeatureDto
{
    public long ProposalId { get; set; }

    public long? TargetFeatureId { get; set; }

    public ProposalChangeType ChangeType { get; set; }

    public EditorFacingStatus Status { get; set; }

    public DateTime SubmittedAt { get; set; }

    /// <summary>WKB, same convention as FeatureDto.Shape.</summary>
    public byte[] GeometryWkb { get; set; } = Array.Empty<byte>();

    public int Srid { get; set; }
}

/// <summary>
/// Own-proposal row for the My-Pending panel. Status is the collapsed editor-facing
/// label — provisional review outcomes are indistinguishable by design.
/// </summary>
public class MyProposalDto
{
    public long ProposalId { get; set; }

    public long SessionId { get; set; }

    public string? SessionTitle { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public long? TargetFeatureId { get; set; }

    public Guid ClientKey { get; set; }

    public ProposalChangeType ChangeType { get; set; }

    public EditorFacingStatus Status { get; set; }

    public int CompetitorCount { get; set; }

    public DateTime SubmittedAt { get; set; }
}

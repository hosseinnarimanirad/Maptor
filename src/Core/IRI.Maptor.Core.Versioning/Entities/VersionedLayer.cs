namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// Per-layer opt-in registry. Layers with <see cref="IsVersioningEnabled"/> reject the
/// direct sync write path; their history starts at <see cref="EnabledAt"/> (no baseline
/// snapshot is taken).
/// </summary>
public class VersionedLayer
{
    public int Id { get; set; }

    /// <summary>Matches the client-side FeatureSet.LayerId.</summary>
    public Guid LayerKey { get; set; }

    /// <summary>Server entity/table identity (e.g. "TransmissionLine").</summary>
    public string EntityName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsVersioningEnabled { get; set; }

    public string? SchemaSignature { get; set; }

    public DateTime? SchemaSignatureUpdatedAt { get; set; }

    public DateTime? EnabledAt { get; set; }
}

using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Core.FileGdb.Catalog.Model;

/// <summary>
/// A relationship class definition. Metadata only: rows are not joined.
/// </summary>
/// <remarks>
/// For one-to-one and one-to-many, destination rows hold <see cref="OriginForeignKey"/> pointing at
/// the origin's <see cref="OriginPrimaryKey"/>. For many-to-many, a mapping table named like the
/// relationship class holds <see cref="OriginForeignKey"/> and <see cref="DestinationForeignKey"/>,
/// pointing at <see cref="OriginPrimaryKey"/> and <see cref="DestinationPrimaryKey"/>.
/// </remarks>
public sealed class FileGdbRelationshipClass
{
    public string Name { get; internal set; } = string.Empty;

    public string? FeatureDatasetName { get; internal set; }

    public GdbRelationshipCardinality Cardinality { get; internal set; }

    public string OriginClassName { get; internal set; } = string.Empty;

    public string DestinationClassName { get; internal set; } = string.Empty;

    public string? OriginPrimaryKey { get; internal set; }

    public string? OriginForeignKey { get; internal set; }

    /// <summary>Many-to-many only.</summary>
    public string? DestinationPrimaryKey { get; internal set; }

    /// <summary>Many-to-many only.</summary>
    public string? DestinationForeignKey { get; internal set; }

    /// <summary>Deleting an origin row deletes its destination rows.</summary>
    public bool IsComposite { get; internal set; }

    /// <summary>The relationship has its own attribute table (always the case for many-to-many with attributes).</summary>
    public bool IsAttributed { get; internal set; }

    public string? ForwardPathLabel { get; internal set; }

    public string? BackwardPathLabel { get; internal set; }

    /// <summary>Esri notification direction, e.g. <c>esriRelNotificationNone</c>.</summary>
    public string? Notification { get; internal set; }

    public override string ToString() => $"{Name}: {OriginClassName} -> {DestinationClassName} ({Cardinality})";
}

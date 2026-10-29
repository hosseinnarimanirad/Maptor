using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Core.FileGdb.Catalog.Model;

/// <summary>
/// A feature class or standalone table: its definition from GDB_Items joined with the physical
/// table listed in GDB_SystemCatalog.
/// </summary>
internal sealed class GdbDatasetInfo
{
    private Dictionary<string, GdbItemFieldInfo>? _fieldsByName;

    public string Name { get; set; } = string.Empty;

    public GdbDatasetKind Kind { get; set; }

    /// <summary>Path inside the geodatabase, e.g. <c>\FD_Default\Roads</c>; <c>null</c> without a definition.</summary>
    public string? CatalogPath { get; set; }

    /// <summary>The feature dataset holding this dataset, or <c>null</c> at the geodatabase root.</summary>
    public string? FeatureDatasetName { get; set; }

    public string? AliasName { get; set; }

    /// <summary>
    /// False for a table found in GDB_SystemCatalog with no GDB_Items entry: it is listed, but has
    /// no aliases, domains, subtypes or spatial reference.
    /// </summary>
    public bool HasDefinition { get; set; }

    /// <summary>The physical table, or <c>null</c> when the system catalog does not list it.</summary>
    public GdbCatalogEntry? Table { get; set; }

    /// <summary>Esri shape type, e.g. <c>esriGeometryPolygon</c>; <c>null</c> for tables.</summary>
    public string? ShapeType { get; set; }

    public string? ShapeFieldName { get; set; }

    public string? ObjectIdFieldName { get; set; }

    public FileGdbSpatialReference? SpatialReference { get; set; }

    /// <summary>Field metadata from the definition (aliases, domains), in definition order.</summary>
    public IReadOnlyList<GdbItemFieldInfo> Fields { get; set; } = Array.Empty<GdbItemFieldInfo>();

    /// <summary>The field whose value selects the subtype, or <c>null</c> when there are no subtypes.</summary>
    public string? SubtypeFieldName { get; set; }

    public int? DefaultSubtypeCode { get; set; }

    public IReadOnlyList<FileGdbSubtype> Subtypes { get; set; } = Array.Empty<FileGdbSubtype>();

    public IReadOnlyList<string> RelationshipClassNames { get; set; } = Array.Empty<string>();

    public GdbItemFieldInfo? GetField(string fieldName)
    {
        _fieldsByName ??= Fields.GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return _fieldsByName.TryGetValue(fieldName, out var field) ? field : null;
    }

    public FileGdbSubtype? GetSubtype(int code) => Subtypes.FirstOrDefault(s => s.Code == code);

    /// <summary>
    /// The domain of a field for a row of the given subtype: the subtype's own domain when it sets
    /// one, else the field's domain. Pass <c>null</c> when the row's subtype is unknown.
    /// </summary>
    public string? GetDomainName(string fieldName, int? subtypeCode)
    {
        if (subtypeCode is int code && GetSubtype(code)?.GetField(fieldName)?.DomainName is string subtypeDomain)
            return subtypeDomain;

        return GetField(fieldName)?.DomainName;
    }

    public override string ToString() => $"{Name} ({Kind})";
}

/// <summary>
/// Field metadata from an item definition (GPFieldInfoEx). The physical layout (type, width) comes
/// from the table itself; this adds what only the definition holds.
/// </summary>
internal sealed class GdbItemFieldInfo
{
    public string Name { get; set; } = string.Empty;

    public string? AliasName { get; set; }

    public string? DomainName { get; set; }

    /// <summary>Esri field type, e.g. <c>esriFieldTypeInteger</c>.</summary>
    public string? FieldType { get; set; }

    public bool IsNullable { get; set; }

    public override string ToString() => Name;
}

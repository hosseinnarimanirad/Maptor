namespace IRI.Maptor.Core.FileGdb.Catalog.Model;

/// <summary>
/// A subtype of a dataset: a code stored in the subtype field, with per-field domain and default overrides.
/// </summary>
public sealed class FileGdbSubtype
{
    public int Code { get; internal set; }

    public string Name { get; internal set; } = string.Empty;

    /// <summary>Per-field domain and default overrides for this subtype.</summary>
    public IReadOnlyList<FileGdbSubtypeField> Fields { get; internal set; } = Array.Empty<FileGdbSubtypeField>();

    public FileGdbSubtypeField? GetField(string fieldName)
        => Fields.FirstOrDefault(f => string.Equals(f.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

    public override string ToString() => $"{Code} = {Name}";
}

public sealed class FileGdbSubtypeField
{
    public string FieldName { get; internal set; } = string.Empty;

    /// <summary>Domain for this field within the subtype; <c>null</c> when the subtype sets none.</summary>
    public string? DomainName { get; internal set; }

    /// <summary>Default for this field within the subtype, typed as in the XML; may be <c>null</c>.</summary>
    public object? DefaultValue { get; internal set; }
}

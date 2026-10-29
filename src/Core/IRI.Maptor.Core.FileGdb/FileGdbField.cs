using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.FileGdb.Table;

namespace IRI.Maptor.Core.FileGdb;

/// <summary>
/// A field of a <see cref="FileGdbDataset"/>: its physical layout from the table, with the alias
/// and domain from the dataset's definition.
/// </summary>
public sealed class FileGdbField
{
    internal FileGdbField(GdbFieldDescriptor physical, GdbItemFieldInfo? definition, FileGdbDomain? domain, int index)
    {
        Name = physical.Name;
        AliasName = string.IsNullOrEmpty(definition?.AliasName) ? physical.Alias : definition!.AliasName;
        FieldType = physical.FieldType;
        Length = physical.MaxLength;
        IsNullable = physical.IsNullable;
        IsRequired = physical.IsRequired;
        IsEditable = physical.IsEditable;
        DefaultValue = physical.DefaultValue;
        DomainName = definition?.DomainName;
        Domain = domain;
        Index = index;
    }

    public string Name { get; }

    /// <summary>The alias from the definition, else the one stored in the table; <c>null</c> when neither is set.</summary>
    public string? AliasName { get; }

    public GdbFieldType FieldType { get; }

    /// <summary>Maximum length in characters for string fields; 0 for other types.</summary>
    public int Length { get; }

    public bool IsNullable { get; }

    public bool IsRequired { get; }

    public bool IsEditable { get; }

    /// <summary>Default value, typed like a row value (see <see cref="FileGdbFeature.Values"/>); <c>null</c> when none.</summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// The field's own domain. Rows of a subtype may use another one: see
    /// <see cref="FileGdbDataset.GetDomain"/> and <see cref="FileGdbFeature.GetDomain"/>.
    /// </summary>
    public string? DomainName { get; }

    /// <summary>The domain named by <see cref="DomainName"/>; <c>null</c> when there is none or it is not in the geodatabase.</summary>
    public FileGdbDomain? Domain { get; }

    /// <summary>Position in <see cref="FileGdbDataset.Fields"/> and <see cref="FileGdbFeature.Values"/>.</summary>
    public int Index { get; }

    public override string ToString() => $"{Name} ({FieldType})";
}

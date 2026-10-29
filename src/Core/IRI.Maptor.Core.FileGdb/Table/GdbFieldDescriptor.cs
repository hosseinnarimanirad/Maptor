using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Core.FileGdb.Table;

/// <summary>
/// One field as described in the field section of a .gdbtable file.
/// </summary>
internal sealed class GdbFieldDescriptor
{
    internal const byte NullableFlag = 1;
    internal const byte RequiredFlag = 2;
    internal const byte EditableFlag = 4;

    public string Name { get; set; } = string.Empty;

    /// <summary>Alias stored with the field; <c>null</c> when none was set.</summary>
    public string? Alias { get; set; }

    public GdbFieldType FieldType { get; set; }

    public bool IsNullable { get; set; }

    public bool IsRequired { get; set; }

    public bool IsEditable { get; set; }

    /// <summary>Maximum string length in characters; 0 for non-string fields.</summary>
    public int MaxLength { get; set; }

    /// <summary>Storage width byte from the descriptor (e.g. 4 for int32, 38 for GUID).</summary>
    public byte Width { get; set; }

    /// <summary>Default value converted like a row value; <c>null</c> when none is stored.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>Set only for <see cref="GdbFieldType.Geometry"/>.</summary>
    public GdbGeometryFieldInfo? Geometry { get; set; }

    /// <summary>Set only for <see cref="GdbFieldType.Raster"/>.</summary>
    public GdbRasterFieldInfo? Raster { get; set; }

    public override string ToString() => $"{Name} ({FieldType})";
}

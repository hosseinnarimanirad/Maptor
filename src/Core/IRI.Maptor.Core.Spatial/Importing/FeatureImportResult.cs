using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Importing;

/// <summary>Why a value, or a whole row, does not fit the layer.</summary>
public enum ImportIssueKind
{
    /// <summary>A required field is empty and the model gives no default.</summary>
    Required,

    /// <summary>The value is neither a value nor a code of the field's domain.</summary>
    OutOfDomain,

    /// <summary>The text is longer than the field allows.</summary>
    TooLong,

    /// <summary>The value cannot be read as the field's type (e.g. "abc" for a number, 3.5 for a whole number).</summary>
    WrongType,

    /// <summary>The row has no shape, or an empty one.</summary>
    NoShape,

    /// <summary>The shape is of another kind than the layer's (e.g. a line in a point layer).</summary>
    WrongShapeType,

    /// <summary>The shape's coordinate system is unknown, so it cannot be brought to WGS84.</summary>
    UnknownSrid,

    /// <summary>Bringing the shape to WGS84 failed.</summary>
    ShapeNotConverted,

    /// <summary>
    /// The mapping's function could not read the value (e.g. «1403/13/45» for Jalali → date). The field is then left empty
    /// (or its default) and the row still fits; only a required field without a default makes the row not fit.
    /// </summary>
    FunctionFailed,
}

/// <summary>One reason a row does not fit: the field (null for the shape) and the value, for the report.</summary>
public class ImportIssue
{
    public ImportIssue(ImportIssueKind kind, string? field, object? value)
    {
        Kind = kind;
        Field = field;
        Value = value;
    }

    public ImportIssueKind Kind { get; }

    public string? Field { get; }

    public object? Value { get; }

    public override string ToString() => Field is null ? $"{Kind}: {Value}" : $"{Field}: {Kind} ({Value})";
}

/// <summary>A source row as the layer would store it, or the reasons it cannot be.</summary>
public class FeatureImportResult
{
    /// <summary>The value of every layer field (by <see cref="ImportField.Name"/>), converted to its type.</summary>
    public Dictionary<string, object?> Values { get; } = new Dictionary<string, object?>();

    /// <summary>The shape in WGS84 (SRID 4326); null for a table without shapes.</summary>
    public Geometry<Point>? Shape { get; set; }

    public List<ImportIssue> Issues { get; } = new List<ImportIssue>();

    /// <summary>The row can be written.</summary>
    public bool Fits => Issues.Count == 0;

    /// <summary>Fields whose source code was replaced by the domain's value (e.g. 1 → فولادی).</summary>
    public List<string> TranslatedCodes { get; } = new List<string>();

    /// <summary>Fields whose value was outside the domain and was written empty or as the default (<see cref="FeatureImportOptions.EmptyOutOfDomainValues"/>).</summary>
    public List<string> EmptiedFields { get; } = new List<string>();

    /// <summary>Fields whose text was cut to the field's length (<see cref="ImportFieldMapping.TruncateTooLong"/>).</summary>
    public List<string> TruncatedFields { get; } = new List<string>();

    /// <summary>Values the mapping's function could not read, written empty or as the default (the row still fits).</summary>
    public List<ImportIssue> FunctionFailures { get; } = new List<ImportIssue>();
}

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.FileGdb;

/// <summary>
/// One row of a <see cref="FileGdbDataset"/>, as returned by <see cref="FileGdbDataset.ReadFeatures"/>.
/// </summary>
public sealed class FileGdbFeature
{
    private readonly object?[] _values;

    private readonly int? _subtypeCode;

    internal FileGdbFeature(FileGdbDataset dataset, long objectId, object?[] values, Geometry<Point>? geometry, bool hasCurves)
    {
        Dataset = dataset;
        ObjectId = objectId;
        _values = values;
        Geometry = geometry;
        HasCurves = hasCurves;
        _subtypeCode = ReadSubtypeCode();
    }

    public FileGdbDataset Dataset { get; }

    /// <summary>OBJECTID; 64-bit since ArcGIS Pro 3.2.</summary>
    public long ObjectId { get; }

    /// <summary>
    /// The 2D geometry in the dataset's <see cref="FileGdbDataset.Srid"/>; <c>null</c> for a null
    /// shape or a standalone table.
    /// </summary>
    public Geometry<Point>? Geometry { get; }

    /// <summary>
    /// The stored shape had true curves (arcs, Béziers, elliptic arcs). They are returned as
    /// straight segments between their end vertices, so the geometry is an approximation.
    /// </summary>
    public bool HasCurves { get; }

    /// <summary>
    /// Stored values, parallel to <see cref="FileGdbDataset.Fields"/>; a null is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Types by field type: Int16 → <see cref="short"/>, Int32 → <see cref="int"/>,
    /// Int64 and ObjectId → <see cref="long"/>, Float32 → <see cref="float"/>, Float64 → <see cref="double"/>,
    /// String and Xml → <see cref="string"/>, DateTime → <see cref="DateTime"/>,
    /// DateOnly → <see cref="DateTime"/> (date part only), TimeOnly → <see cref="TimeSpan"/>,
    /// DateTimeWithOffset → <see cref="DateTimeOffset"/>, Guid and GlobalId → <see cref="Guid"/>,
    /// Geometry → <see cref="Geometry{Point}"/> (the same object as <see cref="Geometry"/>),
    /// Binary → <see cref="byte"/>[], Raster → <see cref="int"/>, <see cref="string"/> or <see cref="byte"/>[].
    /// </remarks>
    public IReadOnlyList<object?> Values => _values;

    public object? this[int fieldIndex] => _values[fieldIndex];

    /// <summary>The stored value of a field (case-insensitive name).</summary>
    /// <exception cref="KeyNotFoundException">The dataset has no such field.</exception>
    public object? this[string fieldName] => _values[GetFieldIndex(fieldName)];

    /// <summary>The value of the subtype field, or <c>null</c> when the dataset has no subtypes or the value is null.</summary>
    public int? SubtypeCode => _subtypeCode;

    /// <summary>The subtype named by <see cref="SubtypeCode"/>; <c>null</c> when none matches.</summary>
    public FileGdbSubtype? Subtype => _subtypeCode is int code ? Dataset.GetSubtype(code) : null;

    /// <summary>The domain that applies to a field in this row, taking the row's subtype into account.</summary>
    public FileGdbDomain? GetDomain(string fieldName) => Dataset.GetDomain(fieldName, _subtypeCode);

    /// <summary>
    /// The coded-value name of a field's value, using the domain for this row's subtype. For the
    /// subtype field itself, the subtype's name. <c>null</c> when the value is null, the field has
    /// no coded-value domain, or the value is not one of its codes.
    /// </summary>
    public string? GetCodedValueName(string fieldName)
    {
        var value = this[fieldName];

        if (value is null)
            return null;

        if (IsSubtypeField(fieldName))
            return Subtype?.Name;

        return GetDomain(fieldName) is FileGdbCodedValueDomain domain && domain.TryGetName(value, out var name) ? name : null;
    }

    /// <summary>
    /// <see cref="GetCodedValueName"/> when there is one, else the stored value. Values outside
    /// their domain are kept as stored rather than lost.
    /// </summary>
    public object? GetDisplayValue(string fieldName) => GetCodedValueName(fieldName) ?? this[fieldName];

    /// <summary>
    /// Every field except the geometry, by name, OBJECTID included. With
    /// <paramref name="resolveCodedValues"/>, coded values and the subtype code are replaced by
    /// their names (see <see cref="GetDisplayValue"/>).
    /// </summary>
    public Dictionary<string, object?> GetAttributes(bool resolveCodedValues = false)
    {
        var fields = Dataset.Fields;
        var result = new Dictionary<string, object?>(fields.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (field.FieldType == Enums.GdbFieldType.Geometry)
                continue;

            result[field.Name] = resolveCodedValues ? GetDisplayValue(field.Name) : _values[field.Index];
        }

        return result;
    }

    /// <summary>
    /// Converts to Maptor's <see cref="Feature{Point}"/>, with the attributes of
    /// <see cref="GetAttributes"/>. <see cref="Feature{T}.Id"/> is the OBJECTID when it fits in an
    /// <see cref="int"/> and 0 otherwise; the full value stays in the OBJECTID attribute.
    /// </summary>
    public Feature<Point> ToFeature(bool resolveCodedValues = false)
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in GetAttributes(resolveCodedValues))
            attributes[pair.Key] = pair.Value!;

        return new Feature<Point>(Geometry!, attributes)
        {
            Id = ObjectId is >= int.MinValue and <= int.MaxValue ? (int)ObjectId : 0,
        };
    }

    private int GetFieldIndex(string fieldName)
        => Dataset.GetField(fieldName)?.Index ?? throw new KeyNotFoundException($"Field '{fieldName}' does not exist in '{Dataset.Name}'.");

    private bool IsSubtypeField(string fieldName)
        => Dataset.SubtypeFieldName is string subtypeField && string.Equals(subtypeField, fieldName, StringComparison.OrdinalIgnoreCase);

    private int? ReadSubtypeCode()
    {
        if (Dataset.SubtypeFieldName is not string subtypeField || Dataset.GetField(subtypeField) is not FileGdbField field)
            return null;

        return _values[field.Index] switch
        {
            short code => code,
            int code => code,
            long code when code is >= int.MinValue and <= int.MaxValue => (int)code,
            _ => null,
        };
    }

    public override string ToString() => $"{Dataset.Name} OBJECTID {ObjectId}";
}

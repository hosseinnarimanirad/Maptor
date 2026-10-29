namespace IRI.Maptor.Core.FileGdb.Table;

/// <summary>
/// One decoded row. <see cref="Values"/> is parallel to <see cref="GdbTableReader.Fields"/>;
/// a SQL-style null is <c>null</c>.
/// </summary>
/// <remarks>
/// Value types per field type: Int16 → <see cref="short"/>, Int32 → <see cref="int"/>,
/// Int64 and ObjectId → <see cref="long"/>, Float32 → <see cref="float"/>, Float64 → <see cref="double"/>,
/// String and Xml → <see cref="string"/>, DateTime → <see cref="DateTime"/>,
/// DateOnly → <see cref="DateTime"/> (date part only, since netstandard2.1 has no DateOnly),
/// TimeOnly → <see cref="TimeSpan"/>, DateTimeWithOffset → <see cref="DateTimeOffset"/>,
/// Guid and GlobalId → <see cref="Guid"/>, Geometry and Binary → <see cref="byte"/>[],
/// Raster → <see cref="int"/> (managed), <see cref="string"/> (external path) or <see cref="byte"/>[] (inline).
/// </remarks>
internal sealed class GdbRow
{
    private readonly GdbTableReader _table;

    internal GdbRow(GdbTableReader table, long objectId, object?[] values)
    {
        _table = table;
        ObjectId = objectId;
        Values = values;
    }

    public long ObjectId { get; }

    public object?[] Values { get; }

    public object? this[int fieldIndex] => Values[fieldIndex];

    /// <summary>Value by field name (case-insensitive, as ArcGIS treats field names).</summary>
    public object? this[string fieldName]
    {
        get
        {
            var index = _table.GetFieldIndex(fieldName);

            if (index < 0)
                throw new KeyNotFoundException($"Field '{fieldName}' does not exist in table '{_table.Name}'.");

            return Values[index];
        }
    }
}

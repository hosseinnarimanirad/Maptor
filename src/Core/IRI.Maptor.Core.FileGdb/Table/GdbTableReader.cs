using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.FileGdb.IO;

namespace IRI.Maptor.Core.FileGdb.Table;

/// <summary>
/// Read-only access to one File Geodatabase table: a .gdbtable file (header, field descriptions,
/// row records) plus its .gdbtablx row-offset index.
/// </summary>
/// <remarks>
/// Layout reference: https://github.com/rouault/dump_gdbtable/wiki/FGDB-Spec, cross-checked
/// against GDAL's OpenFileGDB driver where the two disagree (geometry-field Z/M flag bits and the
/// default-value length byte follow GDAL). Only ArcGIS 10.x+ tables are supported; ArcGIS 9.x
/// tables and compressed (CDF/SDC) tables are rejected with <see cref="NotSupportedException"/>.
/// </remarks>
internal sealed class GdbTableReader : IDisposable
{
    private const int FileHeaderSize = 40;

    // Field description header after its int32 length: version, layer flags, field count.
    private const int FieldSectionHeaderSize = 10;

    private const int MaxFieldSectionSize = 10 * 1024 * 1024;

    private readonly FileStream _stream;

    private readonly GdbTableIndex? _index;

    private readonly Dictionary<string, int> _fieldIndexByName;

    private readonly int _nullableFieldCount;

    private byte[] _rowBuffer = new byte[256];

    private GdbTableReader(string path, string name, FileStream stream, GdbTableIndex? index)
    {
        Path = path;
        Name = name;
        _stream = stream;
        _index = index;
        FileLength = stream.Length;

        var header = new GdbByteReader(ReadAt(0, FileHeaderSize));

        Version = header.ReadInt32();

        if (Version != 3 && Version != 4)
            throw new NotSupportedException($"{path}: .gdbtable format version {Version} is not supported.");

        if (Version == 3)
        {
            ValidRowCount = header.ReadInt32();
            header.Skip(16);
        }
        else
        {
            header.Skip(12);
            ValidRowCount = header.ReadInt64();
        }

        if (ValidRowCount < 0)
            throw new InvalidDataException($"{path}: negative row count {ValidRowCount}.");

        header.Skip(8); // file size
        long fieldSectionOffset = (long)header.ReadUInt64();

        if (fieldSectionOffset < FileHeaderSize || fieldSectionOffset > FileLength - 4)
            throw new InvalidDataException($"{path}: field description offset {fieldSectionOffset} is outside the file.");

        Fields = ReadFieldSection(fieldSectionOffset);

        _fieldIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Fields.Count; i++)
        {
            _fieldIndexByName[Fields[i].Name] = i;

            if (Fields[i].IsNullable)
                _nullableFieldCount++;

            if (Fields[i].FieldType == GdbFieldType.ObjectId)
                ObjectIdFieldIndex = i;
            else if (Fields[i].FieldType == GdbFieldType.Geometry)
                GeometryFieldIndex = i;
        }
    }

    /// <summary>Full path of the .gdbtable file.</summary>
    public string Path { get; }

    /// <summary>Table name from the system catalog, or the file name when opened directly.</summary>
    public string Name { get; }

    /// <summary>3 for 32-bit OBJECTIDs, 4 for 64-bit OBJECTIDs (ArcGIS Pro 3.2+).</summary>
    public int Version { get; }

    /// <summary>Version of the field description section: 4 for ArcGIS 10.x, 6 for ArcGIS Pro 3.2 extended types.</summary>
    public int FieldSectionVersion { get; private set; }

    public long FileLength { get; }

    /// <summary>Number of live (non-deleted) rows declared in the header.</summary>
    public long ValidRowCount { get; }

    /// <summary>Highest OBJECTID slot, deleted rows included; 0 for an empty table.</summary>
    public long TotalRowCount => _index?.TotalRowCount ?? 0;

    /// <summary>False when OBJECTIDs may be shifted, see <see cref="GdbTableIndex.IsObjectIdReliable"/>.</summary>
    public bool IsObjectIdReliable => _index?.IsObjectIdReliable ?? true;

    public GdbTableGeometryType GeometryType { get; private set; }

    public bool HasZ { get; private set; }

    public bool HasM { get; private set; }

    /// <summary>String values are UTF-8 when set, UTF-16 otherwise.</summary>
    public bool StringsAreUtf8 { get; private set; }

    public IReadOnlyList<GdbFieldDescriptor> Fields { get; }

    public int ObjectIdFieldIndex { get; } = -1;

    public int GeometryFieldIndex { get; } = -1;

    /// <summary>
    /// Opens a .gdbtable file directly.
    /// </summary>
    public static GdbTableReader Open(string gdbtablePath, string? name = null)
    {
        if (File.Exists(gdbtablePath + ".cdf") || File.Exists(gdbtablePath + ".sdc"))
            throw new NotSupportedException($"{gdbtablePath}: compressed tables (CDF/SDC) are not supported. Uncompress the geodatabase in ArcGIS first.");

        var stream = new FileStream(gdbtablePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        GdbTableIndex? index = null;

        try
        {
            var version = new GdbByteReader(stream.ReadExactly(0, 4, gdbtablePath)).ReadInt32();
            var tableName = name ?? System.IO.Path.GetFileNameWithoutExtension(gdbtablePath);

            if (version != 3 && version != 4)
                throw new NotSupportedException($"{gdbtablePath}: .gdbtable format version {version} is not supported.");

            var tablxPath = System.IO.Path.ChangeExtension(gdbtablePath, ".gdbtablx");

            if (File.Exists(tablxPath))
                index = GdbTableIndex.Open(tablxPath, version);

            var reader = new GdbTableReader(gdbtablePath, tableName, stream, index);

            if (reader.ValidRowCount > 0 && index is null)
                throw new InvalidDataException($"{gdbtablePath}: the table has {reader.ValidRowCount} rows but its .gdbtablx index is missing.");

            return reader;
        }
        catch
        {
            index?.Dispose();
            stream.Dispose();
            throw;
        }
    }

    public int GetFieldIndex(string fieldName)
        => _fieldIndexByName.TryGetValue(fieldName, out var index) ? index : -1;

    /// <summary>
    /// Every live row in OBJECTID order. Throws <see cref="InvalidDataException"/> on the first
    /// corrupt row; use <see cref="EnumerateRowLocations"/> with <see cref="ReadRow(GdbRowLocation)"/>
    /// to skip bad rows instead.
    /// </summary>
    public IEnumerable<GdbRow> ReadRows()
    {
        foreach (var location in EnumerateRowLocations())
        {
            var row = ReadRow(location);

            if (row is not null)
                yield return row;
        }
    }

    /// <summary>
    /// Slots whose index offset is not 0, in OBJECTID order. A slot can still turn out to be a
    /// deleted record, in which case <see cref="ReadRow(GdbRowLocation)"/> returns <c>null</c>.
    /// </summary>
    public IEnumerable<GdbRowLocation> EnumerateRowLocations()
        => _index?.EnumerateRowLocations() ?? Enumerable.Empty<GdbRowLocation>();

    /// <summary>
    /// The row with the given OBJECTID, or <c>null</c> when it does not exist or is deleted.
    /// </summary>
    public GdbRow? ReadRow(long objectId)
    {
        long offset = _index?.GetRowOffset(objectId) ?? 0;

        return offset == 0 ? null : ReadRow(new GdbRowLocation(objectId, offset));
    }

    /// <summary>
    /// Decodes the record at <paramref name="location"/>; <c>null</c> when it is marked deleted.
    /// </summary>
    public GdbRow? ReadRow(GdbRowLocation location)
    {
        try
        {
            if (location.Offset < FileHeaderSize || location.Offset > FileLength - 4)
                throw new InvalidDataException($"row offset {location.Offset} is outside the file.");

            int length = new GdbByteReader(ReadAt(location.Offset, 4)).ReadInt32();

            // A negative length marks a deleted record whose index entry was not cleared.
            if (length < 0)
                return null;

            if (location.Offset + 4 + length > FileLength)
                throw new InvalidDataException($"row length {length} at offset {location.Offset} runs past the end of the file.");

            if (_rowBuffer.Length < length)
                _rowBuffer = new byte[Math.Max(length, _rowBuffer.Length * 2)];

            _stream.ReadExactly(location.Offset + 4, _rowBuffer, length, Path);

            return new GdbRow(this, location.ObjectId, DecodeRow(_rowBuffer.AsSpan(0, length), location.ObjectId));
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"{Name}: cannot read row with OBJECTID {location.ObjectId}: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _index?.Dispose();
        _stream.Dispose();
    }

    #region Field section

    private List<GdbFieldDescriptor> ReadFieldSection(long offset)
    {
        var lengthBytes = new GdbByteReader(ReadAt(offset, 4));
        uint sectionLength = lengthBytes.ReadUInt32();

        if (sectionLength < FieldSectionHeaderSize || sectionLength > MaxFieldSectionSize || offset + 4 + sectionLength > FileLength)
            throw new InvalidDataException($"{Path}: invalid field description length {sectionLength}.");

        var reader = new GdbByteReader(ReadAt(offset + 4, (int)sectionLength));

        FieldSectionVersion = reader.ReadInt32();

        if (FieldSectionVersion == 3)
            throw new NotSupportedException($"{Path}: ArcGIS 9.x file geodatabases are not supported. Upgrade the geodatabase in ArcGIS first.");

        byte geometryType = reader.ReadByte();
        byte encodingFlags = reader.ReadByte();
        reader.Skip(1);
        byte zmFlags = reader.ReadByte();

        GeometryType = (GdbTableGeometryType)geometryType;
        StringsAreUtf8 = (encodingFlags & 0x01) != 0;
        HasM = (zmFlags & 0x40) != 0;
        HasZ = (zmFlags & 0x80) != 0;

        int fieldCount = reader.ReadUInt16();
        var fields = new List<GdbFieldDescriptor>(fieldCount);

        for (int i = 0; i < fieldCount; i++)
        {
            var field = ReadField(ref reader);

            if (field.FieldType == GdbFieldType.ObjectId && fields.Any(f => f.FieldType == GdbFieldType.ObjectId))
                throw new InvalidDataException($"{Path}: more than one OBJECTID field.");

            if (field.FieldType == GdbFieldType.Geometry && fields.Any(f => f.FieldType == GdbFieldType.Geometry))
                throw new InvalidDataException($"{Path}: more than one geometry field.");

            fields.Add(field);
        }

        return fields;
    }

    private GdbFieldDescriptor ReadField(ref GdbByteReader reader)
    {
        var field = new GdbFieldDescriptor();

        field.Name = reader.ReadUtf16(reader.ReadByte());

        int aliasLength = reader.ReadByte();
        field.Alias = aliasLength == 0 ? null : reader.ReadUtf16(aliasLength);

        byte type = reader.ReadByte();

        if (type > (byte)GdbFieldType.DateTimeWithOffset)
            throw new InvalidDataException($"{Path}: field '{field.Name}' has unknown type {type}.");

        field.FieldType = (GdbFieldType)type;

        switch (field.FieldType)
        {
            case GdbFieldType.Geometry:
                ReadGeometryField(ref reader, field);
                return field;

            case GdbFieldType.Raster:
                ReadRasterField(ref reader, field);
                return field;
        }

        byte flags;
        int defaultLength = 0;

        switch (field.FieldType)
        {
            case GdbFieldType.String:
                field.MaxLength = reader.ReadInt32();
                if (field.MaxLength < 0)
                    throw new InvalidDataException($"{Path}: field '{field.Name}' has negative length {field.MaxLength}.");
                flags = reader.ReadByte();
                defaultLength = reader.ReadVarLength();
                break;

            case GdbFieldType.ObjectId:
            case GdbFieldType.Binary:
            case GdbFieldType.Guid:
            case GdbFieldType.GlobalId:
            case GdbFieldType.Xml:
                field.Width = reader.ReadByte();
                flags = reader.ReadByte();
                break;

            default:
                field.Width = reader.ReadByte();
                flags = reader.ReadByte();
                defaultLength = reader.ReadByte();
                break;
        }

        SetFlags(field, flags);

        // The default value bytes are only present on editable fields (GDAL, PreNIS.gdb).
        if (field.IsEditable && defaultLength > 0)
            field.DefaultValue = DecodeDefaultValue(field.FieldType, reader.ReadSpan(defaultLength));

        if (field.FieldType == GdbFieldType.ObjectId && flags != GdbFieldDescriptor.RequiredFlag)
            throw new InvalidDataException($"{Path}: OBJECTID field '{field.Name}' has unexpected flags {flags}.");

        return field;
    }

    private void ReadGeometryField(ref GdbByteReader reader, GdbFieldDescriptor field)
    {
        reader.Skip(1);
        SetFlags(field, reader.ReadByte());

        var info = new GdbGeometryFieldInfo();
        ReadSpatialReference(ref reader, info);
        ReadQuantization(ref reader, info, field.Name);

        info.XMin = reader.ReadDouble();
        info.YMin = reader.ReadDouble();
        info.XMax = reader.ReadDouble();
        info.YMax = reader.ReadDouble();

        // The Z/M extent follows the table flags, not the field's own origin/scale flags.
        if (HasZ)
        {
            info.ZMin = reader.ReadDouble();
            info.ZMax = reader.ReadDouble();
        }

        if (HasM)
        {
            info.MMin = reader.ReadDouble();
            info.MMax = reader.ReadDouble();
        }

        reader.Skip(1);
        uint gridCount = reader.ReadUInt32();

        if (gridCount > 3)
            throw new InvalidDataException($"{Path}: geometry field '{field.Name}' declares {gridCount} spatial index grids.");

        var grids = new double[gridCount];
        for (int i = 0; i < grids.Length; i++)
            grids[i] = reader.ReadDouble();

        info.SpatialIndexGridSizes = grids;
        field.Geometry = info;
    }

    private void ReadRasterField(ref GdbByteReader reader, GdbFieldDescriptor field)
    {
        reader.Skip(1);
        SetFlags(field, reader.ReadByte());

        var info = new GdbRasterFieldInfo();
        info.RasterColumnName = reader.ReadUtf16(reader.ReadByte());

        // For rasters the origin/scale block is omitted entirely when its flags byte is 0.
        ReadSpatialReference(ref reader, info);
        ReadQuantization(ref reader, info, field.Name, optional: true);

        byte storage = reader.ReadByte();
        if (storage > (byte)GdbRasterStorage.Inline)
            throw new InvalidDataException($"{Path}: raster field '{field.Name}' has unknown storage type {storage}.");

        info.Storage = (GdbRasterStorage)storage;
        field.Raster = info;
    }

    private static void ReadSpatialReference(ref GdbByteReader reader, GdbSpatialFieldInfo info)
    {
        int wktBytes = reader.ReadUInt16();
        var wkt = reader.ReadUtf16(wktBytes / 2);

        // {B286C06B-...} is the CLSID of Esri's UnknownCoordinateSystem.
        info.Wkt = string.IsNullOrEmpty(wkt) || wkt.StartsWith("{", StringComparison.Ordinal) ? null : wkt;
    }

    private void ReadQuantization(ref GdbByteReader reader, GdbSpatialFieldInfo info, string fieldName, bool optional = false)
    {
        byte flags = reader.ReadByte();

        // Bit 1 = M and bit 2 = Z (as in GDAL; the wiki has them the other way round).
        info.HasM = (flags & 0x02) != 0;
        info.HasZ = (flags & 0x04) != 0;

        if (optional && flags == 0)
            return;

        info.XOrigin = reader.ReadDouble();
        info.YOrigin = reader.ReadDouble();
        info.XYScale = reader.ReadDouble();

        if (info.XYScale == 0)
            throw new InvalidDataException($"{Path}: field '{fieldName}' has an XY scale of 0.");

        if (info.HasM)
        {
            info.MOrigin = reader.ReadDouble();
            info.MScale = reader.ReadDouble();
        }

        if (info.HasZ)
        {
            info.ZOrigin = reader.ReadDouble();
            info.ZScale = reader.ReadDouble();
        }

        info.XYTolerance = reader.ReadDouble();

        if (info.HasM)
            info.MTolerance = reader.ReadDouble();

        if (info.HasZ)
            info.ZTolerance = reader.ReadDouble();
    }

    private static void SetFlags(GdbFieldDescriptor field, byte flags)
    {
        field.IsNullable = (flags & GdbFieldDescriptor.NullableFlag) != 0;
        field.IsRequired = (flags & GdbFieldDescriptor.RequiredFlag) != 0;
        field.IsEditable = (flags & GdbFieldDescriptor.EditableFlag) != 0;
    }

    /// <summary>
    /// Defaults use the same encoding as row values but without a length prefix. A default whose
    /// size does not match its type, or whose value is invalid, is ignored (as GDAL does) rather
    /// than failing the table.
    /// </summary>
    private object? DecodeDefaultValue(GdbFieldType type, ReadOnlySpan<byte> bytes)
    {
        try
        {
            return DecodeDefaultValueCore(type, bytes);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private object? DecodeDefaultValueCore(GdbFieldType type, ReadOnlySpan<byte> bytes)
    {
        var reader = new GdbByteReader(bytes);

        switch (type)
        {
            case GdbFieldType.String:
                return StringsAreUtf8 ? reader.ReadUtf8(bytes.Length) : reader.ReadUtf16(bytes.Length / 2);
            case GdbFieldType.Int16 when bytes.Length == 2:
                return reader.ReadInt16();
            case GdbFieldType.Int32 when bytes.Length == 4:
                return reader.ReadInt32();
            case GdbFieldType.Int64 when bytes.Length == 8:
                return reader.ReadInt64();
            case GdbFieldType.Float32 when bytes.Length == 4:
                return reader.ReadSingle();
            case GdbFieldType.Float64 when bytes.Length == 8:
                return reader.ReadDouble();
            case GdbFieldType.DateTime when bytes.Length == 8:
                return GdbValueConverter.DaysToDateTime(reader.ReadDouble());
            case GdbFieldType.DateOnly when bytes.Length == 8:
                return GdbValueConverter.DaysToDateTime(reader.ReadDouble()).Date;
            case GdbFieldType.TimeOnly when bytes.Length == 8:
                return GdbValueConverter.DayFractionToTime(reader.ReadDouble());
            case GdbFieldType.DateTimeWithOffset when bytes.Length == 10:
                return GdbValueConverter.DaysToDateTimeOffset(reader.ReadDouble(), reader.ReadInt16());
            default:
                return null;
        }
    }

    #endregion

    #region Rows

    private object?[] DecodeRow(ReadOnlySpan<byte> blob, long objectId)
    {
        int nullBitmapLength = (_nullableFieldCount + 7) / 8;

        if (blob.Length < nullBitmapLength)
            throw new InvalidDataException($"record of {blob.Length} bytes is shorter than its null bitmap.");

        var nullBitmap = blob.Slice(0, nullBitmapLength);
        var reader = new GdbByteReader(blob.Slice(nullBitmapLength));
        var values = new object?[Fields.Count];
        int nullableIndex = 0;

        for (int i = 0; i < Fields.Count; i++)
        {
            var field = Fields[i];

            if (field.IsNullable)
            {
                bool isNull = (nullBitmap[nullableIndex / 8] & (1 << (nullableIndex % 8))) != 0;
                nullableIndex++;

                if (isNull)
                    continue;
            }

            values[i] = DecodeValue(ref reader, field, objectId);
        }

        return values;
    }

    private object? DecodeValue(ref GdbByteReader reader, GdbFieldDescriptor field, long objectId)
    {
        switch (field.FieldType)
        {
            case GdbFieldType.ObjectId:
                // Not stored in the record: it is the slot number in the index.
                return objectId;

            case GdbFieldType.Int16:
                return reader.ReadInt16();

            case GdbFieldType.Int32:
                return reader.ReadInt32();

            case GdbFieldType.Int64:
                return reader.ReadInt64();

            case GdbFieldType.Float32:
                return reader.ReadSingle();

            case GdbFieldType.Float64:
                return reader.ReadDouble();

            case GdbFieldType.String:
                {
                    int length = reader.ReadVarLength();
                    return StringsAreUtf8 ? reader.ReadUtf8(length) : reader.ReadUtf16(length / 2);
                }

            case GdbFieldType.Xml:
                // XML is UTF-8 regardless of the table's string encoding.
                return reader.ReadUtf8(reader.ReadVarLength());

            case GdbFieldType.DateTime:
                return GdbValueConverter.DaysToDateTime(reader.ReadDouble());

            case GdbFieldType.DateOnly:
                return GdbValueConverter.DaysToDateTime(reader.ReadDouble()).Date;

            case GdbFieldType.TimeOnly:
                return GdbValueConverter.DayFractionToTime(reader.ReadDouble());

            case GdbFieldType.DateTimeWithOffset:
                return GdbValueConverter.DaysToDateTimeOffset(reader.ReadDouble(), reader.ReadInt16());

            case GdbFieldType.Guid:
            case GdbFieldType.GlobalId:
                return GdbValueConverter.BytesToGuid(reader.ReadSpan(16));

            case GdbFieldType.Geometry:
            case GdbFieldType.Binary:
                return reader.ReadSpan(reader.ReadVarLength()).ToArray();

            case GdbFieldType.Raster:
                return DecodeRasterValue(ref reader, field.Raster!);

            default:
                throw new InvalidDataException($"field '{field.Name}' has unknown type {field.FieldType}.");
        }
    }

    private static object DecodeRasterValue(ref GdbByteReader reader, GdbRasterFieldInfo raster)
    {
        if (raster.Storage == GdbRasterStorage.Managed)
            return reader.ReadInt32();

        int length = reader.ReadVarLength();

        return raster.Storage == GdbRasterStorage.External
            ? reader.ReadUtf16(length / 2)
            : reader.ReadSpan(length).ToArray();
    }

    #endregion

    private byte[] ReadAt(long position, int count) => _stream.ReadExactly(position, count, Path);
}

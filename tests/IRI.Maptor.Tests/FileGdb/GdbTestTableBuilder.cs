using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// Writes a synthetic .gdbtable/.gdbtablx pair byte by byte, following the same layout the
/// reader expects, so edge cases absent from the sample geodatabases (ArcGIS Pro 3.2 field
/// types, 64-bit tables, sparse blocks, deleted records) can be tested.
/// </summary>
internal sealed class GdbTestTableBuilder
{
    private readonly List<byte[]> _fieldDescriptions = [];

    private readonly SortedDictionary<long, byte[]?> _records = [];

    public int Version { get; set; } = 3;

    public int FieldSectionVersion { get; set; } = 4;

    public bool Utf8 { get; set; } = true;

    public int OffsetSize { get; set; } = 4;

    /// <summary>Junk bytes before the field section, to test a field description offset other than 40.</summary>
    public int GapBeforeFields { get; set; }

    /// <summary>Write a block bitmap (version 3 only) so empty 1024-row blocks are left out.</summary>
    public bool SparseBlocks { get; set; }

    public GdbTableGeometryType GeometryType { get; set; } = GdbTableGeometryType.None;

    #region Field descriptions

    public GdbTestTableBuilder ObjectIdField(string name = "OBJECTID")
        => AddField(name, null, GdbFieldType.ObjectId, w => { w.Write((byte)(Version == 3 ? 4 : 8)); w.Write((byte)2); });

    public GdbTestTableBuilder StringField(string name, int maxLength, bool nullable, string? alias = null, string? defaultValue = null)
        => AddField(name, alias, GdbFieldType.String, w =>
        {
            w.Write(maxLength);
            var flags = Flags(nullable, defaultValue is not null);
            w.Write(flags);
            var bytes = defaultValue is null ? [] : EncodeString(defaultValue);
            WriteVarUInt(w, (ulong)bytes.Length);
            w.Write(bytes);
        });

    /// <summary>Fixed-size field: width, flags, default length byte, default bytes.</summary>
    public GdbTestTableBuilder FixedField(string name, GdbFieldType type, byte width, bool nullable, byte[]? defaultValue = null, bool editable = true)
        => AddField(name, null, type, w =>
        {
            w.Write(width);
            w.Write(Flags(nullable, editable));
            w.Write((byte)(defaultValue?.Length ?? 0));
            if (defaultValue is not null)
                w.Write(defaultValue);
        });

    /// <summary>Binary, GUID, GlobalID and XML fields: width and flags only.</summary>
    public GdbTestTableBuilder ShortField(string name, GdbFieldType type, byte width, bool nullable)
        => AddField(name, null, type, w => { w.Write(width); w.Write(Flags(nullable, true)); });

    public GdbTestTableBuilder GeometryField(string name, string? wkt, bool hasZ, bool hasM)
    {
        _tableHasZ = hasZ;
        _tableHasM = hasM;

        return AddField(name, null, GdbFieldType.Geometry, w =>
        {
            w.Write((byte)0);
            w.Write((byte)7);

            var wktBytes = Encoding.Unicode.GetBytes(wkt ?? "{B286C06B-0879-11D2-AACA-00C04FA33C20}");
            w.Write((ushort)wktBytes.Length);
            w.Write(wktBytes);

            // bit 1 = M, bit 2 = Z (GDAL's reading)
            w.Write((byte)(1 | (hasM ? 2 : 0) | (hasZ ? 4 : 0)));

            w.Write(-400.0); w.Write(-400.0); w.Write(1e9);       // x/y origin, xy scale
            if (hasM) { w.Write(-100000.0); w.Write(10000.0); }   // m origin, m scale
            if (hasZ) { w.Write(-100000.0); w.Write(10000.0); }   // z origin, z scale
            w.Write(8.983152841195215e-09);                       // xy tolerance
            if (hasM) w.Write(0.001);
            if (hasZ) w.Write(0.001);

            w.Write(44.0); w.Write(25.0); w.Write(63.5); w.Write(40.0); // extent
            if (hasZ) { w.Write(0.0); w.Write(100.0); }
            if (hasM) { w.Write(double.NaN); w.Write(double.NaN); }

            w.Write((byte)0);
            w.Write(2u);
            w.Write(0.5); w.Write(2.0);
        });
    }

    private bool _tableHasZ;

    private bool _tableHasM;

    public GdbTestTableBuilder ManagedRasterField(string name)
        => AddField(name, null, GdbFieldType.Raster, w =>
        {
            w.Write((byte)0);
            w.Write((byte)5);
            var column = Encoding.Unicode.GetBytes("Raster Column");
            w.Write((byte)(column.Length / 2));
            w.Write(column);
            w.Write((ushort)0);   // no WKT
            w.Write((byte)0);     // no origin/scale block
            w.Write((byte)GdbRasterStorage.Managed);
        });

    private GdbTestTableBuilder AddField(string name, string? alias, GdbFieldType type, Action<BinaryWriter> writeRest)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write((byte)name.Length);
        w.Write(Encoding.Unicode.GetBytes(name));
        w.Write((byte)(alias?.Length ?? 0));
        if (alias is not null)
            w.Write(Encoding.Unicode.GetBytes(alias));
        w.Write((byte)type);
        writeRest(w);

        w.Flush();
        _fieldDescriptions.Add(ms.ToArray());
        return this;
    }

    private static byte Flags(bool nullable, bool editable) => (byte)((nullable ? 1 : 0) | 2 | (editable ? 4 : 0));

    #endregion

    #region Rows

    /// <summary>Adds a live row; <paramref name="blob"/> is the null bitmap followed by the values.</summary>
    public GdbTestTableBuilder Row(long objectId, byte[] blob)
    {
        _records[objectId] = blob;
        return this;
    }

    /// <summary>A record kept in the file with a negative length while its index entry still points to it.</summary>
    public GdbTestTableBuilder DeletedRecord(long objectId, int length = 3)
    {
        _records[objectId] = null;
        _deletedLengths[objectId] = length;
        return this;
    }

    private readonly Dictionary<long, int> _deletedLengths = [];

    #endregion

    #region Value encoding helpers

    public byte[] EncodeString(string value) => Utf8 ? Encoding.UTF8.GetBytes(value) : Encoding.Unicode.GetBytes(value);

    public static void WriteVarUInt(BinaryWriter w, ulong value)
    {
        while (value >= 0x80)
        {
            w.Write((byte)(value | 0x80));
            value >>= 7;
        }

        w.Write((byte)value);
    }

    public static byte[] Record(Action<BinaryWriter> write)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        write(w);
        w.Flush();
        return ms.ToArray();
    }

    #endregion

    /// <summary>Writes <c>{name}.gdbtable</c> and (unless <paramref name="withIndex"/> is false) <c>{name}.gdbtablx</c>.</summary>
    public string Write(string folder, string name = "a00000009", bool withIndex = true)
    {
        var tablePath = Path.Combine(folder, name + ".gdbtable");

        using (var w = new BinaryWriter(File.Create(tablePath)))
        {
            w.Write(new byte[40]);
            w.Write(new byte[GapBeforeFields]);

            long fieldSectionOffset = 40 + GapBeforeFields;
            var fields = _fieldDescriptions.SelectMany(f => f).ToArray();

            w.Write((uint)(10 + fields.Length));
            w.Write(FieldSectionVersion);
            w.Write((byte)GeometryType);
            w.Write((byte)(Utf8 ? 1 : 0));
            w.Write((byte)0);
            w.Write((byte)((_tableHasM ? 0x40 : 0) | (_tableHasZ ? 0x80 : 0)));
            w.Write((ushort)_fieldDescriptions.Count);
            w.Write(fields);

            var offsets = new Dictionary<long, long>();
            int maxRow = 0;

            foreach (var (objectId, blob) in _records)
            {
                offsets[objectId] = w.BaseStream.Position;

                if (blob is null)
                {
                    int length = _deletedLengths[objectId];
                    w.Write(-length);
                    w.Write(new byte[length]);
                }
                else
                {
                    w.Write(blob.Length);
                    w.Write(blob);
                    maxRow = Math.Max(maxRow, blob.Length);
                }
            }

            long fileSize = w.BaseStream.Position;
            long validRows = _records.Count(r => r.Value is not null);

            w.BaseStream.Position = 0;
            w.Write(Version);
            if (Version == 3)
            {
                w.Write((int)validRows);
                w.Write(maxRow);
                w.Write(5);
                w.Write(0);
                w.Write(0);
            }
            else
            {
                w.Write(_deletedLengths.Count > 0 ? 1 : 0);
                w.Write(maxRow);
                w.Write(5);
                w.Write(validRows);
            }
            w.Write(fileSize);
            w.Write(fieldSectionOffset);

            if (withIndex)
                WriteIndex(Path.Combine(folder, name + ".gdbtablx"), offsets);
        }

        return tablePath;
    }

    private void WriteIndex(string path, Dictionary<long, long> offsets)
    {
        long totalRows = offsets.Count == 0 ? 0 : offsets.Keys.Max();
        long totalBlocks = (totalRows + 1023) / 1024;

        var presentBlocks = Enumerable.Range(0, (int)totalBlocks)
            .Where(b => !SparseBlocks || offsets.Keys.Any(oid => (oid - 1) / 1024 == b))
            .ToList();

        using var w = new BinaryWriter(File.Create(path));

        w.Write(Version);
        if (Version == 3)
        {
            w.Write(presentBlocks.Count);
            w.Write((int)totalRows);
        }
        else
        {
            w.Write((long)presentBlocks.Count);
        }
        w.Write(OffsetSize);

        foreach (var block in presentBlocks)
        {
            for (int i = 0; i < 1024; i++)
            {
                long objectId = block * 1024L + i + 1;
                long offset = offsets.TryGetValue(objectId, out var o) ? o : 0;

                for (int b = 0; b < OffsetSize; b++)
                    w.Write((byte)(offset >> (8 * b)));
            }
        }

        if (presentBlocks.Count == 0)
            return;

        if (Version == 3)
        {
            if (SparseBlocks)
            {
                int words = (int)((totalBlocks + 31) / 32);
                w.Write(words);
                w.Write((int)totalBlocks);
                w.Write(presentBlocks.Count);
                w.Write(words);

                var bitmap = new byte[words * 4];
                foreach (var block in presentBlocks)
                    bitmap[block / 8] |= (byte)(1 << (block % 8));
                w.Write(bitmap);
            }
            else
            {
                w.Write(0);
                w.Write(presentBlocks.Count);
                w.Write(presentBlocks.Count);
                w.Write(0);
            }
        }
        else
        {
            w.Write(totalRows);
            w.Write(0u);
            w.Write((long)presentBlocks.Count - 1);
        }
    }
}

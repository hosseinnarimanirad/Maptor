using System.IO;
using System.Linq;

using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.FileGdb.IO;
using IRI.Maptor.Core.FileGdb.Table;

namespace IRI.Maptor.Tests.FileGdb;

public sealed class GdbTableReaderTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor_filegdb_" + Guid.NewGuid().ToString("N"));

    public GdbTableReaderTest() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    #region Byte reader and value conversion

    [Theory]
    [InlineData(new byte[] { 0x00 }, 0UL)]
    [InlineData(new byte[] { 0x7F }, 127UL)]
    [InlineData(new byte[] { 0x80, 0x01 }, 128UL)]
    [InlineData(new byte[] { 0xE5, 0x8E, 0x26 }, 624485UL)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 }, ulong.MaxValue)]
    public void VarUInt_Decodes(byte[] bytes, ulong expected)
    {
        var reader = new GdbByteReader(bytes);

        Assert.Equal(expected, reader.ReadVarUInt64());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void VarUInt_RejectsOverflowAndTruncation()
    {
        Assert.Throws<InvalidDataException>(() => new GdbByteReader(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 }).ReadVarUInt64());
        Assert.Throws<InvalidDataException>(() => new GdbByteReader(new byte[] { 0x80, 0x80 }).ReadVarUInt64());
        Assert.Throws<InvalidDataException>(() => new GdbByteReader(new byte[] { 0x05, 0x01 }).ReadVarLength());
    }

    [Theory]
    [InlineData(0.0, "1899-12-30T00:00:00.000")]
    [InlineData(25569.0, "1970-01-01T00:00:00.000")]
    [InlineData(45000.5, "2023-03-15T12:00:00.000")]
    [InlineData(-1.5, "1899-12-28T12:00:00.000")] // linear, unlike DateTime.FromOADate
    public void Dates_AreLinearDaysSince1899(double days, string expected)
    {
        Assert.Equal(expected, GdbValueConverter.DaysToDateTime(days).ToString("yyyy-MM-ddTHH:mm:ss.fff"));
    }

    [Fact]
    public void Dates_RejectInvalidValues()
    {
        Assert.Throws<InvalidDataException>(() => GdbValueConverter.DaysToDateTime(double.NaN));
        Assert.Throws<InvalidDataException>(() => GdbValueConverter.DaysToDateTime(1e10));
        Assert.Throws<InvalidDataException>(() => GdbValueConverter.DayFractionToTime(1.5));
        Assert.Throws<InvalidDataException>(() => GdbValueConverter.DaysToDateTimeOffset(0, 15 * 60));
    }

    [Fact]
    public void Guid_UsesComByteOrder()
    {
        // GDAL: "78563412BC9AF0DE1234567890ABCDEF" --> {12345678-9ABC-DEF0-1234-567890ABCDEF}
        var bytes = new byte[] { 0x78, 0x56, 0x34, 0x12, 0xBC, 0x9A, 0xF0, 0xDE, 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };

        Assert.Equal("{12345678-9ABC-DEF0-1234-567890ABCDEF}", GdbValueConverter.BytesToGuid(bytes).ToString("B").ToUpperInvariant());
    }

    #endregion

    #region Synthetic tables

    [Fact]
    public void ReadsEveryFieldType()
    {
        var guid = new Guid("{12345678-9ABC-DEF0-1234-567890ABCDEF}");
        var geometry = new byte[] { 1, 0x81, 0x01, 0x05 };

        var builder = new GdbTestTableBuilder { OffsetSize = 5, GapBeforeFields = 37, GeometryType = GdbTableGeometryType.Point }
            .ObjectIdField()
            .GeometryField("SHAPE", "GEOGCS[\"GCS_WGS_1984\"]", hasZ: false, hasM: false)
            .FixedField("I16", GdbFieldType.Int16, 2, nullable: false)
            .FixedField("I32", GdbFieldType.Int32, 4, nullable: false)
            .FixedField("F32", GdbFieldType.Float32, 4, nullable: false)
            .FixedField("F64", GdbFieldType.Float64, 8, nullable: false)
            .StringField("Name", 50, nullable: false, alias: "نام")
            .FixedField("When", GdbFieldType.DateTime, 8, nullable: false)
            .ShortField("Id", GdbFieldType.Guid, 38, nullable: false)
            .ShortField("GlobalID", GdbFieldType.GlobalId, 38, nullable: false)
            .ShortField("Doc", GdbFieldType.Xml, 0, nullable: false)
            .ShortField("Blob", GdbFieldType.Binary, 0, nullable: false)
            .FixedField("Big", GdbFieldType.Int64, 8, nullable: false)
            .FixedField("Day", GdbFieldType.DateOnly, 8, nullable: false)
            .FixedField("Time", GdbFieldType.TimeOnly, 8, nullable: false)
            .FixedField("Stamp", GdbFieldType.DateTimeWithOffset, 10, nullable: false)
            .ManagedRasterField("Img");

        // SHAPE (flags 7) and Img (flags 5) are the nullable fields: bits 0 and 1, both present
        builder.Row(1, GdbTestTableBuilder.Record(w =>
        {
            w.Write((byte)0b1111_1100);
            GdbTestTableBuilder.WriteVarUInt(w, (ulong)geometry.Length); w.Write(geometry);
            w.Write((short)-7);
            w.Write(123456);
            w.Write(1.5f);
            w.Write(Math.PI);
            var name = builder.EncodeString("تهران");
            GdbTestTableBuilder.WriteVarUInt(w, (ulong)name.Length); w.Write(name);
            w.Write(45000.5);
            w.Write(guid.ToByteArray());
            w.Write(guid.ToByteArray());
            var xml = System.Text.Encoding.UTF8.GetBytes("<a/>");
            GdbTestTableBuilder.WriteVarUInt(w, (ulong)xml.Length); w.Write(xml);
            GdbTestTableBuilder.WriteVarUInt(w, 3); w.Write(new byte[] { 9, 8, 7 });
            w.Write(9_000_000_000L);
            w.Write(45000.0);
            w.Write(0.75);
            w.Write(45000.25); w.Write((short)210);
            w.Write(42);
        }));

        using var table = GdbTableReader.Open(builder.Write(_folder), "AllTypes");

        Assert.Equal(17, table.Fields.Count);
        Assert.Equal(0, table.ObjectIdFieldIndex);
        Assert.Equal(1, table.GeometryFieldIndex);
        Assert.Equal(GdbTableGeometryType.Point, table.GeometryType);
        Assert.Equal("نام", table.Fields[6].Alias);
        Assert.Equal(50, table.Fields[6].MaxLength);
        Assert.Null(table.Fields[2].Alias);

        var shape = table.Fields[1].Geometry!;
        Assert.Equal("GEOGCS[\"GCS_WGS_1984\"]", shape.Wkt);
        Assert.Equal(1e9, shape.XYScale);
        Assert.Equal(-400.0, shape.XOrigin);
        Assert.Equal(new[] { 0.5, 2.0 }, shape.SpatialIndexGridSizes);
        Assert.Equal(GdbRasterStorage.Managed, table.Fields[16].Raster!.Storage);

        var row = Assert.Single(table.ReadRows());

        Assert.Equal(1L, row.ObjectId);
        Assert.Equal(1L, row["OBJECTID"]);
        Assert.Equal(geometry, row["shape"]);
        Assert.Equal((short)-7, row["I16"]);
        Assert.Equal(123456, row["I32"]);
        Assert.Equal(1.5f, row["F32"]);
        Assert.Equal(Math.PI, row["F64"]);
        Assert.Equal("تهران", row["Name"]);
        Assert.Equal(new DateTime(2023, 3, 15, 12, 0, 0), row["When"]);
        Assert.Equal(guid, row["Id"]);
        Assert.Equal(guid, row["GlobalID"]);
        Assert.Equal("<a/>", row["Doc"]);
        Assert.Equal(new byte[] { 9, 8, 7 }, row["Blob"]);
        Assert.Equal(9_000_000_000L, row["Big"]);
        Assert.Equal(new DateTime(2023, 3, 15), row["Day"]);
        Assert.Equal(TimeSpan.FromHours(18), row["Time"]);
        Assert.Equal(new DateTimeOffset(2023, 3, 15, 6, 0, 0, TimeSpan.FromMinutes(210)), row["Stamp"]);
        Assert.Equal(42, row["Img"]);
    }

    [Fact]
    public void NullBitmap_SkipsOnlyNullableFields()
    {
        var builder = new GdbTestTableBuilder()
            .ObjectIdField()
            .FixedField("A", GdbFieldType.Int32, 4, nullable: true)
            .FixedField("B", GdbFieldType.Int32, 4, nullable: false)
            .StringField("C", 10, nullable: true);

        builder.Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1111_1110); w.Write(11); w.Write(22); }));   // bit 1 set: C null
        builder.Row(2, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1111_1101); w.Write(33); w.Write((byte)1); w.Write((byte)'x'); })); // bit 0 set: A null
        builder.Row(3, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1111_1111); w.Write(44); }));                // A and C null

        using var table = GdbTableReader.Open(builder.Write(_folder));
        var rows = table.ReadRows().ToList();

        Assert.Equal(new object?[] { 1L, 11, 22, null }, rows[0].Values);
        Assert.Equal(new object?[] { 2L, null, 33, "x" }, rows[1].Values);
        Assert.Equal(new object?[] { 3L, null, 44, null }, rows[2].Values);
    }

    [Fact]
    public void DefaultValues_AreDecoded()
    {
        var builder = new GdbTestTableBuilder()
            .ObjectIdField()
            .StringField("S", 10, nullable: true, defaultValue: "abc")
            .FixedField("I", GdbFieldType.Int32, 4, nullable: true, defaultValue: BitConverter.GetBytes(7))
            .FixedField("D", GdbFieldType.DateTime, 8, nullable: true, defaultValue: BitConverter.GetBytes(25569.0))
            .FixedField("Bad", GdbFieldType.DateTime, 8, nullable: true, defaultValue: BitConverter.GetBytes(double.NaN))
            .FixedField("Odd", GdbFieldType.Int32, 4, nullable: true, defaultValue: new byte[] { 1, 2 })
            .FixedField("After", GdbFieldType.Int16, 2, nullable: true);

        using var table = GdbTableReader.Open(builder.Write(_folder));

        Assert.Equal("abc", table.Fields[1].DefaultValue);
        Assert.Equal(7, table.Fields[2].DefaultValue);
        Assert.Equal(new DateTime(1970, 1, 1), table.Fields[3].DefaultValue);
        Assert.Null(table.Fields[4].DefaultValue); // invalid value ignored, table still opens
        Assert.Null(table.Fields[5].DefaultValue); // size mismatch ignored, bytes still skipped
        Assert.Equal("After", table.Fields[6].Name);
        Assert.Empty(table.ReadRows());
    }

    [Fact]
    public void Utf16Strings_AreDecoded()
    {
        var builder = new GdbTestTableBuilder { Utf8 = false }
            .ObjectIdField()
            .StringField("Name", 20, nullable: false, defaultValue: "پیش");

        var value = builder.EncodeString("سلام");
        builder.Row(1, GdbTestTableBuilder.Record(w => { GdbTestTableBuilder.WriteVarUInt(w, (ulong)value.Length); w.Write(value); }));

        using var table = GdbTableReader.Open(builder.Write(_folder));

        Assert.False(table.StringsAreUtf8);
        Assert.Equal("پیش", table.Fields[1].DefaultValue);
        Assert.Equal("سلام", table.ReadRows().Single()["Name"]);
    }

    [Theory]
    [InlineData(3, 4, false)]
    [InlineData(3, 6, false)]
    [InlineData(3, 5, true)]
    [InlineData(4, 5, false)]
    public void DeletedAndSparseRows_KeepTheirObjectIds(int version, int offsetSize, bool sparse)
    {
        var builder = new GdbTestTableBuilder { Version = version, OffsetSize = offsetSize, SparseBlocks = sparse }
            .ObjectIdField()
            .FixedField("V", GdbFieldType.Int32, 4, nullable: false);

        foreach (var objectId in new long[] { 1, 2, 4, 2049, 2050 })
            builder.Row(objectId, GdbTestTableBuilder.Record(w => w.Write((int)objectId * 10)));

        builder.DeletedRecord(5);

        using var table = GdbTableReader.Open(builder.Write(_folder));

        Assert.Equal(version, table.Version);
        Assert.Equal(5, table.ValidRowCount);
        Assert.Equal(2050, table.TotalRowCount);
        Assert.True(table.IsObjectIdReliable);

        var rows = table.ReadRows().ToList();
        Assert.Equal(new long[] { 1, 2, 4, 2049, 2050 }, rows.Select(r => r.ObjectId));
        Assert.All(rows, r => Assert.Equal((int)r.ObjectId * 10, r["V"]));

        Assert.Equal(20490, table.ReadRow(2049)!["V"]);
        Assert.Null(table.ReadRow(3));    // never written
        Assert.Null(table.ReadRow(5));    // negative-length record
        Assert.Null(table.ReadRow(1500)); // inside a block that is left out when sparse
        Assert.Null(table.ReadRow(9999)); // past the end
    }

    [Fact]
    public void EmptyTable_HasNoRows()
    {
        var builder = new GdbTestTableBuilder().ObjectIdField();

        using var table = GdbTableReader.Open(builder.Write(_folder));

        Assert.Equal(0, table.TotalRowCount);
        Assert.Empty(table.ReadRows());
    }

    [Fact]
    public void CorruptRow_ReportsObjectId()
    {
        var builder = new GdbTestTableBuilder()
            .ObjectIdField()
            .FixedField("V", GdbFieldType.Int64, 8, nullable: false);

        builder.Row(1, GdbTestTableBuilder.Record(w => w.Write(1L)));
        builder.Row(2, GdbTestTableBuilder.Record(w => w.Write(2)));   // 4 bytes for an 8-byte field
        builder.Row(3, GdbTestTableBuilder.Record(w => w.Write(3L)));

        using var table = GdbTableReader.Open(builder.Write(_folder), "Broken");

        var ex = Assert.Throws<InvalidDataException>(() => table.ReadRows().ToList());
        Assert.Contains("OBJECTID 2", ex.Message);

        // callers that want to skip bad rows read location by location
        var readable = table.EnumerateRowLocations()
            .Select(l => { try { return table.ReadRow(l); } catch (InvalidDataException) { return null; } })
            .Where(r => r is not null)
            .Select(r => r!.ObjectId);

        Assert.Equal(new long[] { 1, 3 }, readable);
    }

    [Fact]
    public void UnsupportedTables_AreRejected()
    {
        var plain = new GdbTestTableBuilder().ObjectIdField().FixedField("V", GdbFieldType.Int32, 4, nullable: false);
        plain.Row(1, GdbTestTableBuilder.Record(w => w.Write(1)));

        var compressed = plain.Write(_folder, "a0000000a");
        File.WriteAllBytes(compressed + ".cdf", new byte[] { 0 });
        Assert.Throws<NotSupportedException>(() => GdbTableReader.Open(compressed));

        var v9 = new GdbTestTableBuilder { FieldSectionVersion = 3 }.ObjectIdField().Write(_folder, "a0000000b");
        Assert.Throws<NotSupportedException>(() => GdbTableReader.Open(v9));

        var noIndex = plain.Write(_folder, "a0000000c", withIndex: false);
        Assert.Throws<InvalidDataException>(() => GdbTableReader.Open(noIndex));
    }

    [Fact]
    public void SystemCatalog_MapsNamesToFiles()
    {
        var builder = new GdbTestTableBuilder()
            .ObjectIdField("ID")
            .StringField("Name", 160, nullable: false)
            .FixedField("FileFormat", GdbFieldType.Int32, 4, nullable: false);

        void Add(long objectId, string name)
        {
            var bytes = builder.EncodeString(name);
            builder.Row(objectId, GdbTestTableBuilder.Record(w => { GdbTestTableBuilder.WriteVarUInt(w, (ulong)bytes.Length); w.Write(bytes); w.Write(0); }));
        }

        Add(1, "GDB_SystemCatalog");
        Add(4, "GDB_Items");
        Add(10, "Parcels");
        Add(11, "Roads");
        builder.Write(_folder, "a00000001");

        new GdbTestTableBuilder().ObjectIdField().Write(_folder, "a0000000a");
        new GdbTestTableBuilder().ObjectIdField().Write(_folder, "a0000000b");
        File.WriteAllBytes(Path.Combine(_folder, "a0000000b.gdbtable.cdf"), new byte[] { 0 });

        var entries = GdbSystemCatalog.Read(_folder);

        Assert.Equal(new[] { "GDB_SystemCatalog", "GDB_Items", "Parcels", "Roads" }, entries.Select(e => e.Name));
        Assert.Equal(new long[] { 1, 4, 10, 11 }, entries.Select(e => e.TableNumber));
        Assert.Equal("a0000000a.gdbtable", Path.GetFileName(entries[2].TablePath));
        Assert.Equal(new[] { true, true, false, false }, entries.Select(e => e.IsSystemTable));
        Assert.Equal(GdbTableStorage.Missing, entries[1].Storage);
        Assert.Equal(GdbTableStorage.Table, entries[2].Storage);
        Assert.Equal(GdbTableStorage.Compressed, entries[3].Storage);

        Assert.Throws<DirectoryNotFoundException>(() => GdbSystemCatalog.Read(Path.Combine(_folder, "nope.gdb")));
    }

    #endregion
}

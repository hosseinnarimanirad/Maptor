using System.Collections.Generic;
using System.IO;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// The public API (<see cref="FileGeodatabase"/>, <see cref="FileGdbDataset"/>, <see cref="FileGdbFeature"/>)
/// on a synthetic geodatabase with domains, subtypes, a relationship class, a corrupt row, a
/// compressed table and a multipatch table.
/// </summary>
public sealed class FileGeodatabaseTest : IDisposable
{
    private const string Ns = GdbTestGeodatabase.Ns;

    private const string Wgs84Wkt = "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor_filegdb_api_" + Guid.NewGuid().ToString("N"));

    public FileGeodatabaseTest()
    {
        Directory.CreateDirectory(_folder);
        WriteGeodatabase();
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    #region Synthetic geodatabase

    private void WriteGeodatabase()
    {
        GdbTestGeodatabase.WriteSystemCatalog(_folder,
            (1, "GDB_SystemCatalog"), (4, "GDB_Items"),
            (9, "Parcels"), (10, "Owners"), (11, "Archive"), (12, "Patches"));

        var parcels =
            $"<DEFeatureClassInfo xsi:type='typens:DEFeatureClassInfo' {Ns}><CatalogPath>\\Cadastre\\Parcels</CatalogPath><Name>Parcels</Name><AliasName>قطعات</AliasName>" +
            "<GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'>" +
            "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>Use</Name><DomainName>LandUse</DomainName></GPFieldInfoEx>" +
            "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>Name</Name><AliasName>نام</AliasName></GPFieldInfoEx>" +
            "</GPFieldInfoExs>" +
            "<SubtypeFieldName>Kind</SubtypeFieldName><DefaultSubtypeCode>1</DefaultSubtypeCode><Subtypes xsi:type='typens:ArrayOfSubtype'>" +
            "<Subtype xsi:type='typens:Subtype'><SubtypeName>Residential</SubtypeName><SubtypeCode>1</SubtypeCode><FieldInfos xsi:type='typens:ArrayOfSubtypeFieldInfo'>" +
            "<SubtypeFieldInfo xsi:type='typens:SubtypeFieldInfo'><FieldName>Use</FieldName><DomainName>ResidentialUse</DomainName></SubtypeFieldInfo></FieldInfos></Subtype>" +
            "<Subtype xsi:type='typens:Subtype'><SubtypeName>Commercial</SubtypeName><SubtypeCode>2</SubtypeCode><FieldInfos xsi:type='typens:ArrayOfSubtypeFieldInfo'/></Subtype>" +
            "</Subtypes><ShapeType>esriGeometryPoint</ShapeType><ShapeFieldName>SHAPE</ShapeFieldName>" +
            "<SpatialReference xsi:type='typens:GeographicCoordinateSystem'><WKID>4326</WKID><LatestWKID>4326</LatestWKID></SpatialReference></DEFeatureClassInfo>";

        var owners =
            $"<DETableInfo xsi:type='typens:DETableInfo' {Ns}><CatalogPath>\\Owners</CatalogPath><Name>Owners</Name><GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'/></DETableInfo>";

        var ownership =
            $"<DERelationshipClassInfo xsi:type='typens:DERelationshipClassInfo' {Ns}><Name>Ownership</Name><Cardinality>esriRelCardinalityOneToMany</Cardinality>" +
            "<OriginClassNames xsi:type='typens:Names'><Name>Parcels</Name></OriginClassNames><DestinationClassNames xsi:type='typens:Names'><Name>Owners</Name></DestinationClassNames>" +
            "<IsAttachmentRelationship>false</IsAttachmentRelationship></DERelationshipClassInfo>";

        static string Domain(string name, params (short Code, string Name)[] values) =>
            $"<GPCodedValueDomain2 xsi:type='typens:GPCodedValueDomain2' {Ns}><DomainName>{name}</DomainName><CodedValues xsi:type='typens:ArrayOfCodedValue'>" +
            string.Concat(values.Select(v => $"<CodedValue><Name>{v.Name}</Name><Code xsi:type='xs:short'>{v.Code}</Code></CodedValue>")) +
            "</CodedValues></GPCodedValueDomain2>";

        var archive =
            $"<DETableInfo xsi:type='typens:DETableInfo' {Ns}><CatalogPath>\\Archive</CatalogPath><Name>Archive</Name><GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'/></DETableInfo>";

        GdbTestGeodatabase.WriteItems(_folder,
            (GdbItemTypes.FeatureDataset, "Cadastre", @"\Cadastre", $"<DEFeatureDataset xsi:type='typens:DEFeatureDataset' {Ns}><Name>Cadastre</Name></DEFeatureDataset>"),
            (GdbItemTypes.FeatureClass, "Parcels", @"\Cadastre\Parcels", parcels),
            (GdbItemTypes.Table, "Owners", @"\Owners", owners),
            (GdbItemTypes.Table, "Archive", @"\Archive", archive),
            (GdbItemTypes.RelationshipClass, "Ownership", @"\Ownership", ownership),
            (GdbItemTypes.CodedValueDomain, "LandUse", "", Domain("LandUse", (1, "مسکونی"), (2, "تجاری"))),
            (GdbItemTypes.CodedValueDomain, "ResidentialUse", "", Domain("ResidentialUse", (1, "Apartment"), (2, "Villa"))));

        WriteParcels();

        new GdbTestTableBuilder()
            .ObjectIdField()
            .StringField("OwnerName", 50, nullable: true)
            .Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0); GdbTestGeodatabase.WriteVarString(w, "Ali"); }))
            .Write(_folder, "a0000000a");

        // a compressed table: listed, not readable
        new GdbTestTableBuilder().ObjectIdField().Write(_folder, "a0000000b");
        File.WriteAllBytes(Path.Combine(_folder, "a0000000b.gdbtable.cdf"), new byte[] { 0 });

        // a multipatch feature class with no item definition
        new GdbTestTableBuilder { GeometryType = GdbTableGeometryType.Multipatch }
            .ObjectIdField()
            .GeometryField("SHAPE", Wgs84Wkt, hasZ: true, hasM: false)
            .Write(_folder, "a0000000c");
    }

    private void WriteParcels()
    {
        // nullable fields in order: SHAPE (bit 0), Use (bit 1), Kind (bit 2), Name (bit 3)
        var builder = new GdbTestTableBuilder { GeometryType = GdbTableGeometryType.Point }
            .ObjectIdField()
            .GeometryField("SHAPE", Wgs84Wkt, hasZ: false, hasM: false)
            .FixedField("Use", GdbFieldType.Int16, 2, nullable: true)
            .FixedField("Kind", GdbFieldType.Int32, 4, nullable: true)
            .StringField("Name", 50, nullable: true, alias: "StoredAlias");

        builder.Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0); WritePoint(w, 50, 30); w.Write((short)1); w.Write(1); GdbTestGeodatabase.WriteVarString(w, "a"); }));
        builder.Row(2, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1000); WritePoint(w, 60, 35); w.Write((short)2); w.Write(2); }));
        builder.Row(3, GdbTestTableBuilder.Record(w => { w.Write((byte)0b0101); w.Write((short)9); GdbTestGeodatabase.WriteVarString(w, "c"); }));

        // unknown shape type 99
        builder.Row(4, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1100); w.Write((byte)1); w.Write((byte)99); w.Write((short)1); }));

        builder.Write(_folder, "a00000009");
    }

    /// <summary>A point shape as stored with the builder's grid: origin -400, scale 1e9, value + 1.</summary>
    private static void WritePoint(BinaryWriter w, double x, double y)
    {
        var shape = GdbTestTableBuilder.Record(s =>
        {
            GdbTestTableBuilder.WriteVarUInt(s, 1);
            GdbTestTableBuilder.WriteVarUInt(s, (ulong)Math.Round((x + 400) * 1e9) + 1);
            GdbTestTableBuilder.WriteVarUInt(s, (ulong)Math.Round((y + 400) * 1e9) + 1);
        });

        GdbTestTableBuilder.WriteVarUInt(w, (ulong)shape.Length);
        w.Write(shape);
    }

    #endregion

    [Fact]
    public void Open_ListsDatasets_WithCatalogMetadata()
    {
        var gdb = FileGeodatabase.Open(_folder);

        Assert.Empty(gdb.Warnings);
        Assert.Equal(new[] { "Parcels", "Owners", "Archive", "Patches" }, gdb.Datasets.Select(d => d.Name));
        Assert.Equal("Cadastre", Assert.Single(gdb.FeatureDatasets).Name);
        Assert.Equal(2, gdb.Domains.Count);
        Assert.Equal("Ownership", Assert.Single(gdb.RelationshipClasses).Name);

        var parcels = gdb.GetDataset("PARCELS")!;
        Assert.Same(gdb, parcels.Geodatabase);
        Assert.Equal("قطعات", parcels.AliasName);
        Assert.Equal(GdbDatasetKind.FeatureClass, parcels.Kind);
        Assert.Equal("Cadastre", parcels.FeatureDatasetName);
        Assert.Equal(GdbTableStorage.Table, parcels.Storage);
        Assert.Equal(new[] { "1 = Residential", "2 = Commercial" }, parcels.Subtypes.Select(s => s.ToString()));
        Assert.Equal("Kind", parcels.SubtypeFieldName);
        Assert.Equal("Ownership", Assert.Single(parcels.RelationshipClasses).Name);
        Assert.Equal("Ownership", Assert.Single(gdb.GetDataset("Owners")!.RelationshipClasses).Name);

        Assert.IsType<FileGdbCodedValueDomain>(gdb.GetDomain("landuse"));
        Assert.Null(gdb.GetDataset("Nothing"));
    }

    [Fact]
    public void Schema_MergesPhysicalFieldsWithDefinition()
    {
        var parcels = FileGeodatabase.Open(_folder).GetDataset("Parcels")!;

        Assert.Equal(new[] { "OBJECTID", "SHAPE", "Use", "Kind", "Name" }, parcels.Fields.Select(f => f.Name));
        Assert.Equal(Enumerable.Range(0, 5), parcels.Fields.Select(f => f.Index));
        Assert.Equal("OBJECTID", parcels.ObjectIdFieldName);
        Assert.Equal("SHAPE", parcels.GeometryFieldName);
        Assert.Equal(4, parcels.RowCount);
        Assert.Equal(GdbTableGeometryType.Point, parcels.GeometryType);
        Assert.False(parcels.HasZ);
        Assert.True(parcels.IsObjectIdReliable);
        Assert.Equal(4326, parcels.Srid);
        Assert.Equal(new BoundingBox(44, 25, 63.5, 40), parcels.Extent);

        var name = parcels.GetField("name")!;
        Assert.Equal("نام", name.AliasName);   // the definition's alias wins over the stored one
        Assert.Equal(GdbFieldType.String, name.FieldType);
        Assert.Equal(50, name.Length);
        Assert.True(name.IsNullable);

        var use = parcels.GetField("Use")!;
        Assert.Equal("LandUse", use.DomainName);
        Assert.Equal("LandUse", use.Domain!.Name);
        Assert.Equal("ResidentialUse", parcels.GetDomain("Use", 1)!.Name);
        Assert.Equal("LandUse", parcels.GetDomain("Use", 2)!.Name);

        Assert.Null(parcels.GetField("Kind")!.AliasName);
    }

    [Fact]
    public void ReadFeatures_ResolvesSubtypeAwareCodedValues()
    {
        var errors = new List<FileGdbRowError>();
        var features = FileGeodatabase.Open(_folder).GetDataset("Parcels")!
            .ReadFeatures(new FileGdbReadOptions { OnRowError = errors.Add })
            .ToList();

        Assert.Equal(new long[] { 1, 2, 3 }, features.Select(f => f.ObjectId));

        // OBJECTID 4 has an unknown shape type: reported and skipped
        var error = Assert.Single(errors);
        Assert.Equal(4, error.ObjectId);
        Assert.Equal("Parcels", error.DatasetName);
        Assert.Contains("OBJECTID 4", error.Message);
        Assert.IsType<InvalidDataException>(error.Exception);

        var first = features[0];
        var point = first.Geometry!;
        Assert.Equal(GeometryType.Point, point.Type);
        Assert.Equal(4326, point.Srid);
        var location = point.Points![0];
        Assert.Equal(50, location.X, 6);
        Assert.Equal(30, location.Y, 6);
        Assert.False(first.HasCurves);
        Assert.Same(first.Geometry, first["shape"]);
        Assert.Equal((short)1, first["Use"]);
        Assert.Equal(1, first.SubtypeCode);
        Assert.Equal("Residential", first.Subtype!.Name);
        Assert.Equal("Apartment", first.GetCodedValueName("Use"));   // subtype 1 has its own domain
        Assert.Equal("Residential", first.GetCodedValueName("Kind"));
        Assert.Equal("a", first.GetDisplayValue("Name"));

        var second = features[1];
        Assert.Equal("تجاری", second.GetCodedValueName("Use"));      // subtype 2 falls back to the field's domain
        Assert.Null(second["Name"]);

        var third = features[2];
        Assert.Null(third.Geometry);
        Assert.Null(third.SubtypeCode);
        Assert.Null(third.GetCodedValueName("Use"));                  // 9 is not a code
        Assert.Equal((short)9, third.GetDisplayValue("Use"));         // kept as stored

        Assert.Throws<KeyNotFoundException>(() => first["Missing"]);
    }

    [Fact]
    public void ReadFeatures_WithoutErrorHandler_ThrowsAtTheBadRow()
    {
        var parcels = FileGeodatabase.Open(_folder).GetDataset("Parcels")!;
        var read = new List<long>();

        var ex = Assert.Throws<InvalidDataException>(() =>
        {
            foreach (var feature in parcels.ReadFeatures())
                read.Add(feature.ObjectId);
        });

        Assert.Equal(new long[] { 1, 2, 3 }, read);
        Assert.Contains("OBJECTID 4", ex.Message);
    }

    [Fact]
    public void ReadFeatures_BoundingBoxFilter()
    {
        var parcels = FileGeodatabase.Open(_folder).GetDataset("Parcels")!;

        var options = new FileGdbReadOptions { BoundingBox = new BoundingBox(49, 29, 51, 31), OnRowError = _ => { } };

        // OBJECTID 2 is outside, 3 has no shape
        Assert.Equal(new long[] { 1 }, parcels.ReadFeatures(options).Select(f => f.ObjectId));
    }

    [Fact]
    public void ToFeature_ConvertsToMaptorFeature()
    {
        var first = FileGeodatabase.Open(_folder).GetDataset("Parcels")!
            .ReadFeatures(new FileGdbReadOptions { OnRowError = _ => { } })
            .First();

        var raw = first.ToFeature();
        Assert.Equal(1, raw.Id);
        Assert.Same(first.Geometry, raw.TheGeometry);
        Assert.Equal(new[] { "OBJECTID", "Use", "Kind", "Name" }, raw.Attributes.Keys);
        Assert.Equal(1L, raw.Attributes["OBJECTID"]);
        Assert.Equal((short)1, raw.Attributes["Use"]);

        var resolved = first.ToFeature(resolveCodedValues: true);
        Assert.Equal("Apartment", resolved.Attributes["use"]);
        Assert.Equal("Residential", resolved.Attributes["Kind"]);
        Assert.Equal("a", resolved.Attributes["Name"]);
    }

    [Fact]
    public void StandaloneTable_HasNoGeometry()
    {
        var owners = FileGeodatabase.Open(_folder).GetDataset("Owners")!;

        Assert.Equal(GdbDatasetKind.Table, owners.Kind);
        Assert.Equal(GdbTableGeometryType.None, owners.GeometryType);
        Assert.Null(owners.SpatialReference);
        Assert.Equal(0, owners.Srid);
        Assert.Null(owners.Extent);
        Assert.Null(owners.GeometryFieldName);

        var feature = Assert.Single(owners.ReadFeatures());
        Assert.Null(feature.Geometry);
        Assert.Equal("Ali", feature["OwnerName"]);
        Assert.Null(feature.SubtypeCode);
        Assert.Null(feature.ToFeature().TheGeometry);
    }

    [Fact]
    public void UnreadableDatasets_AreListed_ButFailClearly()
    {
        var gdb = FileGeodatabase.Open(_folder);

        var archive = gdb.GetDataset("Archive")!;
        Assert.Equal(GdbTableStorage.Compressed, archive.Storage);
        Assert.Throws<NotSupportedException>(() => archive.ReadFeatures());   // thrown before enumeration
        Assert.Throws<NotSupportedException>(() => archive.Fields);

        var patches = gdb.GetDataset("Patches")!;
        Assert.False(patches.HasDefinition);
        Assert.Equal(GdbDatasetKind.FeatureClass, patches.Kind);
        Assert.Equal(GdbTableGeometryType.Multipatch, patches.GeometryType);
        Assert.True(patches.HasZ);
        Assert.Equal(4326, patches.Srid);   // from the WKT stored in the table
        Assert.Throws<NotSupportedException>(() => patches.ReadFeatures());
    }

    [Fact]
    public void Open_FailsClearly_OnBadInput()
    {
        Assert.Throws<DirectoryNotFoundException>(() => FileGeodatabase.Open(Path.Combine(_folder, "missing.gdb")));
        Assert.Throws<ArgumentException>(() => FileGeodatabase.Open(" "));

        var empty = Path.Combine(_folder, "empty.gdb");
        Directory.CreateDirectory(empty);
        Assert.Throws<InvalidDataException>(() => FileGeodatabase.Open(empty));
    }
}

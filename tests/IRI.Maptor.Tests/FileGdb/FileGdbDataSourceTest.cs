using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Persistence.Model;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// <see cref="FileGdbDataSource"/> on a synthetic geodatabase: a feature class with domains and subtypes, one with
/// its coordinate system only in the stored WKT, one with none, a standalone table, a compressed table and a
/// multipatch feature class.
/// </summary>
public sealed class FileGdbDataSourceTest : IDisposable
{
    private const string Ns = GdbTestGeodatabase.Ns;

    private const string Wgs84Wkt = "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor_filegdb_source_" + Guid.NewGuid().ToString("N"), "Test.gdb");

    public FileGdbDataSourceTest()
    {
        Directory.CreateDirectory(_folder);
        WriteGeodatabase();
    }

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_folder)!, recursive: true);

    #region Synthetic geodatabase

    private void WriteGeodatabase()
    {
        GdbTestGeodatabase.WriteSystemCatalog(_folder,
            (1, "GDB_SystemCatalog"), (4, "GDB_Items"),
            (9, "Parcels"), (10, "Owners"), (11, "Archive"), (12, "Patches"), (13, "Wells"), (14, "Plots"));

        var parcels =
            $"<DEFeatureClassInfo xsi:type='typens:DEFeatureClassInfo' {Ns}><CatalogPath>\\Cadastre\\Parcels</CatalogPath><Name>Parcels</Name><AliasName>Land parcels</AliasName>" +
            "<GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'>" +
            "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>Use</Name><DomainName>LandUse</DomainName></GPFieldInfoEx>" +
            "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>Name</Name><AliasName>Parcel name</AliasName></GPFieldInfoEx>" +
            "</GPFieldInfoExs>" +
            "<SubtypeFieldName>Kind</SubtypeFieldName><DefaultSubtypeCode>1</DefaultSubtypeCode><Subtypes xsi:type='typens:ArrayOfSubtype'>" +
            "<Subtype xsi:type='typens:Subtype'><SubtypeName>Residential</SubtypeName><SubtypeCode>1</SubtypeCode><FieldInfos xsi:type='typens:ArrayOfSubtypeFieldInfo'>" +
            "<SubtypeFieldInfo xsi:type='typens:SubtypeFieldInfo'><FieldName>Use</FieldName><DomainName>ResidentialUse</DomainName></SubtypeFieldInfo></FieldInfos></Subtype>" +
            "<Subtype xsi:type='typens:Subtype'><SubtypeName>Commercial</SubtypeName><SubtypeCode>2</SubtypeCode><FieldInfos xsi:type='typens:ArrayOfSubtypeFieldInfo'/></Subtype>" +
            "</Subtypes><ShapeType>esriGeometryPoint</ShapeType><ShapeFieldName>SHAPE</ShapeFieldName>" +
            "<SpatialReference xsi:type='typens:GeographicCoordinateSystem'><WKID>4326</WKID><LatestWKID>4326</LatestWKID></SpatialReference></DEFeatureClassInfo>";

        var owners =
            $"<DETableInfo xsi:type='typens:DETableInfo' {Ns}><CatalogPath>\\Owners</CatalogPath><Name>Owners</Name><GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'/></DETableInfo>";

        var archive =
            $"<DEFeatureClassInfo xsi:type='typens:DEFeatureClassInfo' {Ns}><CatalogPath>\\Archive</CatalogPath><Name>Archive</Name><GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'/>" +
            "<ShapeType>esriGeometryPoint</ShapeType><ShapeFieldName>SHAPE</ShapeFieldName></DEFeatureClassInfo>";

        static string Domain(string name, params (short Code, string Name)[] values) =>
            $"<GPCodedValueDomain2 xsi:type='typens:GPCodedValueDomain2' {Ns}><DomainName>{name}</DomainName><CodedValues xsi:type='typens:ArrayOfCodedValue'>" +
            string.Concat(values.Select(v => $"<CodedValue><Name>{v.Name}</Name><Code xsi:type='xs:short'>{v.Code}</Code></CodedValue>")) +
            "</CodedValues></GPCodedValueDomain2>";

        // Wells and Plots have no item definition: their coordinate system is only what their geometry field stores
        GdbTestGeodatabase.WriteItems(_folder,
            (GdbItemTypes.FeatureDataset, "Cadastre", @"\Cadastre", $"<DEFeatureDataset xsi:type='typens:DEFeatureDataset' {Ns}><Name>Cadastre</Name></DEFeatureDataset>"),
            (GdbItemTypes.FeatureClass, "Parcels", @"\Cadastre\Parcels", parcels),
            (GdbItemTypes.Table, "Owners", @"\Owners", owners),
            (GdbItemTypes.FeatureClass, "Archive", @"\Archive", archive),
            (GdbItemTypes.CodedValueDomain, "LandUse", "", Domain("LandUse", (1, "Housing"), (2, "Shops"))),
            (GdbItemTypes.CodedValueDomain, "ResidentialUse", "", Domain("ResidentialUse", (1, "Apartment"), (2, "Villa"))));

        WriteParcels();

        new GdbTestTableBuilder()
            .ObjectIdField()
            .StringField("OwnerName", 50, nullable: true)
            .Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0); GdbTestGeodatabase.WriteVarString(w, "Ali"); }))
            .Write(_folder, "a0000000a");

        // a compressed feature class
        new GdbTestTableBuilder().ObjectIdField().Write(_folder, "a0000000b");
        File.WriteAllBytes(Path.Combine(_folder, "a0000000b.gdbtable.cdf"), new byte[] { 0 });

        // a multipatch feature class
        new GdbTestTableBuilder { GeometryType = GdbTableGeometryType.Multipatch }
            .ObjectIdField()
            .GeometryField("SHAPE", Wgs84Wkt, hasZ: true, hasM: false)
            .Write(_folder, "a0000000c");

        // WGS84 given only as WKT, without an EPSG code
        new GdbTestTableBuilder { GeometryType = GdbTableGeometryType.Point }
            .ObjectIdField()
            .GeometryField("SHAPE", Wgs84Wkt, hasZ: false, hasM: false)
            .Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0); WritePoint(w, 52, 33); }))
            .Write(_folder, "a0000000d");

        // no coordinate system at all, and coordinates that are not longitude/latitude (UTM 39N metres)
        new GdbTestTableBuilder { GeometryType = GdbTableGeometryType.Point }
            .ObjectIdField()
            .GeometryField("SHAPE", null, hasZ: false, hasM: false, extent: (499000, 3499000, 501000, 3501000))
            .Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0); WritePoint(w, 500000, 3500000); }))
            .Write(_folder, "a0000000e");
    }

    private void WriteParcels()
    {
        // nullable fields in order: SHAPE (bit 0), Use (bit 1), Kind (bit 2), Name (bit 3)
        var builder = new GdbTestTableBuilder { GeometryType = GdbTableGeometryType.Point }
            .ObjectIdField()
            .GeometryField("SHAPE", Wgs84Wkt, hasZ: false, hasM: false)
            .FixedField("Use", GdbFieldType.Int16, 2, nullable: true)
            .FixedField("Kind", GdbFieldType.Int32, 4, nullable: true)
            .StringField("Name", 50, nullable: true);

        // subtype 1: Use from ResidentialUse
        builder.Row(1, GdbTestTableBuilder.Record(w => { w.Write((byte)0); WritePoint(w, 50, 30); w.Write((short)1); w.Write(1); GdbTestGeodatabase.WriteVarString(w, "first"); }));

        // subtype 2: Use from LandUse; no name
        builder.Row(2, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1000); WritePoint(w, 60, 35); w.Write((short)2); w.Write(2); }));

        // no shape: left out
        builder.Row(3, GdbTestTableBuilder.Record(w => { w.Write((byte)0b0101); w.Write((short)1); GdbTestGeodatabase.WriteVarString(w, "noshape"); }));

        // unknown shape type 99: unreadable, left out
        builder.Row(4, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1100); w.Write((byte)1); w.Write((byte)99); w.Write((short)1); }));

        // 9 is not a code of LandUse and the subtype field is null: kept as text
        builder.Row(5, GdbTestTableBuilder.Record(w => { w.Write((byte)0b1100); WritePoint(w, 55, 32); w.Write((short)9); }));

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

    private static Point ToWebMercator(double x, double y, SrsBase source) => new Point(x, y).Project(source, SrsBases.WebMercator);

    #endregion

    [Fact]
    public void Constructor_ReadsSchemaOnly()
    {
        var source = new FileGdbDataSource(_folder, "parcels");

        Assert.False(source.IsLoaded);
        Assert.Equal(DataSourceKind.FileGdb, source.DataSourceKind);
        Assert.Equal(SridHelper.WebMercator, source.Srid);
        Assert.Equal(4326, source.OriginalSrid);
        Assert.Null(source.AssumedSrid);
        Assert.Equal(GeometryType.Point, source.GeometryType);
        Assert.Equal("Parcels", source.DatasetName);

        var location = Assert.IsType<FileLocation>(source.Location);
        Assert.Equal(_folder, location.Path);
        Assert.Equal("Parcels", location.TableName);

        // the stored extent (44, 25)–(63.5, 40), in Web Mercator
        var min = ToWebMercator(44, 25, SrsBases.GeodeticWgs84);
        var max = ToWebMercator(63.5, 40, SrsBases.GeodeticWgs84);
        Assert.Equal(min.X, source.WebMercatorExtent.XMin, 3);
        Assert.Equal(min.Y, source.WebMercatorExtent.YMin, 3);
        Assert.Equal(max.X, source.WebMercatorExtent.XMax, 3);
        Assert.Equal(max.Y, source.WebMercatorExtent.YMax, 3);

        // no geometry field; coded fields as text; the definition's alias
        Assert.Equal(new[] { "OBJECTID", "Use", "Kind", "Name" }, source.Fields.Select(f => f.Name));
        Assert.Equal(new[] { FieldDataType.Long, FieldDataType.Text, FieldDataType.Text, FieldDataType.Text }, source.Fields.Select(f => f.DataType));
        Assert.Equal("Parcel name", source.Fields[3].Alias);
        Assert.Equal("Use", source.Fields[1].Alias);
        Assert.All(source.Fields, f => Assert.False(f.CanWrite));
    }

    [Fact]
    public async Task LoadAsync_ReadsFeatures_InWebMercator_WithNames()
    {
        var source = new FileGdbDataSource(_folder, "Parcels");

        await source.LoadAsync();

        Assert.True(source.IsLoaded);
        Assert.False(source.HasError);
        Assert.Equal(2, source.SkippedRowCount);   // OBJECTID 3 has no shape, 4 cannot be read
        Assert.Equal(0, source.CurvedShapeCount);

        var features = (await source.GetAsFeatureSetAsync()).Features;
        Assert.Equal(new object[] { 1L, 2L, 5L }, features.Select(f => f.Attributes["OBJECTID"]));
        Assert.Equal(new[] { 0, 1, 2 }, features.Select(f => f.Id));
        Assert.All(features, f => Assert.Equal(SridHelper.WebMercator, f.TheGeometry.Srid));

        var expected = ToWebMercator(50, 30, SrsBases.GeodeticWgs84);
        var point = features[0].TheGeometry.Points![0];
        Assert.Equal(expected.X, point.X, 3);
        Assert.Equal(expected.Y, point.Y, 3);

        Assert.Equal("Apartment", features[0].Attributes["Use"]);   // subtype 1 has its own domain
        Assert.Equal("Residential", features[0].Attributes["Kind"]);
        Assert.Equal("first", features[0].Attributes["Name"]);

        Assert.Equal("Shops", features[1].Attributes["Use"]);       // subtype 2 falls back to the field's domain
        Assert.Equal("Commercial", features[1].Attributes["Kind"]);
        Assert.Null(features[1].Attributes["Name"]);

        Assert.Equal("9", features[2].Attributes["Use"]);           // not a code: the stored value, as text
        Assert.Null(features[2].Attributes["Kind"]);

        // the schema's fields, not ones guessed from the first feature
        Assert.Same(source.Fields, (await source.GetAsFeatureSetAsync()).Fields);
    }

    [Fact]
    public async Task GetAsFeatureSetAsync_FiltersByBoundingBox()
    {
        var source = new FileGdbDataSource(_folder, "Parcels");
        await source.LoadAsync();

        var min = ToWebMercator(49, 29, SrsBases.GeodeticWgs84);
        var max = ToWebMercator(51, 31, SrsBases.GeodeticWgs84);

        var inside = await source.GetAsFeatureSetAsync(new BoundingBox(min.X, min.Y, max.X, max.Y));

        Assert.Equal(1L, Assert.Single(inside.Features).Attributes["OBJECTID"]);
    }

    [Fact]
    public async Task SearchAsync_MatchesAttributeText()
    {
        var source = new FileGdbDataSource(_folder, "Parcels");
        await source.LoadAsync();

        var all = await source.GetAsFeatureSetAsync();
        var found = await source.SearchAsync("shop");

        Assert.Equal(2L, Assert.Single(found.Features).Attributes["OBJECTID"]);
        Assert.NotSame(all, found);
        Assert.Empty((await source.SearchAsync("nothing like this")).Features);
    }

    [Fact]
    public async Task CoordinateSystem_FromWktWithoutEpsgCode()
    {
        var source = new FileGdbDataSource(_folder, "Wells");

        Assert.Null(source.AssumedSrid);

        await source.LoadAsync();

        var expected = ToWebMercator(52, 33, SrsBases.GeodeticWgs84);
        var point = Assert.Single((await source.GetAsFeatureSetAsync()).Features).TheGeometry.Points![0];
        Assert.Equal(expected.X, point.X, 3);
        Assert.Equal(expected.Y, point.Y, 3);
    }

    [Fact]
    public async Task UnknownCoordinateSystem_NeedsFallbackSrid()
    {
        Assert.Throws<InvalidOperationException>(() => new FileGdbDataSource(_folder, "Plots"));

        var source = new FileGdbDataSource(_folder, "Plots", fallbackSrid: 32639);

        Assert.Equal(32639, source.AssumedSrid);
        Assert.Equal(32639, source.OriginalSrid);

        await source.LoadAsync();

        var expected = ToWebMercator(500000, 3500000, SrsBase.Create(32639)!);
        var point = Assert.Single((await source.GetAsFeatureSetAsync()).Features).TheGeometry.Points![0];
        Assert.Equal(expected.X, point.X, 3);
        Assert.Equal(expected.Y, point.Y, 3);
    }

    [Fact]
    public void FallbackSrid_IsIgnored_WhenTheGeodatabaseGivesOne()
    {
        var source = new FileGdbDataSource(_folder, "Parcels", fallbackSrid: 32639);

        Assert.Null(source.AssumedSrid);
        Assert.Equal(4326, source.OriginalSrid);
    }

    [Fact]
    public void UnreadableDatasets_FailClearly()
    {
        Assert.Contains("without shapes", Assert.Throws<NotSupportedException>(() => new FileGdbDataSource(_folder, "Owners")).Message);
        Assert.Contains("compressed", Assert.Throws<NotSupportedException>(() => new FileGdbDataSource(_folder, "Archive")).Message);
        Assert.Contains("multipatch", Assert.Throws<NotSupportedException>(() => new FileGdbDataSource(_folder, "Patches")).Message);

        var missing = Assert.Throws<InvalidOperationException>(() => new FileGdbDataSource(_folder, "Nothing"));
        Assert.Contains("Nothing", missing.Message);
    }

    [Fact]
    public void OneOpenGeodatabase_ServesSeveralDataSources()
    {
        var gdb = FileGeodatabase.Open(_folder);

        var parcels = new FileGdbDataSource(gdb.GetDataset("Parcels")!);
        var wells = new FileGdbDataSource(gdb.GetDataset("Wells")!);

        Assert.Equal(_folder, parcels.GeodatabasePath);
        Assert.Equal(_folder, wells.GeodatabasePath);
    }
}

using System.IO;
using System.Linq;

using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// <see cref="GdbCatalog.Read"/> on a synthetic geodatabase: joining items with tables, and
/// leaving out topology, attachment and raster helper tables.
/// </summary>
public sealed class GdbCatalogTest : IDisposable
{
    private const string Ns = GdbTestGeodatabase.Ns;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor_filegdb_cat_" + Guid.NewGuid().ToString("N"));

    public GdbCatalogTest() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private void WriteSystemCatalog(params (long Number, string Name)[] tables) => GdbTestGeodatabase.WriteSystemCatalog(_folder, tables);

    private void WriteItems(params (Guid Type, string Name, string Path, string Definition)[] items) => GdbTestGeodatabase.WriteItems(_folder, items);

    private void WriteEmptyTable(long number, bool withGeometry = false)
    {
        var builder = new GdbTestTableBuilder().ObjectIdField();

        if (withGeometry)
            builder.GeometryField("SHAPE", null, hasZ: false, hasM: false);

        builder.Write(_folder, GdbSystemCatalog.GetBaseFileName(number));
    }

    private static string FeatureClass(string name) =>
        $"<DEFeatureClassInfo xsi:type='typens:DEFeatureClassInfo' {Ns}><CatalogPath>\\{name}</CatalogPath><Name>{name}</Name>" +
        "<GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'><GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>Use</Name><DomainName>LandUse</DomainName><FieldType>esriFieldTypeSmallInteger</FieldType></GPFieldInfoEx></GPFieldInfoExs>" +
        "<ShapeType>esriGeometryPolygon</ShapeType><SpatialReference xsi:type='typens:GeographicCoordinateSystem'><WKID>4326</WKID><LatestWKID>4326</LatestWKID></SpatialReference></DEFeatureClassInfo>";

    private static string Table(string name) =>
        $"<DETableInfo xsi:type='typens:DETableInfo' {Ns}><CatalogPath>\\{name}</CatalogPath><Name>{name}</Name><GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'/></DETableInfo>";

    private static string Relationship(string name, string origin, string destination, bool isAttachment) =>
        $"<DERelationshipClassInfo xsi:type='typens:DERelationshipClassInfo' {Ns}><Name>{name}</Name><Cardinality>esriRelCardinalityOneToMany</Cardinality>" +
        $"<OriginClassNames xsi:type='typens:Names'><Name>{origin}</Name></OriginClassNames><DestinationClassNames xsi:type='typens:Names'><Name>{destination}</Name></DestinationClassNames>" +
        "<OriginClassKeys xsi:type='typens:ArrayOfRelationshipClassKey'><RelationshipClassKey><ObjectKeyName>OBJECTID</ObjectKeyName><KeyRole>esriRelKeyRoleOriginPrimary</KeyRole></RelationshipClassKey>" +
        "<RelationshipClassKey><ObjectKeyName>REL_OBJECTID</ObjectKeyName><KeyRole>esriRelKeyRoleOriginForeign</KeyRole></RelationshipClassKey></OriginClassKeys>" +
        $"<IsAttachmentRelationship>{(isAttachment ? "true" : "false")}</IsAttachmentRelationship></DERelationshipClassInfo>";

    [Fact]
    public void Read_JoinsItemsWithTables_AndLeavesOutInternalTables()
    {
        WriteSystemCatalog(
            (1, "GDB_SystemCatalog"), (4, "GDB_Items"),
            (9, "Parcels"), (10, "Parcels__ATTACH"), (11, "T_1_DirtyAreas"),
            (12, "Orphan"), (13, "fras_ras_Img"), (14, "Owners"), (15, "Gone"), (16, "OrphanPoints"));

        WriteItems(
            (GdbItemTypes.FeatureClass, "Parcels", @"\Parcels", FeatureClass("Parcels")),
            (GdbItemTypes.Table, "Parcels__ATTACH", @"\Parcels__ATTACH", Table("Parcels__ATTACH")),
            (GdbItemTypes.RelationshipClass, "Parcels__ATTACHREL", @"\Parcels__ATTACHREL", Relationship("Parcels__ATTACHREL", "Parcels", "Parcels__ATTACH", isAttachment: true)),
            (GdbItemTypes.Table, "Owners", @"\Owners", Table("Owners")),
            (GdbItemTypes.RelationshipClass, "Ownership", @"\Ownership", Relationship("Ownership", "Parcels", "Owners", isAttachment: false)),
            (GdbItemTypes.Topology, "Topo", @"\Topo", $"<DETopology xsi:type='typens:DETopology' {Ns}><Name>Topo</Name><TopologyID>1</TopologyID></DETopology>"),
            (GdbItemTypes.FeatureClass, "T_1_DirtyAreas", @"\T_1_DirtyAreas", FeatureClass("T_1_DirtyAreas")),
            (GdbItemTypes.CodedValueDomain, "LandUse", "", $"<GPCodedValueDomain2 xsi:type='typens:GPCodedValueDomain2' {Ns}><DomainName>LandUse</DomainName><CodedValues xsi:type='typens:ArrayOfCodedValue'><CodedValue><Name>مسکونی</Name><Code xsi:type='xs:short'>1</Code></CodedValue></CodedValues></GPCodedValueDomain2>"),
            (GdbItemTypes.Table, "Broken", @"\Broken", "<DETableInfo><unclosed>"));

        foreach (var number in new long[] { 9, 10, 11, 12, 13, 14 })
            WriteEmptyTable(number);
        WriteEmptyTable(16, withGeometry: true);

        var catalog = GdbCatalog.Read(_folder);

        Assert.Equal(new[] { "Parcels", "Owners", "Orphan", "OrphanPoints" }, catalog.Datasets.Select(d => d.Name));

        var parcels = catalog.GetDataset("parcels")!;
        Assert.True(parcels.HasDefinition);
        Assert.Equal(GdbDatasetKind.FeatureClass, parcels.Kind);
        Assert.Equal(GdbTableStorage.Table, parcels.Table!.Storage);
        Assert.Equal("a00000009.gdbtable", Path.GetFileName(parcels.Table.TablePath));
        Assert.Equal(4326, parcels.SpatialReference!.Srid);
        Assert.Equal(new[] { "Ownership" }, parcels.RelationshipClassNames);

        var orphan = catalog.GetDataset("Orphan")!;
        Assert.False(orphan.HasDefinition);
        Assert.Equal(GdbDatasetKind.Table, orphan.Kind);
        Assert.Equal(GdbDatasetKind.FeatureClass, catalog.GetDataset("OrphanPoints")!.Kind);

        var relationship = Assert.Single(catalog.RelationshipClasses);
        Assert.Equal("Ownership", relationship.Name);

        var domain = Assert.IsType<FileGdbCodedValueDomain>(catalog.GetDomain(parcels.GetDomainName("Use", null)));
        Assert.True(domain.TryGetName(1, out var name));
        Assert.Equal("مسکونی", name);

        // the broken definition is reported, not fatal; the listed-but-absent "Gone" table is skipped
        Assert.Contains(catalog.Warnings, w => w.Contains("Broken"));
        Assert.Null(catalog.GetDataset("Gone"));
    }

    [Fact]
    public void Read_FailsClearly_WithoutGdbItems()
    {
        WriteSystemCatalog((1, "GDB_SystemCatalog"));

        Assert.Throws<InvalidDataException>(() => GdbCatalog.Read(_folder));
    }
}

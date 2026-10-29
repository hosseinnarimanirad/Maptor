using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.FileGdb.Catalog;
using IRI.Maptor.Core.FileGdb.Catalog.Model;
using IRI.Maptor.Core.FileGdb.Catalog.Xml;
using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// Item definition parsing. The XML samples follow the styles seen in real geodatabases:
/// unprefixed one-line documents with single quotes, and prefixed, pretty-printed documents
/// with an XML declaration and double quotes.
/// </summary>
public class GdbCatalogXmlTest
{
    private const string Ns = "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.8'";

    #region Domains

    [Fact]
    public void CodedValueDomain_UnprefixedStyle()
    {
        var xml = $"<GPCodedValueDomain2 xsi:type='typens:GPCodedValueDomain2' {Ns}><DomainName>MedianType</DomainName><FieldType>esriFieldTypeSmallInteger</FieldType>" +
                  "<MergePolicy>esriMPTDefaultValue</MergePolicy><SplitPolicy>esriSPTDefaultValue</SplitPolicy><Description>Road median types.</Description><Owner></Owner>" +
                  "<CodedValues xsi:type='typens:ArrayOfCodedValue'>" +
                  "<CodedValue xsi:type='typens:CodedValue'><Name>None</Name><Code xsi:type='xs:short'>0</Code></CodedValue>" +
                  "<CodedValue xsi:type='typens:CodedValue'><Name>سیمانی</Name><Code xsi:type='xs:short'>1</Code></CodedValue>" +
                  "</CodedValues></GPCodedValueDomain2>";

        var domain = Assert.IsType<FileGdbCodedValueDomain>(GdbCatalog.ParseDomain(GdbXml.Parse(xml), "item"));

        Assert.Equal("MedianType", domain.Name);
        Assert.Equal("Road median types.", domain.Description);
        Assert.Equal("esriFieldTypeSmallInteger", domain.FieldType);
        Assert.Equal(new object[] { (short)0, (short)1 }, domain.CodedValues.Select(c => c.Code));
        Assert.True(domain.TryGetName((short)1, out var name));
        Assert.Equal("سیمانی", name);
    }

    [Fact]
    public void RangeDomain_PrefixedPrettyPrintedStyle()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <typens:GPRangeDomain2 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:typens="http://www.esri.com/schemas/ArcGIS/10.1" xsi:type="typens:GPRangeDomain2">
              <DomainName>SpeedLimit</DomainName>
              <FieldType>esriFieldTypeSmallInteger</FieldType>
              <Description>The maximun speed of the road</Description>
              <Owner/>
              <MaxValue xsi:type="xs:short">100</MaxValue>
              <MinValue xsi:type="xs:short">40</MinValue>
            </typens:GPRangeDomain2>
            """;

        var domain = Assert.IsType<FileGdbRangeDomain>(GdbCatalog.ParseDomain(GdbXml.Parse(xml), "item"));

        Assert.Equal("SpeedLimit", domain.Name);
        Assert.Equal((short)40, domain.MinValue);
        Assert.Equal((short)100, domain.MaxValue);
    }

    [Fact]
    public void CodedValueLookup_IgnoresNumericStorageType()
    {
        var domain = new FileGdbCodedValueDomain("d", null, null,
        [
            new FileGdbCodedValue(1, "int one"),
            new FileGdbCodedValue("A", "letter"),
            new FileGdbCodedValue(2.5, "two and a half"),
        ]);

        Assert.True(domain.TryGetName((short)1, out var a)); Assert.Equal("int one", a);
        Assert.True(domain.TryGetName(1L, out var b)); Assert.Equal("int one", b);
        Assert.True(domain.TryGetName(1.0, out var c)); Assert.Equal("int one", c);
        Assert.True(domain.TryGetName("A", out var d)); Assert.Equal("letter", d);
        Assert.True(domain.TryGetName(2.5f, out var e)); Assert.Equal("two and a half", e);

        Assert.False(domain.TryGetName(3, out _));
        Assert.False(domain.TryGetName(null, out _));
        Assert.False(domain.TryGetName("a", out _)); // string codes are case-sensitive
    }

    #endregion

    #region Datasets and subtypes

    // Subtype block taken from Esri's "XML Schema of the Geodatabase" (J-9620, 2012); no real
    // geodatabase with subtypes was available.
    private static readonly string FeatureClassWithSubtypes =
        $"<DEFeatureClassInfo xsi:type='typens:DEFeatureClassInfo' {Ns}><CatalogPath>\\Network\\Switches</CatalogPath><Name>Switches</Name>" +
        "<DatasetType>esriDTFeatureClass</DatasetType><HasOID>true</HasOID><OIDFieldName>OBJECTID</OIDFieldName>" +
        "<GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'>" +
        "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>OBJECTID</Name><AliasName>OBJECTID</AliasName><FieldType>esriFieldTypeOID</FieldType><IsNullable>false</IsNullable></GPFieldInfoEx>" +
        "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>attribute4</Name><AliasName>ظرفیت</AliasName><DomainName>fieldDomain</DomainName><FieldType>esriFieldTypeInteger</FieldType><IsNullable>true</IsNullable></GPFieldInfoEx>" +
        "<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>attribute6</Name><FieldType>esriFieldTypeInteger</FieldType><IsNullable>true</IsNullable></GPFieldInfoEx>" +
        "</GPFieldInfoExs><RelationshipClassNames xsi:type='typens:Names'></RelationshipClassNames><AliasName>کلیدها</AliasName>" +
        "<SubtypeFieldName>attribute6</SubtypeFieldName><DefaultSubtypeCode>1</DefaultSubtypeCode>" +
        "<Subtypes xsi:type='typens:ArrayOfSubtype'>" +
        "<Subtype xsi:type='typens:Subtype'><SubtypeName>SW1</SubtypeName><SubtypeCode>1</SubtypeCode><FieldInfos xsi:type='typens:ArrayOfSubtypeFieldInfo'>" +
        "<SubtypeFieldInfo xsi:type='typens:SubtypeFieldInfo'><FieldName>attribute4</FieldName><DomainName>attributeRangeDomain</DomainName><DefaultValue xsi:type='xs:int'>25</DefaultValue></SubtypeFieldInfo>" +
        "</FieldInfos></Subtype>" +
        "<Subtype xsi:type='typens:Subtype'><SubtypeName>SW2</SubtypeName><SubtypeCode>2</SubtypeCode><FieldInfos xsi:type='typens:ArrayOfSubtypeFieldInfo'>" +
        "<SubtypeFieldInfo xsi:type='typens:SubtypeFieldInfo'><FieldName>attribute4</FieldName><DefaultValue xsi:type='xs:int'>7</DefaultValue></SubtypeFieldInfo>" +
        "</FieldInfos></Subtype>" +
        "</Subtypes><FeatureType>esriFTSimple</FeatureType><ShapeType>esriGeometryPoint</ShapeType><ShapeFieldName>Shape</ShapeFieldName>" +
        "<SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\"]</WKT><WKID>102100</WKID><LatestWKID>3857</LatestWKID></SpatialReference>" +
        "</DEFeatureClassInfo>";

    [Fact]
    public void FeatureClass_DefinitionIsParsed()
    {
        var dataset = GdbCatalog.ParseDataset(GdbXml.Parse(FeatureClassWithSubtypes), "Switches", @"\Network\Switches", isFeatureClass: true, table: null);

        Assert.Equal(GdbDatasetKind.FeatureClass, dataset.Kind);
        Assert.Equal("Network", dataset.FeatureDatasetName);
        Assert.Equal("کلیدها", dataset.AliasName);
        Assert.Equal("esriGeometryPoint", dataset.ShapeType);
        Assert.Equal("Shape", dataset.ShapeFieldName);
        Assert.Equal("OBJECTID", dataset.ObjectIdFieldName);
        Assert.Equal(3857, dataset.SpatialReference!.Srid);
        Assert.Equal(102100, dataset.SpatialReference.Wkid);

        Assert.Equal(3, dataset.Fields.Count);
        Assert.Equal("ظرفیت", dataset.GetField("ATTRIBUTE4")!.AliasName);
        Assert.True(dataset.GetField("attribute4")!.IsNullable);
    }

    [Fact]
    public void Subtypes_OverrideFieldDomains()
    {
        var dataset = GdbCatalog.ParseDataset(GdbXml.Parse(FeatureClassWithSubtypes), "Switches", @"\Network\Switches", isFeatureClass: true, table: null);

        Assert.Equal("attribute6", dataset.SubtypeFieldName);
        Assert.Equal(1, dataset.DefaultSubtypeCode);
        Assert.Equal(new[] { "1 = SW1", "2 = SW2" }, dataset.Subtypes.Select(s => s.ToString()));

        var sw1 = dataset.GetSubtype(1)!.GetField("attribute4")!;
        Assert.Equal("attributeRangeDomain", sw1.DomainName);
        Assert.Equal(25, sw1.DefaultValue);

        // subtype 1 sets its own domain; subtype 2 sets only a default, so the field's domain applies
        Assert.Equal("attributeRangeDomain", dataset.GetDomainName("attribute4", 1));
        Assert.Equal("fieldDomain", dataset.GetDomainName("attribute4", 2));
        Assert.Equal("fieldDomain", dataset.GetDomainName("attribute4", 99));
        Assert.Equal("fieldDomain", dataset.GetDomainName("attribute4", null));
        Assert.Null(dataset.GetDomainName("attribute6", 1));
    }

    [Fact]
    public void Table_WithoutSubtypes()
    {
        var xml = $"<DETableInfo xsi:type='typens:DETableInfo' {Ns}><CatalogPath>\\table1</CatalogPath><Name>table1</Name><OIDFieldName>OBJECTID</OIDFieldName>" +
                  "<GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'><GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>pk</Name><FieldType>esriFieldTypeInteger</FieldType></GPFieldInfoEx></GPFieldInfoExs>" +
                  "<AliasName>table1</AliasName></DETableInfo>";

        var dataset = GdbCatalog.ParseDataset(GdbXml.Parse(xml), "table1", @"\table1", isFeatureClass: false, table: null);

        Assert.Equal(GdbDatasetKind.Table, dataset.Kind);
        Assert.Null(dataset.FeatureDatasetName);
        Assert.Null(dataset.SpatialReference);
        Assert.Null(dataset.SubtypeFieldName);
        Assert.Empty(dataset.Subtypes);
        Assert.Single(dataset.Fields);
    }

    #endregion

    #region Spatial reference

    [Theory]
    [InlineData("<WKID>102100</WKID><LatestWKID>3857</LatestWKID>", 3857)]
    [InlineData("<WKID>4326</WKID>", 4326)]
    [InlineData("", 4326)] // from the EPSG authority in the WKT
    [InlineData("<WKID>0</WKID>", 4326)] // 0 means none (GDAL's weird_winding_order sample)
    public void Srid_PrefersLatestWkid_ThenWkid_ThenWkt(string ids, int expected)
    {
        var wkt = "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433],AUTHORITY[\"EPSG\",4326]]";
        var xml = $"<SpatialReference xsi:type='typens:GeographicCoordinateSystem' {Ns}><WKT>{System.Security.SecurityElement.Escape(wkt)}</WKT>{ids}</SpatialReference>";

        var srs = GdbCatalog.ParseSpatialReference(GdbXml.Parse(xml))!;

        Assert.Equal(expected, srs.Srid);
        Assert.Equal(wkt, srs.Wkt);
    }

    [Fact]
    public void Srid_IsZero_WhenNothingIsKnown()
    {
        var srs = GdbCatalog.ParseSpatialReference(GdbXml.Parse($"<SpatialReference {Ns}><WKT>not a wkt</WKT></SpatialReference>"))!;

        Assert.Equal(0, srs.Srid);
    }

    #endregion

    #region Relationship classes

    private static string Relationship(string cardinality, string keys) =>
        $"<DERelationshipClassInfo xsi:type='typens:DERelationshipClassInfo' {Ns}><Name>rel</Name><Cardinality>{cardinality}</Cardinality>" +
        "<Notification>esriRelNotificationForward</Notification><IsAttributed>false</IsAttributed><IsComposite>true</IsComposite>" +
        "<OriginClassNames xsi:type='typens:Names'><Name>parcels</Name></OriginClassNames>" +
        "<DestinationClassNames xsi:type='typens:Names'><Name>owners</Name></DestinationClassNames>" +
        "<ForwardPathLabel>owners</ForwardPathLabel><BackwardPathLabel>parcels</BackwardPathLabel>" + keys + "</DERelationshipClassInfo>";

    private static string Key(string field, string role) =>
        $"<RelationshipClassKey xsi:type='typens:RelationshipClassKey'><ObjectKeyName>{field}</ObjectKeyName><ClassKeyName></ClassKeyName><KeyRole>{role}</KeyRole></RelationshipClassKey>";

    [Fact]
    public void Relationship_OneToMany()
    {
        var xml = Relationship("esriRelCardinalityOneToMany",
            "<OriginClassKeys xsi:type='typens:ArrayOfRelationshipClassKey'>" + Key("pk", "esriRelKeyRoleOriginPrimary") + Key("parent_pk", "esriRelKeyRoleOriginForeign") + "</OriginClassKeys>");

        var rel = GdbCatalog.ParseRelationshipClass(GdbXml.Parse(xml), "rel", @"\fd\rel", new List<string>())!;

        Assert.Equal(GdbRelationshipCardinality.OneToMany, rel.Cardinality);
        Assert.Equal("parcels", rel.OriginClassName);
        Assert.Equal("owners", rel.DestinationClassName);
        Assert.Equal("pk", rel.OriginPrimaryKey);
        Assert.Equal("parent_pk", rel.OriginForeignKey);
        Assert.Null(rel.DestinationPrimaryKey);
        Assert.True(rel.IsComposite);
        Assert.False(rel.IsAttributed);
        Assert.Equal("fd", rel.FeatureDatasetName);
        Assert.Equal("owners", rel.ForwardPathLabel);
        Assert.Equal("esriRelNotificationForward", rel.Notification);
    }

    [Fact]
    public void Relationship_ManyToMany_HasMappingTableKeys()
    {
        var xml = Relationship("esriRelCardinalityManyToMany",
            "<OriginClassKeys xsi:type='typens:ArrayOfRelationshipClassKey'>" + Key("pk", "esriRelKeyRoleOriginPrimary") + Key("origin_fk", "esriRelKeyRoleOriginForeign") + "</OriginClassKeys>" +
            "<DestinationClassKeys xsi:type='typens:ArrayOfRelationshipClassKey'>" + Key("dest_pk", "esriRelKeyRoleDestinationPrimary") + Key("dest_fk", "esriRelKeyRoleDestinationForeign") + "</DestinationClassKeys>");

        var rel = GdbCatalog.ParseRelationshipClass(GdbXml.Parse(xml), "rel", @"\rel", new List<string>())!;

        Assert.Equal(GdbRelationshipCardinality.ManyToMany, rel.Cardinality);
        Assert.Equal(("pk", "origin_fk", "dest_pk", "dest_fk"), (rel.OriginPrimaryKey, rel.OriginForeignKey, rel.DestinationPrimaryKey, rel.DestinationForeignKey));
        Assert.Null(rel.FeatureDatasetName);
    }

    [Fact]
    public void Relationship_UnknownCardinality_IsSkippedWithWarning()
    {
        var warnings = new List<string>();

        Assert.Null(GdbCatalog.ParseRelationshipClass(GdbXml.Parse(Relationship("esriRelCardinalityWeird", "")), "rel", @"\rel", warnings));
        Assert.Single(warnings);
    }

    #endregion

    [Theory]
    [InlineData(@"\Roads", null)]
    [InlineData(@"\FD_Default\Roads", "FD_Default")]
    [InlineData(null, null)]
    public void FeatureDatasetName_ComesFromThePath(string? path, string? expected)
    {
        Assert.Equal(expected, GdbCatalog.GetFeatureDatasetName(path));
    }

    [Theory]
    [InlineData("<V xsi:type='xs:short'>5</V>", (short)5)]
    [InlineData("<V xsi:type='xs:int'>5</V>", 5)]
    [InlineData("<V xsi:type='xs:double'>2.5</V>", 2.5)]
    [InlineData("<V xsi:type='xs:string'>abc</V>", "abc")]
    [InlineData("<V>untyped</V>", "untyped")]
    [InlineData("<V xsi:type='xs:int'>oops</V>", "oops")]
    public void TypedValue_FollowsXsiType(string element, object expected)
    {
        var xml = element.Replace("<V", $"<V {Ns}");

        Assert.Equal(expected, GdbXml.Parse(xml).TypedValue());
    }
}

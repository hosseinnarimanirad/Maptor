using System.Text;

using IRI.Maptor.Ket.PersonalGdbPersistence.Model;

namespace IRI.Maptor.Ket.PersonalGdbPersistence.Write;

// Constants and XML templates for authoring the GDB_* catalog rows of a personal
// geodatabase. The Definition XML is templated verbatim from an ArcGIS 10.7-authored
// pgdb (the Xml/* DTOs are partial read-models and cannot serialize the full ordered
// element set ArcObjects expects), so keep the element order exactly as-is.
internal static class PersonalGdbTemplates
{
    // GDB_ItemTypes: 'Feature Class'
    internal static readonly Guid FeatureClassItemType = new("70737809-852C-4A03-9E22-2CECEA5B9BFA");

    // GDB_ItemTypes: 'Folder' (the root item, Path = '\')
    internal static readonly Guid FolderItemType = new("F3783E6F-65CA-4514-8315-CE3985DAD3B1");

    // GDB_ItemRelationshipTypes: 'DatasetInFolder'
    internal static readonly Guid DatasetInFolderRelationshipType = new("DC78F1AB-34E4-43AC-BA47-1C4EABD0E7C7");

    // ArcObjects CLSID of a simple feature class, stamped in every DEFeatureClassInfo
    private const string SimpleFeatureClassClsid = "{52353152-891A-11D0-BEC6-00805F7C4268}";

    // ESRI writes this exact quiet-NaN bit pattern (not .NET's negative NaN) in shape blobs
    private static readonly byte[] _esriNaN = BitConverter.GetBytes(BitConverter.Int64BitsToDouble(0x7FF8000000000001));

    // GDB_Items.Shape for a feature-class row is a constant empty polygon:
    // int32 shapeType 5 + NaN bounding box + numParts 0 + numPoints 0 (44 bytes)
    internal static byte[] CreateEmptyItemShapeBlob()
    {
        var bytes = new byte[44];

        bytes[0] = 5;

        for (int i = 0; i < 4; i++)
            _esriNaN.CopyTo(bytes, 4 + 8 * i);

        return bytes;
    }

    // ArcGIS keeps the live extent in GDB_GeomColumns only; the Definition extent stays NaN
    // forever, so this XML is written once at feature-class creation and never updated.
    internal static string BuildFeatureClassDefinition(
        string name,
        string aliasName,
        int dsid,
        string esriShapeTypeName,
        bool hasLengthField,
        bool hasAreaField,
        IReadOnlyList<PersonalGdbField>? userFields,
        string wkt,
        bool isGeographic,
        int epsgSrid)
    {
        var srsXsiType = isGeographic ? "typens:GeographicCoordinateSystem" : "typens:ProjectedCoordinateSystem";

        var srsBody = BuildSpatialReferenceBody(wkt, isGeographic, epsgSrid);

        var lengthFieldName = hasLengthField ? "SHAPE_Length" : string.Empty;

        var areaFieldName = hasAreaField ? "SHAPE_Area" : string.Empty;

        return
            "<DEFeatureClassInfo xsi:type='typens:DEFeatureClassInfo' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.7'>" +
            $"<CatalogPath>\\{name}</CatalogPath>" +
            $"<Name>{name}</Name>" +
            "<ChildrenExpanded>false</ChildrenExpanded>" +
            "<DatasetType>esriDTFeatureClass</DatasetType>" +
            FormattableString.Invariant($"<DSID>{dsid}</DSID>") +
            "<Versioned>false</Versioned>" +
            "<CanVersion>false</CanVersion>" +
            "<ConfigurationKeyword></ConfigurationKeyword>" +
            "<RequiredGeodatabaseClientVersion>10.0</RequiredGeodatabaseClientVersion>" +
            "<HasOID>true</HasOID>" +
            "<OIDFieldName>OBJECTID</OIDFieldName>" +
            $"<GPFieldInfoExs xsi:type='typens:ArrayOfGPFieldInfoEx'>{BuildFieldInfos(hasLengthField, hasAreaField, userFields)}</GPFieldInfoExs>" +
            $"<CLSID>{SimpleFeatureClassClsid}</CLSID>" +
            "<EXTCLSID></EXTCLSID>" +
            "<RelationshipClassNames xsi:type='typens:Names'></RelationshipClassNames>" +
            $"<AliasName>{EscapeXml(aliasName)}</AliasName>" +
            "<ModelName></ModelName>" +
            "<HasGlobalID>false</HasGlobalID>" +
            "<GlobalIDFieldName></GlobalIDFieldName>" +
            "<RasterFieldName></RasterFieldName>" +
            "<ExtensionProperties xsi:type='typens:PropertySet'><PropertyArray xsi:type='typens:ArrayOfPropertySetProperty'></PropertyArray></ExtensionProperties>" +
            "<ControllerMemberships xsi:type='typens:ArrayOfControllerMembership'></ControllerMemberships>" +
            "<EditorTrackingEnabled>false</EditorTrackingEnabled>" +
            "<CreatorFieldName></CreatorFieldName>" +
            "<CreatedAtFieldName></CreatedAtFieldName>" +
            "<EditorFieldName></EditorFieldName>" +
            "<EditedAtFieldName></EditedAtFieldName>" +
            "<IsTimeInUTC>true</IsTimeInUTC>" +
            "<FeatureType>esriFTSimple</FeatureType>" +
            $"<ShapeType>{esriShapeTypeName}</ShapeType>" +
            "<ShapeFieldName>SHAPE</ShapeFieldName>" +
            "<HasM>false</HasM>" +
            "<HasZ>false</HasZ>" +
            "<HasSpatialIndex>true</HasSpatialIndex>" +
            $"<AreaFieldName>{areaFieldName}</AreaFieldName>" +
            $"<LengthFieldName>{lengthFieldName}</LengthFieldName>" +
            "<Extent xsi:type='typens:EnvelopeN'><XMin>NaN</XMin><YMin>NaN</YMin><XMax>NaN</XMax><YMax>NaN</YMax>" +
            $"<SpatialReference xsi:type='{srsXsiType}'>{srsBody}</SpatialReference></Extent>" +
            $"<SpatialReference xsi:type='{srsXsiType}'>{srsBody}</SpatialReference>" +
            "<ChangeTracked>false</ChangeTracked>" +
            "<FieldFilteringEnabled>false</FieldFilteringEnabled>" +
            "<FilteredFieldNames xsi:type='typens:Names'></FilteredFieldNames>" +
            "</DEFeatureClassInfo>";
    }

    private static string BuildFieldInfos(bool hasLengthField, bool hasAreaField, IReadOnlyList<PersonalGdbField>? userFields)
    {
        var builder = new StringBuilder();

        builder.Append("<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>OBJECTID</Name><AliasName>OBJECTID</AliasName><ModelName>OBJECTID</ModelName><FieldType>esriFieldTypeOID</FieldType><IsNullable>false</IsNullable><DomainFixed>true</DomainFixed><Required>true</Required><Editable>false</Editable></GPFieldInfoEx>");

        builder.Append("<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>SHAPE</Name><ModelName>SHAPE</ModelName><FieldType>esriFieldTypeGeometry</FieldType><IsNullable>true</IsNullable><DomainFixed>true</DomainFixed><Required>true</Required></GPFieldInfoEx>");

        if (hasLengthField)
            builder.Append("<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>SHAPE_Length</Name><ModelName>SHAPE_Length</ModelName><FieldType>esriFieldTypeDouble</FieldType><IsNullable>true</IsNullable><Required>true</Required><Editable>false</Editable></GPFieldInfoEx>");

        if (hasAreaField)
            builder.Append("<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>SHAPE_Area</Name><ModelName>SHAPE_Area</ModelName><FieldType>esriFieldTypeDouble</FieldType><IsNullable>true</IsNullable><Required>true</Required><Editable>false</Editable></GPFieldInfoEx>");

        if (userFields is not null)
        {
            foreach (var field in userFields)
            {
                var alias = EscapeXml(string.IsNullOrWhiteSpace(field.Alias) ? field.Name : field.Alias);

                var isNullable = field.IsNullable ? "true" : "false";

                builder.Append($"<GPFieldInfoEx xsi:type='typens:GPFieldInfoEx'><Name>{field.Name}</Name><AliasName>{alias}</AliasName><ModelName>{field.Name}</ModelName><FieldType>{field.FieldType}</FieldType><IsNullable>{isNullable}</IsNullable></GPFieldInfoEx>");
            }
        }

        return builder.ToString();
    }

    // Precision/tolerance values are the ArcGIS 10.x defaults observed in ESRI-authored
    // pgdbs: degrees domain origin -400 with 1e9 scale, meters domain origin
    // -5120900/-9998100 with 1e4 scale. Must stay consistent with PersonalGdb's
    // GDB_SpatialRefs defaults.
    private static string BuildSpatialReferenceBody(string wkt, bool isGeographic, int epsgSrid)
    {
        var xOrigin = isGeographic ? "-400" : "-5120900";

        var yOrigin = isGeographic ? "-400" : "-9998100";

        // 1e9 renders as 999999999.99999988 in ESRI XML; kept verbatim
        var xyScale = isGeographic ? "999999999.99999988" : "10000";

        var xyTolerance = isGeographic ? "8.983152841195215e-09" : "0.001";

        var leftLongitude = isGeographic ? "<LeftLongitude>-180</LeftLongitude>" : string.Empty;

        var wkid = epsgSrid > 0
            ? FormattableString.Invariant($"<WKID>{epsgSrid}</WKID><LatestWKID>{epsgSrid}</LatestWKID>")
            : string.Empty;

        return
            $"<WKT>{EscapeXml(wkt)}</WKT>" +
            $"<XOrigin>{xOrigin}</XOrigin>" +
            $"<YOrigin>{yOrigin}</YOrigin>" +
            $"<XYScale>{xyScale}</XYScale>" +
            "<ZOrigin>-100000</ZOrigin>" +
            "<ZScale>10000</ZScale>" +
            "<MOrigin>-100000</MOrigin>" +
            "<MScale>10000</MScale>" +
            $"<XYTolerance>{xyTolerance}</XYTolerance>" +
            "<ZTolerance>0.001</ZTolerance>" +
            "<MTolerance>0.001</MTolerance>" +
            "<HighPrecision>true</HighPrecision>" +
            leftLongitude +
            wkid;
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
}

using System.Data.OleDb;
using System.Text.RegularExpressions;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.PersonalGdb;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;
using IRI.Maptor.Ket.PersonalGdbPersistence.Write;

namespace IRI.Maptor.Ket.PersonalGdbPersistence;

// Write-side companion of PersoanlGdbDataSource: creates ArcGIS-compatible personal
// geodatabases (.mdb) and feature classes, and inserts features.
//
// Usage: var gdb = PersonalGdb.CreateEmpty(path);
//        gdb.CreateFeatureClass("Roads", GeometryType.LineString, SrsBases.GeodeticWgs84);
//        gdb.Insert("Roads", features);
//
// Geometries are written as-is (no reprojection) and must already be in the feature
// class CRS. XY only: Z/M feature classes are not supported.
public class PersonalGdb
{
    private static readonly Regex _nameRegex = new(@"^[A-Za-z_][A-Za-z0-9_]{0,63}$", RegexOptions.Compiled);

    private static readonly string[] _reservedFieldNames = { "OBJECTID", "SHAPE", "SHAPE_LENGTH", "SHAPE_AREA" };

    public string MdbFileName { get; }

    private PersonalGdb(string mdbFileName)
    {
        MdbFileName = mdbFileName;
    }

    // Extracts the embedded empty-pgdb template (an ArcCatalog-authored ArcGIS 10.7
    // personal geodatabase containing only the GDB_* catalog tables) to mdbFileName.
    public static PersonalGdb CreateEmpty(string mdbFileName, bool overwrite = false)
    {
        if (File.Exists(mdbFileName) && !overwrite)
            throw new IOException($"File already exists: {mdbFileName}. Pass overwrite: true to replace it.");

        var directory = Path.GetDirectoryName(mdbFileName);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var assembly = typeof(PersonalGdb).Assembly;

        var resourceName = assembly.GetManifestResourceNames()
                                   .FirstOrDefault(n => n.EndsWith("template.mdb", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded personal geodatabase template (template.mdb) was not found.");

        using (var resource = assembly.GetManifestResourceStream(resourceName)!)
        using (var file = File.Create(mdbFileName))
        {
            resource.CopyTo(file);
        }

        return new PersonalGdb(mdbFileName);
    }

    public static PersonalGdb Open(string mdbFileName)
    {
        using (var connection = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(mdbFileName)))
        {
            connection.Open();

            using var command = new OleDbCommand($"SELECT COUNT(*) FROM {PersonalGdbInfrastructure.GdbItemsTable}", connection);

            command.ExecuteScalar();
        }

        return new PersonalGdb(mdbFileName);
    }

    #region CreateFeatureClass

    public void CreateFeatureClass(
        string name,
        GeometryType geometryType,
        SrsBase srs,
        IReadOnlyList<PersonalGdbField>? fields = null,
        string? aliasName = null)
    {
        ValidateNames(name, fields);

        var (shapeType, esriShapeTypeName, hasLengthField, hasAreaField) = MapGeometryType(geometryType);

        var isGeographic = srs.Type is SpatialReferenceType.None or SpatialReferenceType.Geodetic;

        // Definition XML carries the AUTHORITY node (and WKID) when the EPSG code is known;
        // GDB_SpatialRefs.SRTEXT is stored without it — both as observed in ESRI-authored pgdbs
        var wkt = srs.AsEsriCrsWkt();

        var srtext = Regex.Replace(wkt, @",AUTHORITY\[[^\]]*\]", string.Empty);

        using var connection = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(MdbFileName));

        connection.Open();

        using var transaction = connection.BeginTransaction();

        if (FeatureClassExists(connection, transaction, name))
            throw new InvalidOperationException($"A dataset named '{name}' already exists in {MdbFileName}.");

        var (srid, falseX, falseY) = EnsureSpatialReference(connection, transaction, srtext, isGeographic);

        foreach (var ddl in PersonalGdbSql.BuildCreateFeatureClassDdl(name, hasLengthField, hasAreaField, fields))
            PersonalGdbSql.ExecuteNonQuery(connection, transaction, ddl);

        var itemId = PersonalGdbSql.GetNextId(connection, transaction, PersonalGdbInfrastructure.GdbItemsTable, "ObjectID");

        var itemUuid = Guid.NewGuid();

        var definition = PersonalGdbTemplates.BuildFeatureClassDefinition(
            name,
            string.IsNullOrWhiteSpace(aliasName) ? name : aliasName,
            dsid: itemId,
            esriShapeTypeName,
            hasLengthField,
            hasAreaField,
            fields,
            wkt,
            isGeographic,
            epsgSrid: srs.Srid);

        InsertGdbItem(connection, transaction, itemId, itemUuid, name, shapeType, definition);

        InsertGdbItemRelationship(connection, transaction, itemUuid);

        InsertGdbGeomColumn(connection, transaction, name, shapeType, srid, falseX, falseY, isGeographic);

        transaction.Commit();
    }

    private static void ValidateNames(string name, IReadOnlyList<PersonalGdbField>? fields)
    {
        if (!_nameRegex.IsMatch(name))
            throw new ArgumentException($"Invalid feature class name: '{name}'.", nameof(name));

        if (fields is null)
            return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (!_nameRegex.IsMatch(field.Name))
                throw new ArgumentException($"Invalid field name: '{field.Name}'.", nameof(fields));

            if (_reservedFieldNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Field name '{field.Name}' is reserved; it is created automatically.", nameof(fields));

            if (!seen.Add(field.Name))
                throw new ArgumentException($"Duplicate field name: '{field.Name}'.", nameof(fields));
        }
    }

    private static (EsriPGDBColumnShapeType ShapeType, string EsriName, bool HasLength, bool HasArea) MapGeometryType(GeometryType geometryType)
    {
        switch (geometryType)
        {
            case GeometryType.Point:
                return (EsriPGDBColumnShapeType.Point, "esriGeometryPoint", false, false);

            case GeometryType.MultiPoint:
                return (EsriPGDBColumnShapeType.Multipoint, "esriGeometryMultipoint", false, false);

            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return (EsriPGDBColumnShapeType.Polyline, "esriGeometryPolyline", true, false);

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return (EsriPGDBColumnShapeType.Polygon, "esriGeometryPolygon", true, true);

            default:
                throw new NotSupportedException($"Geometry type {geometryType} is not supported in a personal geodatabase feature class.");
        }
    }

    private static bool FeatureClassExists(OleDbConnection connection, OleDbTransaction transaction, string name)
    {
        using var command = new OleDbCommand(
            $"SELECT COUNT(*) FROM {PersonalGdbInfrastructure.GdbItemsTable} WHERE PhysicalName = ?", connection, transaction);

        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, name.ToUpperInvariant());

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    // Returns the internal GDB_SpatialRefs id for the given ESRI WKT, inserting a new row
    // (with the ArcGIS 10.x default precision domain) when the CRS is not stored yet.
    private static (int Srid, double FalseX, double FalseY) EnsureSpatialReference(
        OleDbConnection connection, OleDbTransaction transaction, string srtext, bool isGeographic)
    {
        var maxSrid = 0;

        using (var command = new OleDbCommand(
            $"SELECT SRID, SRTEXT, FalseX, FalseY FROM {PersonalGdbInfrastructure.GdbSpatialRefTable}", connection, transaction))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var srid = Convert.ToInt32(reader["SRID"]);

                maxSrid = Math.Max(maxSrid, srid);

                if (reader["SRTEXT"] != DBNull.Value && string.Equals((string)reader["SRTEXT"], srtext, StringComparison.Ordinal))
                    return (srid, Convert.ToDouble(reader["FalseX"]), Convert.ToDouble(reader["FalseY"]));
            }
        }

        var falseX = isGeographic ? -400d : -5120900d;

        var falseY = isGeographic ? -400d : -9998100d;

        var xyUnits = isGeographic ? 1e9 : 1e4;

        var newSrid = maxSrid + 1;

        using (var insert = new OleDbCommand(
            $"INSERT INTO {PersonalGdbInfrastructure.GdbSpatialRefTable} (SRID, SRTEXT, FalseX, FalseY, XYUnits, FalseZ, ZUnits, FalseM, MUnits) VALUES (?,?,?,?,?,?,?,?,?)",
            connection, transaction))
        {
            PersonalGdbSql.AddParameter(insert, OleDbType.Integer, newSrid);
            PersonalGdbSql.AddParameter(insert, OleDbType.LongVarWChar, srtext);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, falseX);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, falseY);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, xyUnits);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, -100000d);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, 10000d);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, -100000d);
            PersonalGdbSql.AddParameter(insert, OleDbType.Double, 10000d);

            insert.ExecuteNonQuery();
        }

        return (newSrid, falseX, falseY);
    }

    private static void InsertGdbItem(
        OleDbConnection connection, OleDbTransaction transaction,
        int itemId, Guid itemUuid, string name, EsriPGDBColumnShapeType shapeType, string definition)
    {
        using var command = new OleDbCommand(
            $"INSERT INTO {PersonalGdbInfrastructure.GdbItemsTable} " +
            "(ObjectID, UUID, Type, Name, PhysicalName, Path, DatasetSubtype1, DatasetSubtype2, DatasetInfo1, Properties, Definition, Shape) " +
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?)",
            connection, transaction);

        PersonalGdbSql.AddParameter(command, OleDbType.Integer, itemId);
        PersonalGdbSql.AddParameter(command, OleDbType.Guid, itemUuid);
        PersonalGdbSql.AddParameter(command, OleDbType.Guid, PersonalGdbTemplates.FeatureClassItemType);
        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, name);
        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, name.ToUpperInvariant());
        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, $"\\{name}");
        PersonalGdbSql.AddParameter(command, OleDbType.Integer, 1);   // esriFTSimple
        PersonalGdbSql.AddParameter(command, OleDbType.Integer, (int)shapeType);
        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, "SHAPE");
        PersonalGdbSql.AddParameter(command, OleDbType.Integer, 1);
        PersonalGdbSql.AddParameter(command, OleDbType.LongVarWChar, definition);
        PersonalGdbSql.AddParameter(command, OleDbType.LongVarBinary, PersonalGdbTemplates.CreateEmptyItemShapeBlob());

        command.ExecuteNonQuery();
    }

    // links the new feature class to the root folder item ('DatasetInFolder')
    private static void InsertGdbItemRelationship(OleDbConnection connection, OleDbTransaction transaction, Guid featureClassUuid)
    {
        Guid rootUuid;

        using (var command = new OleDbCommand(
            $"SELECT UUID FROM {PersonalGdbInfrastructure.GdbItemsTable} WHERE Path = '\\' AND Type = ?", connection, transaction))
        {
            PersonalGdbSql.AddParameter(command, OleDbType.Guid, PersonalGdbTemplates.FolderItemType);

            var result = command.ExecuteScalar();

            if (result is null || result == DBNull.Value)
                throw new InvalidOperationException("The geodatabase catalog has no root folder item; the file is not a valid personal geodatabase.");

            rootUuid = (Guid)result;
        }

        var relationshipId = PersonalGdbSql.GetNextId(connection, transaction, "GDB_ItemRelationships", "ObjectID");

        using var insert = new OleDbCommand(
            "INSERT INTO GDB_ItemRelationships (ObjectID, UUID, OriginID, DestID, Type, Properties) VALUES (?,?,?,?,?,?)",
            connection, transaction);

        PersonalGdbSql.AddParameter(insert, OleDbType.Integer, relationshipId);
        PersonalGdbSql.AddParameter(insert, OleDbType.Guid, Guid.NewGuid());
        PersonalGdbSql.AddParameter(insert, OleDbType.Guid, rootUuid);
        PersonalGdbSql.AddParameter(insert, OleDbType.Guid, featureClassUuid);
        PersonalGdbSql.AddParameter(insert, OleDbType.Guid, PersonalGdbTemplates.DatasetInFolderRelationshipType);
        PersonalGdbSql.AddParameter(insert, OleDbType.Integer, 1);

        insert.ExecuteNonQuery();
    }

    private static void InsertGdbGeomColumn(
        OleDbConnection connection, OleDbTransaction transaction,
        string name, EsriPGDBColumnShapeType shapeType, int srid, double falseX, double falseY, bool isGeographic)
    {
        var gridSize = isGeographic ? PersonalGdbSpatialIndex.GeographicGridSize : PersonalGdbSpatialIndex.ProjectedGridSize;

        using var command = new OleDbCommand(
            $"INSERT INTO {PersonalGdbInfrastructure.GdbGeomColumnsTable} " +
            "(TableName, FieldName, ShapeType, ExtentLeft, ExtentBottom, ExtentRight, ExtentTop, IdxOriginX, IdxOriginY, IdxGridSize, SRID, HasZ, HasM, ZLow, ZHigh, MLow, MHigh) " +
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
            connection, transaction);

        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, name);
        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, "SHAPE");
        PersonalGdbSql.AddParameter(command, OleDbType.Integer, (int)shapeType);

        // extent stays NaN until the first Insert; Z/M ranges stay NaN for XY-only classes
        for (int i = 0; i < 4; i++)
            PersonalGdbSql.AddParameter(command, OleDbType.Double, double.NaN);

        PersonalGdbSql.AddParameter(command, OleDbType.Double, falseX);
        PersonalGdbSql.AddParameter(command, OleDbType.Double, falseY);
        PersonalGdbSql.AddParameter(command, OleDbType.Double, gridSize);
        PersonalGdbSql.AddParameter(command, OleDbType.Integer, srid);
        PersonalGdbSql.AddParameter(command, OleDbType.Boolean, false);
        PersonalGdbSql.AddParameter(command, OleDbType.Boolean, false);

        for (int i = 0; i < 4; i++)
            PersonalGdbSql.AddParameter(command, OleDbType.Double, double.NaN);

        command.ExecuteNonQuery();
    }

    #endregion

    #region Insert

    public int Insert(string featureClassName, FeatureSet<Point> featureSet) => Insert(featureClassName, featureSet.Features);

    // Inserts features (geometry + attributes matched to columns by name) and maintains
    // the spatial index and the GDB_GeomColumns extent. Attribute values missing from a
    // feature are written as null.
    public int Insert(string featureClassName, IEnumerable<Feature<Point>> features)
    {
        using var connection = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(MdbFileName));

        connection.Open();

        var metadata = ReadGeomColumnMetadata(connection, featureClassName);

        var attributeColumns = GetAttributeColumns(connection, featureClassName, metadata.FieldName);

        var hasLengthField = metadata.ShapeType is EsriPGDBColumnShapeType.Polyline or EsriPGDBColumnShapeType.Polygon;

        var hasAreaField = metadata.ShapeType is EsriPGDBColumnShapeType.Polygon;

        using var transaction = connection.BeginTransaction();

        var columnNames = new List<string> { "OBJECTID", $"[{metadata.FieldName}]" };

        if (hasLengthField)
            columnNames.Add("SHAPE_Length");

        if (hasAreaField)
            columnNames.Add("SHAPE_Area");

        columnNames.AddRange(attributeColumns.Select(c => $"[{c.Name}]"));

        using var insert = new OleDbCommand(
            $"INSERT INTO [{featureClassName}] ({string.Join(", ", columnNames)}) VALUES ({string.Join(",", columnNames.Select(_ => "?"))})",
            connection, transaction);

        var oidParameter = PersonalGdbSql.AddParameter(insert, OleDbType.Integer, null);

        var shapeParameter = PersonalGdbSql.AddParameter(insert, OleDbType.LongVarBinary, null);

        var lengthParameter = hasLengthField ? PersonalGdbSql.AddParameter(insert, OleDbType.Double, null) : null;

        var areaParameter = hasAreaField ? PersonalGdbSql.AddParameter(insert, OleDbType.Double, null) : null;

        var attributeParameters = attributeColumns.Select(c => PersonalGdbSql.AddParameter(insert, c.Type, null)).ToList();

        using var insertIndex = new OleDbCommand(
            $"INSERT INTO [{featureClassName}_SHAPE_Index] (IndexedObjectId, MinGX, MinGY, MaxGX, MaxGY) VALUES (?,?,?,?,?)",
            connection, transaction);

        for (int i = 0; i < 5; i++)
            PersonalGdbSql.AddParameter(insertIndex, OleDbType.Integer, null);

        var oid = PersonalGdbSql.GetNextId(connection, transaction, featureClassName, "OBJECTID");

        var count = 0;

        var extent = metadata.Extent;

        foreach (var feature in features)
        {
            var geometry = feature.TheGeometry;

            oidParameter.Value = oid;

            if (geometry.IsNullOrEmpty())
            {
                shapeParameter.Value = DBNull.Value;

                if (lengthParameter is not null)
                    lengthParameter.Value = DBNull.Value;

                if (areaParameter is not null)
                    areaParameter.Value = DBNull.Value;
            }
            else
            {
                ValidateGeometryType(geometry!.Type, metadata.ShapeType, featureClassName);

                shapeParameter.Value = geometry.AsEsriShape()!.WriteContentsToByte();

                if (lengthParameter is not null)
                    lengthParameter.Value = geometry.GetEuclideanLength();

                if (areaParameter is not null)
                    areaParameter.Value = geometry.EuclideanArea;
            }

            for (int i = 0; i < attributeColumns.Count; i++)
                attributeParameters[i].Value = GetAttributeValue(feature, attributeColumns[i].Name) ?? DBNull.Value;

            insert.ExecuteNonQuery();

            if (!geometry.IsNullOrEmpty())
            {
                var boundingBox = geometry!.GetBoundingBox();

                insertIndex.Parameters[0].Value = oid;
                insertIndex.Parameters[1].Value = PersonalGdbSpatialIndex.GetGridCell(boundingBox.XMin, metadata.IdxOriginX, metadata.IdxGridSize);
                insertIndex.Parameters[2].Value = PersonalGdbSpatialIndex.GetGridCell(boundingBox.YMin, metadata.IdxOriginY, metadata.IdxGridSize);
                insertIndex.Parameters[3].Value = PersonalGdbSpatialIndex.GetGridCell(boundingBox.XMax, metadata.IdxOriginX, metadata.IdxGridSize);
                insertIndex.Parameters[4].Value = PersonalGdbSpatialIndex.GetGridCell(boundingBox.YMax, metadata.IdxOriginY, metadata.IdxGridSize);

                insertIndex.ExecuteNonQuery();

                extent = Union(extent, boundingBox);
            }

            oid++;

            count++;
        }

        UpdateExtent(connection, transaction, featureClassName, extent);

        transaction.Commit();

        return count;
    }

    private sealed record GeomColumnMetadata(
        string FieldName, EsriPGDBColumnShapeType ShapeType, int Srid,
        double IdxOriginX, double IdxOriginY, double IdxGridSize, BoundingBox Extent);

    private GeomColumnMetadata ReadGeomColumnMetadata(OleDbConnection connection, string featureClassName)
    {
        using var command = new OleDbCommand(
            $"SELECT FieldName, ShapeType, SRID, IdxOriginX, IdxOriginY, IdxGridSize, ExtentLeft, ExtentBottom, ExtentRight, ExtentTop " +
            $"FROM {PersonalGdbInfrastructure.GdbGeomColumnsTable} WHERE TableName = ?",
            connection);

        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, featureClassName);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            throw new InvalidOperationException($"Feature class '{featureClassName}' was not found in {MdbFileName}.");

        var extent = new BoundingBox(
            Convert.ToDouble(reader["ExtentLeft"]),
            Convert.ToDouble(reader["ExtentBottom"]),
            Convert.ToDouble(reader["ExtentRight"]),
            Convert.ToDouble(reader["ExtentTop"]));

        return new GeomColumnMetadata(
            (string)reader["FieldName"],
            (EsriPGDBColumnShapeType)Convert.ToInt32(reader["ShapeType"]),
            Convert.ToInt32(reader["SRID"]),
            Convert.ToDouble(reader["IdxOriginX"]),
            Convert.ToDouble(reader["IdxOriginY"]),
            Convert.ToDouble(reader["IdxGridSize"]),
            extent);
    }

    private static List<(string Name, OleDbType Type)> GetAttributeColumns(OleDbConnection connection, string featureClassName, string shapeFieldName)
    {
        var schema = connection.GetSchema("Columns", new string?[] { null, null, featureClassName, null });

        return schema.Rows.Cast<System.Data.DataRow>()
            .OrderBy(row => Convert.ToInt32(row["ORDINAL_POSITION"]))
            .Select(row => (Name: (string)row["COLUMN_NAME"], Type: (OleDbType)Convert.ToInt32(row["DATA_TYPE"])))
            .Where(column => !column.Name.EqualsIgnoreCase("OBJECTID") &&
                             !column.Name.EqualsIgnoreCase(shapeFieldName) &&
                             !column.Name.EqualsIgnoreCase("SHAPE_Length") &&
                             !column.Name.EqualsIgnoreCase("SHAPE_Area"))
            .ToList();
    }

    private static void ValidateGeometryType(GeometryType geometryType, EsriPGDBColumnShapeType shapeType, string featureClassName)
    {
        var isValid = shapeType switch
        {
            EsriPGDBColumnShapeType.Point => geometryType == GeometryType.Point,
            EsriPGDBColumnShapeType.Multipoint => geometryType == GeometryType.MultiPoint,
            EsriPGDBColumnShapeType.Polyline => geometryType is GeometryType.LineString or GeometryType.MultiLineString,
            EsriPGDBColumnShapeType.Polygon => geometryType is GeometryType.Polygon or GeometryType.MultiPolygon,
            _ => false,
        };

        if (!isValid)
            throw new ArgumentException($"Geometry type {geometryType} cannot be stored in the {shapeType} feature class '{featureClassName}'.");
    }

    private static object? GetAttributeValue(Feature<Point> feature, string columnName)
    {
        if (feature.Attributes is null)
            return null;

        if (feature.Attributes.TryGetValue(columnName, out var value))
            return value;

        var key = feature.Attributes.Keys.FirstOrDefault(k => k.EqualsIgnoreCase(columnName));

        return key is null ? null : feature.Attributes[key];
    }

    private static BoundingBox Union(BoundingBox current, BoundingBox addition)
    {
        // NaN (never written) and inverted (ESRI empty convention) extents count as empty
        if (current.IsNaN() || current.XMin > current.XMax || current.YMin > current.YMax)
            return addition;

        return new BoundingBox(
            Math.Min(current.XMin, addition.XMin),
            Math.Min(current.YMin, addition.YMin),
            Math.Max(current.XMax, addition.XMax),
            Math.Max(current.YMax, addition.YMax));
    }

    private static void UpdateExtent(OleDbConnection connection, OleDbTransaction transaction, string featureClassName, BoundingBox extent)
    {
        if (extent.IsNaN())
            return;

        using var command = new OleDbCommand(
            $"UPDATE {PersonalGdbInfrastructure.GdbGeomColumnsTable} SET ExtentLeft = ?, ExtentBottom = ?, ExtentRight = ?, ExtentTop = ? WHERE TableName = ?",
            connection, transaction);

        PersonalGdbSql.AddParameter(command, OleDbType.Double, extent.XMin);
        PersonalGdbSql.AddParameter(command, OleDbType.Double, extent.YMin);
        PersonalGdbSql.AddParameter(command, OleDbType.Double, extent.XMax);
        PersonalGdbSql.AddParameter(command, OleDbType.Double, extent.YMax);
        PersonalGdbSql.AddParameter(command, OleDbType.VarWChar, featureClassName);

        command.ExecuteNonQuery();
    }

    #endregion
}

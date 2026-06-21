using System.Data.OleDb;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.IO.Prj;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;
using IRI.Maptor.Ket.PersonalGdbPersistence.Xml;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Ket.PersonalGdbPersistence;

public static class PersonalGdbInfrastructure
{
    internal static readonly string GdbSpatialRefTable = "GDB_SpatialRefs";

    internal static readonly string GdbGeomColumnsTable = "GDB_GeomColumns";

    internal static readonly string GdbItemsTable = "GDB_Items";

    // Canonical ESRI schema namespace the Xml/* models are bound to. GDB 'Definition' XML
    // stamps this namespace with the authoring ArcGIS version (e.g. .../10.6, .../11.0); the
    // raw XML is normalized to this value before deserialization so any 10.x/11.x GDB matches.
    // Must be a const (not static readonly) so it can be referenced from [XmlType] attributes.
    public const string EsriSchemaNamespace = "http://www.esri.com/schemas/ArcGIS/10.8";

    // ACE OLEDB provider ids in preference order (newest first); resolved against the
    // providers actually registered on the machine, with the 12.0 string as a final fallback.
    private static readonly string[] _aceProviderPreference =
    {
        "Microsoft.ACE.OLEDB.16.0",
        "Microsoft.ACE.OLEDB.12.0",
    };

    // Resolved once, thread-safely: GetConnectionString can be called concurrently because
    // PersoanlGdbDataSource runs reads via Task.Run.
    private static readonly Lazy<string> _aceProvider = new(ResolveAceProvider);

    private static readonly System.Text.RegularExpressions.Regex _esriSchemaNamespaceRegex =
        new(@"http://www\.esri\.com/schemas/ArcGIS/[0-9.]+",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    // Rewrites any versioned ESRI schema namespace in the GDB 'Definition' XML to the canonical
    // EsriSchemaNamespace so deserialization is independent of the authoring ArcGIS version.
    internal static string NormalizeSchemaVersion(string? definitionXml) =>
        string.IsNullOrEmpty(definitionXml)
            ? definitionXml ?? string.Empty
            : _esriSchemaNamespaceRegex.Replace(definitionXml, EsriSchemaNamespace);

    // Picks an installed ACE OLEDB provider (preferring the newest), so the adapter works
    // whether the machine has the 12.0 or the 16.0 Access Database Engine. Falls back to 12.0
    // to preserve the previous behavior when provider enumeration yields nothing.
    private static string ResolveAceProvider()
    {
        try
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var providers = new OleDbEnumerator().GetElements())
            {
                foreach (System.Data.DataRow row in providers.Rows)
                {
                    var name = row["SOURCES_NAME"]?.ToString();

                    if (!string.IsNullOrEmpty(name))
                        installed.Add(name);
                }
            }

            var match = _aceProviderPreference.FirstOrDefault(installed.Contains);

            if (match is not null)
                return match;
        }
        catch (Exception)
        {
            // enumeration unavailable: fall through to the default below
        }

        return "Microsoft.ACE.OLEDB.12.0";
    }

    public static string GetConnectionString(string mdbFileName)
    {
        if (!System.IO.File.Exists(mdbFileName))
            throw new System.IO.FileNotFoundException("Personal geodatabase (.mdb) file was not found.", mdbFileName);

        return $"Provider={_aceProvider.Value};Data Source={mdbFileName};Persist Security Info=False;";
    }

    public static Dictionary<int, SrsBase> GetSpatialReferenceSystems(OleDbConnection connection)
    {
        try
        {
            //connection.Open();
            var query = FormattableString.Invariant(@$"SELECT SRID, SRTEXT FROM {GdbSpatialRefTable}");

            Dictionary<int, SrsBase> result = new Dictionary<int, SrsBase>();

            using (var cmd = new OleDbCommand(query, connection))
            using (var dataReader = cmd.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    if (dataReader["SRTEXT"] == DBNull.Value)
                        continue;

                    var srid = Convert.ToInt32(dataReader["SRID"]);
                    var srtext = dataReader["SRTEXT"].ToString()!;

                    var srsbase = EsriPrjFile.Parse(srtext).AsMapProjection();

                    // indexer (not Add) tolerates duplicate SRID rows in GDB_SpatialRefs
                    result[srid] = srsbase;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("PersonalGdbInfrastructure > GetSpatialReferenceSystems", ex);
        }
    }

    //var query = FormattableString.Invariant(
    //         @$"SELECT   Name, PhysicalName, Path, DatasetSubtype1, DatasetSubtype2, DatasetInfo1, Definition
    //                FROM    GDB_Items
    //                WHERE   (Definition LIKE '<DEFeatureDataset%') OR
    //                        (Definition LIKE '<DEFeatureClassInfo%')");


    private static List<GdbItem> GetGdbItems(OleDbConnection connection, string definitionStartsWith)
    {
        //< DEFeatureDataset
        try
        {
            // Item type is detected by the leading XML tag of the 'Definition' column. This
            // assumes the stored XML starts exactly with that tag (no <?xml?> prolog/BOM/leading
            // whitespace), which holds for ESRI-authored personal geodatabases.
            var query = FormattableString.Invariant(
                @$"SELECT   Name, PhysicalName, Path, Definition
                    FROM    {GdbItemsTable}
                    WHERE   (Definition LIKE '{definitionStartsWith}%')");

            using (var cmd = new OleDbCommand(query, connection))
            using (var dataReader = cmd.ExecuteReader())
            {
                List<GdbItem> result = new List<GdbItem>();

                while (dataReader.Read())
                {
                    GdbItem gdbItem = new GdbItem()
                    {
                        Name = dataReader[nameof(GdbItem.Name)].ToString()!,
                        PhysicalName = dataReader[nameof(GdbItem.PhysicalName)].ToString()!,
                        Path = dataReader[nameof(GdbItem.Path)].ToString()!,
                        Definition = dataReader[nameof(GdbItem.Definition)].ToString()!,
                    };

                    result.Add(gdbItem);
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("PersonalGdbInfrastructure > GetGdbItems", ex);
        }
    }

    public static List<GdbItem> GetFeatureDatasets(OleDbConnection connection)
    {
        // in 'GDB_Items' table, the 'Definition' column starts
        // with the '<DEFeatureDataset' strings for featureDatasets
        return GetGdbItems(connection, "<DEFeatureDataset");
    }

    public static List<GdbItem> GetFeatureClassInfo(OleDbConnection connection)
    {
        // in 'GDB_Items' table, the 'Definition' column starts
        // with the '<DEFeatureClassInfo' strings for featureDatasets
        return GetGdbItems(connection, "<DEFeatureClassInfo");
    }

    public static List<GdbItemColumnInfo> GetFieldInfo(string definition)
    {
        List<GdbItemColumnInfo> result = new List<GdbItemColumnInfo>();

        // parse xml field Definition and return list of field info
        // in 'GDB_Items' table, the 'Definition' column starts
        // with the '<DEFeatureClassInfo' strings for featureDatasets
        // <DEFeatureClassInfo

        var featureClassInfos = IRI.Maptor.Sta.Common.Helpers.XmlHelper.DeserializeFromXmlString<GdbXml_FeatureClass>(NormalizeSchemaVersion(definition));

        return featureClassInfos is null || featureClassInfos.GPFieldInfoExs is null
            ? result
            : featureClassInfos.GPFieldInfoExs.Items.Select(GdbItemColumnInfo.Parse).ToList();
    }

    public static List<GdbCodedValueDomain> GetAllDomains(OleDbConnection connection)
    {
        var items = GetGdbItems(connection, "<GPCodedValueDomain2");

        var result = new List<GdbCodedValueDomain>();

        foreach (var item in items)
        {
            var info = IRI.Maptor.Sta.Common.Helpers.XmlHelper.DeserializeFromXmlString<GdbXml_CodedValueDomain>(NormalizeSchemaVersion(item.Definition));

            // CodedValues can be null for a domain definition without a <CodedValues> node
            if (info is null || info.CodedValues is null || info.CodedValues.Items.IsNullOrEmpty())
                continue;

            result.Add(new GdbCodedValueDomain()
            {
                DomainName = info.DomainName,
                FieldType = info.FieldType,
                Values = info.CodedValues.Items.Select(c => new GdbCodedValue()
                {
                    Name = c.Name,
                    Code = c.Code
                }).ToList()
            });
        }

        return result.OrderBy(r => r.DomainName).ToList();
    }

    public static async Task<List<Field>> GetTableSchema(OleDbConnection connection, string tableName)
    {
        var fields = new List<Field>();

        // Get columns schema
        var columns = await connection.GetSchemaAsync("Columns", new[] { null, null, tableName });

        foreach (System.Data.DataRow row in columns.Rows)
        {
            var field = new Field()
            {
                Alias = string.Empty,
                Length = row["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 0 : int.Parse(row["CHARACTER_MAXIMUM_LENGTH"].ToString()!),
                Name = row["COLUMN_NAME"].ToString(),

                // todo: add another field for sql server specific types
                // and use .net type for TypeFullName
                TypeFullName = OleDbTypeToSqlServerType(Convert.ToInt32(row["DATA_TYPE"])),
                IsNullable = row["IS_NULLABLE"].ToString() == "YES",
                Scale = row["NUMERIC_SCALE"] == DBNull.Value ? 0 : int.Parse(row["NUMERIC_SCALE"].ToString()!),
                Precision = row["NUMERIC_PRECISION"] == DBNull.Value ? 0 : int.Parse(row["NUMERIC_PRECISION"].ToString()!),
                DateTimePrecision = row["DATETIME_PRECISION"] == DBNull.Value ? 0 : int.Parse(row["DATETIME_PRECISION"].ToString()!)
            };

            //field.Type = OleDbTypeToSqlServerType(byte.Parse(row["DATA_TYPE"].ToString()!), field.Length, field.Precision, field.Scale, field.DateTimePrecision);

            fields.Add(field);
        }

        return fields.OrderBy(i => i.Name).ToList();
    }

    public static string OleDbTypeToSqlServerType(int oleDbType)
    {
        switch (oleDbType)
        {
            // Numeric Types
            case 2:   // adSmallInt (2-byte signed integer) (DBTYPE_I2).
                return "smallint";

            case 3:   // adInteger (4-byte signed integer) (DBTYPE_I4).
                return "int";

            case 4:   // adSingle (32-bit floating-point) (DBTYPE_R4).
                return "real";

            case 5:   // adDouble (64-bit floating-point) (DBTYPE_R8).
                return "float";

            case 6:   // adCurrency (Fixed-point decimal, 4 decimal places) (DBTYPE_CY).
                return "money";

            // Boolean
            case 11:  // adBoolean (True/False) (DBTYPE_BOOL).
                return "bit";

            case 14:  // adDecimal (Exact numeric, precision/scale) (DBTYPE_DECIMAL).
                //return $"decimal({preceision}, {scale})";
                return $"decimal";

            case 16:  // adTinyInt (1-byte signed integer, -128 to 127) (DBTYPE_I1).
                return "smallint";  // SQL Server lacks 1-byte signed, closest match

            case 17:  // adUnsignedTinyInt (1-byte unsigned integer, 0-255) (DBTYPE_UI1).
                return "tinyint";

            case 18: // adUnsignedSmallInt (2-byte unsigned integer, 0-65535) (DBTYPE_UI2).
                return "int";

            case 19:  // adUnsignedInt (4 -byte unsigned integer, 0-4294967295) (DBTYPE_UI4).
                return "bigint";

            case 20:  // adBigInt (8 -byte signed integer) (DBTYPE_I8).
                return "bigint";  // SQL Server lacks unsigned bigint

            case 21:  // adUnsignedBigInt (8 -byte unsigned integer) (DBTYPE_UI8).
                return "decimal(38, 0)"; // SQL Server lacks unsigned bigint

            case 131: // adNumeric (Exact numeric, deprecated alias for adDecimal) (DBTYPE_NUMERIC).
                //return $"decimal({preceision}, {scale})";
                return $"decimal";

            // Date/Time Types
            case 7:   // adDate (Date, OLE Automation format) (DBTYPE_DATE).
            case 133: // adDBDate (Date, yyyymmdd) Indicates a date value (yyyymmdd) (DBTYPE_DBDATE).
                return "date";

            case 134: // adDBTime (Time, hhmmss) Indicates a time value (hhmmss) (DBTYPE_DBTIME).
                return "time";

            case 135: // adDBTimeStamp (DateTime, yyyymmddhhmmss)
                //return datePreceision > 0 ? $"datetime2{datePreceision}" : "datetime2";
                return "datetime2";

            //// String Types          
            case 8:   // adBSTR (Unicode string, fixed-length) - Indicates a null-terminated character string (Unicode) (DBTYPE_BSTR).
                      //return $"nchar({length})";
                return $"nchar";

            case 130: // adWChar (Unicode string, fixed-length) - (DBTYPE_WSTR).
            case 202: // adVarWChar (Unicode long string)
            case 203: // adLongVarWChar (Unicode long string) - (DBTYPE_WSTR).
                //return length > 4000 ? "nvarchar(max)" : $"nvarchar({length})";
                return "nvarchar";

            case 201: // adLongVarChar                                          - Indicates a long string value.
            case 200: // adVarChar (Non-Unicode string, variable-length)            - Indicates a string value.
                //return length > 8000 ? "varchar(max)" : $"varchar({length})";
                return "varchar";

            // Binary Types
            case 128: // adBinary (Binary, fixed-length)  (DBTYPE_BYTES).
                      //return length > 8000 ? "varbinary(max)" : $"binary({length})";
                return "varbinary";

            case 129: // adChar (Non-Unicode string, fixed-length) (DBTYPE_STR).
                //return $"char({length})";
                return "char";

            case 204: // adVarBinary (Binary, variable-length)
                      //return length > 8000 ? "varbinary(max)" : $"varbinary({length})";
                return "varbinary";

            case 205: // adLongVarBinary (Binary, variable-length) 
                return "varbinary";

            // GUID
            case 72:  // adGUID (Globally Unique Identifier) (DBTYPE_GUID).
                return "uniqueidentifier";

            default:
                return $"unknown (OleDbType: {oleDbType})";
        }
    }
}

using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;
using IRI.Maptor.Ket.PersonalGdbPersistence.Xml;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System.Data.OleDb;

namespace IRI.Maptor.Ket.PersonalGdbPersistence;

public static class PersonalGdbInfrastructure
{
    internal static readonly string GdbSpatialRefTable = "GDB_SpatialRefs";

    internal static readonly string GdbGeomColumnsTable = "GDB_GeomColumns";

    public static string GetConnectionString(string mdbFileName)
    {
        if (System.IO.File.Exists(mdbFileName))
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={mdbFileName};Persist Security Info=False;";
        }
        else
        {
            return string.Empty;
        }
    }

    public static Dictionary<int, SrsBase> GetSpatialReferenceSystems(OleDbConnection connection)
    {
        try
        {
            //connection.Open();
            var query = FormattableString.Invariant(@$"SELECT SRID, SRTEXT FROM {GdbSpatialRefTable}");

            var cmd = new OleDbCommand(query, connection);

            Dictionary<int, SrsBase> result = new Dictionary<int, SrsBase>();

            using (var dataReader = cmd.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    var srid = (int)dataReader["SRID"];
                    var srtext = (string)dataReader["SRTEXT"];

                    var srsbase = IRI.Maptor.Sta.ShapefileFormat.Prj.EsriPrjFile.Parse(srtext).AsMapProjection();

                    result.Add(srid, srsbase);
                }
            }

            return result;
        }
        catch (Exception)
        {
            throw new NotImplementedException("PersonalGdbInfrastructure > GetSpatialReferenceSystems");
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
            var query = FormattableString.Invariant(
                @$"SELECT   Name, PhysicalName, Path, Definition
                    FROM    GDB_Items
                    WHERE   (Definition LIKE '{definitionStartsWith}%')");

            using (var cmd = new OleDbCommand(query, connection))
            {
                List<GdbItem> result = new List<GdbItem>();

                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        try
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
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            throw new NotImplementedException("PersonalGdbInfrastructure > GetGdbItems");
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

        var featureClassInfos = IRI.Maptor.Sta.Common.Helpers.XmlHelper.DeserializeFromXmlString<GdbXml_FeatureClass>(definition);

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
            try
            {

                var info = IRI.Maptor.Sta.Common.Helpers.XmlHelper.DeserializeFromXmlString<GdbXml_CodedValueDomain>(item.Definition);

                if (info is null || info.CodedValues.Items.IsNullOrEmpty())
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
            catch (Exception ex)
            {

                throw;
            }
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
                Type = OleDbTypeToSqlServerType(byte.Parse(row["DATA_TYPE"].ToString()!)),
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

    private static string GetDataTypeName(int oleDbType)
    {
        // Map OLEDB type to friendly name
        return oleDbType switch
        {
            2 => "smallInt",
            3 => "int",
            4 => "Single",
            5 => "Double",
            6 => "Currency",
            7 => "Date",
            11 => "Boolean",
            17 => "Byte",
            72 => "GUID",
            128 => "Binary",
            130 => "Text",
            131 => "Decimal",
            133 => "Date",
            134 => "Time",
            135 => "DateTime",
            _ => $"Unknown ({oleDbType})"
        };
    }

    //private static string OleDbTypeToSqlServerType(int oleDbType, int length, int preceision, int scale, int datePreceision)
    //{
    //    switch (oleDbType)
    //    {
    //        // Numeric Types
    //        case 2:  // SmallInt
    //            return "smallint";

    //        case 20:
    //            return "bigint";

    //        case 3:  // Int
    //            return "int";

    //        case 4:  // Single (Float)
    //            return "real";

    //        case 5:  // Double
    //            return "float";

    //        case 6:  // Currency (Money)
    //            return "money";

    //        case 17: // Byte (TinyInt)
    //            return "tinyint";

    //        case 131: // Decimal/Numeric
    //            return scale != 0 ? $"decimal({scale}, {preceision})" : "decimal(18, 2)";

    //        // Date/Time Types
    //        case 7:  // Date (Legacy)
    //        case 133: // Date (SQL Server)
    //            return "date";

    //        case 134: // Time
    //            return "time";

    //        case 135: // DateTime
    //            return $"datetime2({datePreceision})";

    //        // String Types
    //        case 130: // Text (VarChar)
    //            return length <= 8000 ? $"varchar({length})" : "varchar(MAX)";

    //        case 202: // Wide Text (NVarChar)
    //            return length <= 4000 ? $"nvarchar({length})" : "nvarchar(MAX)";

    //        case 203: // Memo (Long Text)
    //            return "nvarchar(MAX)";

    //        // Binary Types
    //        case 128: // Binary (VarBinary)
    //            return length <= 8000 ? $"varbinary({length})" : "varbinary(MAX)";

    //        case 204: // Long Binary (OLE Object)
    //            return "varbinary(MAX)";

    //        case 72:  // GUID
    //            return "uniqueidentifier";

    //        // Boolean
    //        case 11: // Boolean (Bit)
    //            return "bit";

    //        // Fallback
    //        default:
    //            return $"UNKNOWN (OleDbType: {oleDbType})";
    //    }
    //}

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

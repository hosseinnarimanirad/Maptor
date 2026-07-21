using System.Data.OleDb;

using IRI.Maptor.Ket.PersonalGdbPersistence.Enums;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;

namespace IRI.Maptor.Ket.PersonalGdbPersistence.Write;

internal static class PersonalGdbSql
{
    internal static int ExecuteNonQuery(OleDbConnection connection, OleDbTransaction transaction, string sql)
    {
        using var command = new OleDbCommand(sql, connection, transaction);

        return command.ExecuteNonQuery();
    }

    // MAX(column)+1 over a possibly empty table
    internal static int GetNextId(OleDbConnection connection, OleDbTransaction transaction, string tableName, string columnName)
    {
        using var command = new OleDbCommand($"SELECT MAX({columnName}) FROM [{tableName}]", connection, transaction);

        var result = command.ExecuteScalar();

        return (result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result)) + 1;
    }

    internal static OleDbParameter AddParameter(OleDbCommand command, OleDbType type, object? value)
    {
        var parameter = command.Parameters.Add($"@p{command.Parameters.Count}", type);

        parameter.Value = value ?? DBNull.Value;

        return parameter;
    }

    // Jet DDL builders for the feature-class data table and its spatial-index side table.
    // ESRI's layout: OBJECTID + SHAPE (+ SHAPE_Length/SHAPE_Area) + user fields, no primary
    // key, a single unique index named FDO_OBJECTID, and a '<Name>_SHAPE_Index' grid table
    // with one non-unique index per column except the last.
    internal static IEnumerable<string> BuildCreateFeatureClassDdl(
        string name, bool hasLengthField, bool hasAreaField, IReadOnlyList<PersonalGdbField>? userFields)
    {
        var columns = new List<string> { "OBJECTID COUNTER NOT NULL", "SHAPE LONGBINARY" };

        if (hasLengthField)
            columns.Add("SHAPE_Length DOUBLE");

        if (hasAreaField)
            columns.Add("SHAPE_Area DOUBLE");

        if (userFields is not null)
            columns.AddRange(userFields.Select(f => $"[{f.Name}] {GetAccessDdlType(f.FieldType, f.Length)}{(f.IsNullable ? string.Empty : " NOT NULL")}"));

        yield return $"CREATE TABLE [{name}] ({string.Join(", ", columns)})";

        yield return $"CREATE UNIQUE INDEX FDO_OBJECTID ON [{name}] (OBJECTID)";

        yield return $"CREATE TABLE [{name}_SHAPE_Index] (IndexedObjectId LONG, MinGX LONG, MinGY LONG, MaxGX LONG, MaxGY LONG)";

        yield return $"CREATE INDEX IndexedObjectId_Index ON [{name}_SHAPE_Index] (IndexedObjectId)";

        yield return $"CREATE INDEX MinGX_Index ON [{name}_SHAPE_Index] (MinGX)";

        yield return $"CREATE INDEX MinGY_Index ON [{name}_SHAPE_Index] (MinGY)";

        yield return $"CREATE INDEX MaxGX_Index ON [{name}_SHAPE_Index] (MaxGX)";

        yield return $"CREATE INDEX MaxGY_Index ON [{name}_SHAPE_Index] (MaxGY)";
    }

    internal static string GetAccessDdlType(GdbEsriFieldType fieldType, int length)
    {
        switch (fieldType)
        {
            case GdbEsriFieldType.esriFieldTypeSmallInteger:
                return "SHORT";

            case GdbEsriFieldType.esriFieldTypeInteger:
                return "LONG";

            case GdbEsriFieldType.esriFieldTypeSingle:
                return "SINGLE";

            case GdbEsriFieldType.esriFieldTypeDouble:
                return "DOUBLE";

            case GdbEsriFieldType.esriFieldTypeString:
                return length is > 0 and <= 255 ? FormattableString.Invariant($"TEXT({length})") : "LONGTEXT";

            case GdbEsriFieldType.esriFieldTypeDate:
                return "DATETIME";

            case GdbEsriFieldType.esriFieldTypeGUID:
                return "GUID";

            case GdbEsriFieldType.esriFieldTypeBlob:
                return "LONGBINARY";

            default:
                throw new NotSupportedException($"Field type {fieldType} is not supported in a personal geodatabase feature class.");
        }
    }
}

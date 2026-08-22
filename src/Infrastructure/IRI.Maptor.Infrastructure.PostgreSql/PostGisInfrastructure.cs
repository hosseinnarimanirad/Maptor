using IRI.Maptor.Core.Persistence.Infrastructure;
using Npgsql;
using System.Data;

namespace IRI.Maptor.Infrastructure.PostgreSql;

public class PostgreSqlInfrastructure : DataSourceInfrastructure
{
    NpgsqlConnection connection;        //"localhost", "postgres", "sa123456", "farsiDatabase", "5432"

    public PostgreSqlInfrastructure(string connectionString)
    {
        this.connection = new NpgsqlConnection(connectionString);
    }
    
    public PostgreSqlInfrastructure(string hostName, string userName, string password, string database, string port)
    {
        this.connection = new NpgsqlConnection(GetConnectionString(hostName, userName, password, database, port));
    }

    public static string GetConnectionString(string hostName, string userName, string password, string database, string port)
    {
        return string.Format("Server={0}; UID={1}; PWD={2}; Database={3}; Port={4}", hostName, userName, password, database, port);
    }

    /// <summary>
    /// not completed
    /// </summary>
    /// <returns></returns>
    public static List<string> GetAllTypes()
    {
        return new List<string>() 
        { 
            "boolean",
            "char",
            "varchar",
            "text",
            "smallint",
            "int",
            "serial",
            "float",
            "geometry"
        };
    }

    public void ExecuteNonQuery(string commandString)
    {
        NpgsqlCommand command = new NpgsqlCommand(commandString, connection);

        connection.Open();

        command.ExecuteNonQuery();

        connection.Close();
    }

    public bool Exists(string tableName)
    {
        string command = string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'", tableName.ToLower());

        connection.Open();

        var result = ((Int64)new NpgsqlCommand(command, connection).ExecuteScalar()) > 0;

        connection.Close();

        return result;
    }



    public void Insert(string tableName, DataTable sourceTable, List<string> targetColumnNames, List<DbType> types, List<Func<DataRow, object>> mapFunctions, List<Func<object, object>> targetColumnMaps = null)
    {
        if (!Exists(tableName))
        {
            throw new NotImplementedException();
        }

        connection.Open();

        string insertCommandString;

        if (targetColumnMaps == null)
        {
            insertCommandString = string.Format("INSERT INTO {0}({1}) VALUES({2})", tableName,
          string.Join(", ", targetColumnNames),
          string.Join(", ", targetColumnNames.Select(i => "@" + i)));
        }
        else
        {
            insertCommandString = string.Format("INSERT INTO {0}({1}) VALUES({2})", tableName,
                 string.Join(", ", targetColumnNames),
                 string.Join(", ", targetColumnMaps.Zip(targetColumnNames, (func, s) => func(s))));
        }

        NpgsqlCommand insertCommand = new NpgsqlCommand(insertCommandString, connection);

        foreach (DataRow item in sourceTable.Rows)
        {
            insertCommand.Parameters.Clear();

            for (int i = 0; i < targetColumnNames.Count; i++)
            {
                NpgsqlParameter parameter = new NpgsqlParameter("@" + targetColumnNames[i], types[i]);

                parameter.Value = mapFunctions[i](item);

                insertCommand.Parameters.Add(parameter);
            }

            insertCommand.ExecuteNonQuery();
        }

        connection.Close();
    }

    public void Insert<T>(string tableName, List<T> sourceValues, List<string> targetColumnNames, List<DbType> types, List<Func<T, object>> mapFunctions)
    {
        if (!Exists(tableName))
        {
            throw new NotImplementedException();
        }

        connection.Open();

        string insertCommandString = string.Format("INSERT INTO {0}({1}) VALUES({2})", tableName,
           string.Join(", ", targetColumnNames),
           string.Join(", ", targetColumnNames.Select(i => "@" + i)));

        NpgsqlCommand insertCommand = new NpgsqlCommand(insertCommandString, connection);

        foreach (T item in sourceValues)
        {
            insertCommand.Parameters.Clear();

            for (int i = 0; i < targetColumnNames.Count; i++)
            {
                object value = mapFunctions[i](item);

                NpgsqlParameter parameter = new NpgsqlParameter("@" + targetColumnNames[i], types[i]);

                parameter.Value = value;

                insertCommand.Parameters.Add(parameter);
            }

            insertCommand.ExecuteNonQuery();
        }

        connection.Close();
    }

    //*****************************THIRD PARTY CODE SAMPLE. DIFFERENT APPROACH
    private void ExecuteCommand(string commandType, string commandString)
    {
        try
        {
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();

            info.FileName = @"C:\Program Files\PostgreSQL\9.2\bin\" + commandType + ".exe";

            info.Arguments = commandString;

            info.CreateNoWindow = true;

            info.UseShellExecute = true;

            System.Diagnostics.Process process = new System.Diagnostics.Process();

            process.StartInfo = info;

            process.Start();

            process.WaitForExit();

        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    private void PostGisToShapefile(string shapefilePath, string table, string database)
    {
        string commandString = "-u postgres -p sa123456 " + database + " " + table + " -f " + shapefilePath + "\\" + table + ".shp ";

        ExecuteCommand("pgsql2shp", commandString);
    }

    private void ShapefileToPostGis(string shapefilePath, string table, string database)
    {
        string commandString = "-l -D " + shapefilePath + " " + table + " |psql " + database;

        ExecuteCommand("shp2pgsql", commandString);
    }
    //************************************************************************

    public void CreateTable(string createTableCommand)
    {
        ExecuteNonQuery(createTableCommand);
    }

    public static void ExecuteNonQuery(string commandString, string connectionString)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        NpgsqlCommand command = new NpgsqlCommand(commandString, connection);

        connection.Open();

        command.ExecuteNonQuery();

        connection.Close();
    }

    public static bool Exists(string tableName, string connectionString)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        string command = string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'", tableName.ToLower());

        connection.Open();

        var result = ((Int64)new NpgsqlCommand(command, connection).ExecuteScalar()) > 0;

        connection.Close();

        return result;
    }

    public static List<string> GetTableNames(string connectionString, string schema)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        string command = string.Format("SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE table_schema='{0}' AND table_type='BASE TABLE'", schema);

        connection.Open();

        var reader = new NpgsqlCommand(command, connection).ExecuteReader();

        List<string> result = new List<string>();

        while (reader.Read())
        {
            result.Add(reader[0].ToString());
        }

        connection.Close();

        return result;
    }

    public static List<string> GetColumnNames(string connectionString, string schema, string tableName)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        string command = string.Format("SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema='{0}' AND table_name = '{1}'", schema, tableName);

        connection.Open();

        var reader = new NpgsqlCommand(command, connection).ExecuteReader();

        List<string> result = new List<string>();

        while (reader.Read())
        {
            result.Add(reader[0].ToString());
        }

        connection.Close();

        return result;
    }

    public static List<string> GetSchemas(string connectionString)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        string command = string.Format("SELECT DISTINCT(table_schema) FROM INFORMATION_SCHEMA.TABLES");

        connection.Open();

        var reader = new NpgsqlCommand(command, connection).ExecuteReader();

        List<string> result = new List<string>();

        while (reader.Read())
        {
            result.Add(reader[0].ToString());
        }

        connection.Close();

        return result;
    }

    public static IList<string> GetColumnTypes(string connectionString, string schema, string tableName)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        string command = string.Format("SELECT data_type FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema='{0}' AND table_name = '{1}'", schema, tableName);

        connection.Open();

        var reader = new NpgsqlCommand(command, connection).ExecuteReader();

        List<string> result = new List<string>();

        while (reader.Read())
        {
            result.Add(reader[0].ToString());
        }

        connection.Close();

        return result;
    }

    public static void Delete(string tableName, string connectionString)
    {
        if (Exists(tableName, connectionString))
        {
            ExecuteNonQuery(string.Format("DROP TABLE {0}", tableName), connectionString);
        }
    }

    public static string TestConnection(string connectionString)
    {
        NpgsqlConnection connection = new NpgsqlConnection();

        try
        {
            connection = new NpgsqlConnection(connectionString);

            connection.Open();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            connection.Close();
        }

        return "Connected Successfully";
    }
}

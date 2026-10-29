using System.Threading.Tasks;

using Npgsql;

namespace IRI.Maptor.Tests.PostgreSql;

/// <summary>
/// Locates the PostGIS server used by the integration tests. The connection string comes from the
/// <c>MAPTOR_POSTGIS_TEST_CONNECTION</c> environment variable, or defaults to the local Docker container
/// (<c>docker run -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgis/postgis:17-3.5</c>). When the server is
/// unreachable, tests marked <see cref="PostGisFactAttribute"/> are skipped rather than failed.
/// </summary>
public static class PostGisTestDatabase
{
    public const string EnvironmentVariable = "MAPTOR_POSTGIS_TEST_CONNECTION";

    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    private static readonly Lazy<string?> _unavailableReason = new(ProbeServer);

    /// <summary>Connection string of the server's maintenance database (used to create/drop test databases).</summary>
    public static string ServerConnectionString { get; } =
        Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } fromEnvironment
            ? fromEnvironment
            : DefaultConnectionString;

    /// <summary>Null when the server is reachable; otherwise why the PostGIS tests are skipped.</summary>
    public static string? UnavailableReason => _unavailableReason.Value;

    /// <summary>Creates an empty database with the postgis extension and returns its connection string.</summary>
    public static async Task<string> CreateDatabaseAsync(string databaseName)
    {
        await using (var connection = new NpgsqlConnection(ServerConnectionString))
        {
            await connection.OpenAsync();
            await Execute(connection, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
            await Execute(connection, $"CREATE DATABASE \"{databaseName}\"");
        }

        var connectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = databaseName }.ConnectionString;

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await Execute(connection, "CREATE EXTENSION IF NOT EXISTS postgis");
        }

        return connectionString;
    }

    public static async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(ServerConnectionString);
        await connection.OpenAsync();
        await Execute(connection, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string? ProbeServer()
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(ServerConnectionString) { Timeout = 3 };
            using var connection = new NpgsqlConnection(builder.ConnectionString);
            connection.Open();

            using var command = new NpgsqlCommand("SELECT count(*) FROM pg_available_extensions WHERE name = 'postgis'", connection);
            return (long)command.ExecuteScalar()! > 0
                ? null
                : "The PostgreSQL server has no postgis extension available.";
        }
        catch (Exception ex)
        {
            return $"PostGIS test server not reachable ({ex.GetType().Name}); set {EnvironmentVariable} or start the Docker container.";
        }
    }
}

/// <summary>A <see cref="FactAttribute"/> that is skipped when <see cref="PostGisTestDatabase"/> is unreachable.</summary>
public sealed class PostGisFactAttribute : FactAttribute
{
    public PostGisFactAttribute()
    {
        if (PostGisTestDatabase.UnavailableReason is { } reason)
            Skip = reason;
    }
}

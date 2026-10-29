using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Infrastructure;

/// <summary>
/// Applies the Npgsql type plugin (<see cref="NpgsqlMaptorGeometryExtensions.UseMaptor"/>) to the data source that the
/// EF Core provider builds from the connection string.
/// </summary>
public class MaptorGeometryDataSourceConfigurationPlugin : INpgsqlDataSourceConfigurationPlugin
{
    public virtual void Configure(NpgsqlDataSourceBuilder npgsqlDataSourceBuilder)
        => npgsqlDataSourceBuilder.UseMaptor();
}

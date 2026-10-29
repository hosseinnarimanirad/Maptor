using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Conventions;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Infrastructure;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Storage;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;

/// <summary>
/// Entry points that wire IRI.Maptor <c>Geometry&lt;Point&gt;</c> support into the Npgsql EF Core provider.
/// This is the replacement for the Npgsql provider's <c>UseNetTopologySuite()</c>.
/// </summary>
public static class NpgsqlMaptorGeometryDbContextOptionsExtensions
{
    /// <summary>
    /// Enables mapping of <c>Geometry&lt;Point&gt;</c> properties to PostGIS <c>geometry</c>/<c>geography</c> columns.
    /// Call inside <c>UseNpgsql(connectionString, x =&gt; x.UseMaptor())</c>. When the context is given a ready-made
    /// <c>NpgsqlDataSource</c> instead of a connection string, also call <c>UseMaptor()</c> on that data source's builder.
    /// </summary>
    public static NpgsqlDbContextOptionsBuilder UseMaptor(this NpgsqlDbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;

        var extension = coreOptionsBuilder.Options.FindExtension<NpgsqlMaptorGeometryOptionsExtension>()
            ?? new NpgsqlMaptorGeometryOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    /// <summary>
    /// Registers the Maptor plugins with the EF Core internal service provider. Applications normally call
    /// <see cref="UseMaptor"/> instead, which invokes this during option application.
    /// </summary>
    public static IServiceCollection AddEntityFrameworkNpgsqlMaptorGeometry(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        new EntityFrameworkNpgsqlServicesBuilder(serviceCollection)
            .TryAdd<INpgsqlDataSourceConfigurationPlugin, MaptorGeometryDataSourceConfigurationPlugin>()
            .TryAdd<IRelationalTypeMappingSourcePlugin, NpgsqlMaptorGeometryTypeMappingSourcePlugin>()
            .TryAdd<IConventionSetPlugin, PostGisConventionSetPlugin>();

        return serviceCollection;
    }
}

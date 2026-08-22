using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

using IRI.Maptor.Infrastructure.EfCore.Infrastructure;
using IRI.Maptor.Infrastructure.EfCore.Storage;

namespace IRI.Maptor.Infrastructure.EfCore.Extensions;

/// <summary>
/// Entry points that wire IRI.Maptor <c>Geometry&lt;Point&gt;</c> support into the SQL Server EF Core provider.
/// This is the replacement for <c>UseNetTopologySuite()</c>.
/// </summary>
public static class SqlServerMaptorGeometryDbContextOptionsExtensions
{
    /// <summary>
    /// Enables mapping of <c>Geometry&lt;Point&gt;</c> properties to SQL Server <c>geometry</c>/<c>geography</c> columns.
    /// Call inside <c>UseSqlServer(connectionString, x =&gt; x.UseMaptorGeometry())</c>.
    /// </summary>
    public static SqlServerDbContextOptionsBuilder UseMaptor(this SqlServerDbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;

        var extension = coreOptionsBuilder.Options.FindExtension<SqlServerMaptorGeometryOptionsExtension>()
            ?? new SqlServerMaptorGeometryOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    /// <summary>
    /// Registers the Maptor spatial type-mapping plugin with the EF Core internal service provider. Applications
    /// normally call <see cref="UseMaptor"/> instead, which invokes this during option application.
    /// </summary>
    public static IServiceCollection AddEntityFrameworkSqlServerMaptorGeometry(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<IRelationalTypeMappingSourcePlugin, SqlServerMaptorGeometryTypeMappingSourcePlugin>();

        return serviceCollection;
    }
}

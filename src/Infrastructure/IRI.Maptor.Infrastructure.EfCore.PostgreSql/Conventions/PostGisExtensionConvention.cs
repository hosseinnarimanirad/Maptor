using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Conventions;

/// <summary>
/// Adds the <c>postgis</c> extension to any model that has a <see cref="Geometry{Point}"/> property, so migrations
/// (and <c>EnsureCreated</c>) run <c>CREATE EXTENSION IF NOT EXISTS postgis</c> before creating spatial columns.
/// </summary>
public class PostGisExtensionConvention : IModelFinalizingConvention
{
    public const string PostGisExtensionName = "postgis";

    public virtual void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var hasSpatialProperty = modelBuilder.Metadata.GetEntityTypes()
            .SelectMany(entityType => entityType.GetDeclaredProperties())
            .Any(property => property.ClrType == typeof(Geometry<Point>));

        if (hasSpatialProperty)
            modelBuilder.HasPostgresExtension(PostGisExtensionName);
    }
}

/// <summary>Registers <see cref="PostGisExtensionConvention"/> with the model's convention set.</summary>
public class PostGisConventionSetPlugin : IConventionSetPlugin
{
    public virtual ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.Add(new PostGisExtensionConvention());

        return conventionSet;
    }
}

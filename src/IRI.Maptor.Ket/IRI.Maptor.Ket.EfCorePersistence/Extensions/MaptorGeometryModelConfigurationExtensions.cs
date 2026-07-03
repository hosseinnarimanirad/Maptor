using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.EfCorePersistence.Extensions;

/// <summary>
/// Model-building helpers that hide the SQL Server spatial column-type strings from application <c>DbContext</c>s.
/// </summary>
public static class MaptorGeometryModelConfigurationExtensions
{
    public const string GeographyStoreType = "geography";
    public const string GeometryStoreType = "geometry";

    /// <summary>
    /// Call from <c>DbContext.ConfigureConventions</c> to map every <see cref="Geometry{Point}"/> property to the
    /// given SQL Server spatial column type (defaults to <c>geography</c>). Replaces per-context
    /// <c>Properties&lt;NetTopologySuite.Geometries.Geometry&gt;().HaveColumnType("GEOGRAPHY")</c> conventions.
    /// </summary>
    public static ModelConfigurationBuilder ConfigureMaptorGeometry(
        this ModelConfigurationBuilder configurationBuilder,
        string storeType = GeographyStoreType)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<Geometry<Point>>().HaveColumnType(storeType);

        return configurationBuilder;
    }

    /// <summary>Maps a single <see cref="Geometry{Point}"/> property to a SQL Server <c>geography</c> column.</summary>
    public static PropertyBuilder<Geometry<Point>> IsGeography(this PropertyBuilder<Geometry<Point>> propertyBuilder)
        => propertyBuilder.HasColumnType(GeographyStoreType);

    /// <summary>Maps a single <see cref="Geometry{Point}"/> property to a SQL Server <c>geometry</c> column.</summary>
    public static PropertyBuilder<Geometry<Point>> IsGeometry(this PropertyBuilder<Geometry<Point>> propertyBuilder)
        => propertyBuilder.HasColumnType(GeometryStoreType);
}

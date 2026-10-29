using Npgsql.TypeMapping;

using IRI.Maptor.Infrastructure.EfCore.PostgreSql.TypeHandling;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;

/// <summary>
/// ADO.NET entry point: teaches Npgsql to read and write PostGIS <c>geometry</c>/<c>geography</c> as
/// IRI.Maptor <c>Geometry&lt;Point&gt;</c>. This is the replacement for Npgsql's <c>UseNetTopologySuite()</c>.
/// </summary>
public static class NpgsqlMaptorGeometryExtensions
{
    /// <summary>
    /// Maps <c>Geometry&lt;Point&gt;</c> to PostGIS <c>geometry</c> (the default for untyped parameters) and
    /// <c>geography</c>. Call on an <c>NpgsqlDataSourceBuilder</c> before building the data source. Values read
    /// with Z or M lose those ordinates.
    /// </summary>
    public static INpgsqlTypeMapper UseMaptor(this INpgsqlTypeMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        mapper.AddTypeInfoResolverFactory(new MaptorGeometryTypeInfoResolverFactory());

        return mapper;
    }
}

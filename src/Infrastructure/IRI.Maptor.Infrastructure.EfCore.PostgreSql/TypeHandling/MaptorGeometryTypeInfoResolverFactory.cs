using Npgsql.Internal;
using Npgsql.Internal.Postgres;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.TypeHandling;

/// <summary>
/// Registers <see cref="Geometry{Point}"/> (and arrays of it) for the PostGIS <c>geometry</c> and <c>geography</c>
/// types. <c>geometry</c> is the default when a parameter does not name its data type.
/// </summary>
internal sealed class MaptorGeometryTypeInfoResolverFactory : PgTypeInfoResolverFactory
{
    internal const string GeometryDataTypeName = "geometry";
    internal const string GeographyDataTypeName = "geography";

    public override IPgTypeInfoResolver CreateResolver() => new Resolver();

    public override IPgTypeInfoResolver CreateArrayResolver() => new ArrayResolver();

    private class Resolver : IPgTypeInfoResolver
    {
        private TypeInfoMappingCollection? _mappings;

        protected TypeInfoMappingCollection Mappings => _mappings ??= AddMappings(new());

        public PgTypeInfo? GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
            => Mappings.Find(type, dataTypeName, options);

        private static TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
        {
            mappings.AddType<Geometry<Point>>(GeometryDataTypeName,
                static (options, mapping, _) => mapping.CreateInfo(options, new MaptorGeometryConverter()),
                isDefault: true);

            // Matched by data type name alone, so geography columns also read through GetValue()/object;
            // only geometry is the default for an untyped Geometry<Point> parameter.
            mappings.AddType<Geometry<Point>>(GeographyDataTypeName,
                static (options, mapping, _) => mapping.CreateInfo(options, new MaptorGeometryConverter()),
                MatchRequirement.DataTypeName);

            return mappings;
        }
    }

    private sealed class ArrayResolver : Resolver, IPgTypeInfoResolver
    {
        private TypeInfoMappingCollection? _arrayMappings;

        private TypeInfoMappingCollection ArrayMappings => _arrayMappings ??= AddArrayMappings(new(Mappings));

        PgTypeInfo? IPgTypeInfoResolver.GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
            => ArrayMappings.Find(type, dataTypeName, options);

        private static TypeInfoMappingCollection AddArrayMappings(TypeInfoMappingCollection mappings)
        {
            mappings.AddArrayType<Geometry<Point>>(GeometryDataTypeName);
            mappings.AddArrayType<Geometry<Point>>(GeographyDataTypeName);

            return mappings;
        }
    }
}

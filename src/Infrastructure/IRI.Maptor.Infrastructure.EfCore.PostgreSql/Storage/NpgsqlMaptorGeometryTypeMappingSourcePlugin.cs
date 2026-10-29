using Microsoft.EntityFrameworkCore.Storage;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Storage;

/// <summary>
/// Supplies the <see cref="NpgsqlMaptorGeometryTypeMapping"/> to the Npgsql provider's type-mapping source. It claims
/// the CLR type <see cref="Geometry{Point}"/> (an unspecified column defaults to <c>geography</c>, as in the SQL Server
/// plugin) and the <c>geometry</c>/<c>geography</c> store types when no CLR type is known (e.g. raw SQL projections).
/// </summary>
public class NpgsqlMaptorGeometryTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    private const string DefaultStoreType = "geography";

    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        if (storeTypeName is not null && !NpgsqlMaptorGeometryTypeMapping.IsSpatialStoreType(storeTypeName))
            return null;

        if (clrType == typeof(Geometry<Point>))
            return new NpgsqlMaptorGeometryTypeMapping(storeTypeName ?? DefaultStoreType);

        if (clrType is null && storeTypeName is not null)
            return new NpgsqlMaptorGeometryTypeMapping(storeTypeName);

        return null;
    }
}

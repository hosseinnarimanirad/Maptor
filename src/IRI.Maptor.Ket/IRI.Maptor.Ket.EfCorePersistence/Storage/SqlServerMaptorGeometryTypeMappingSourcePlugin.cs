using Microsoft.EntityFrameworkCore.Storage;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.EfCorePersistence.Storage;

/// <summary>
/// Supplies the <see cref="SqlServerMaptorGeometryTypeMapping"/> to the SQL Server provider's type-mapping source.
/// Claims the CLR type <see cref="Geometry{Point}"/> (defaulting an unspecified column to <c>geography</c>), and the
/// <c>geometry</c>/<c>geography</c> store types when no CLR type is known (e.g. raw SQL projections).
/// </summary>
public class SqlServerMaptorGeometryTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    private const string DefaultStoreType = "geography";

    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        if (clrType == typeof(Geometry<Point>))
            return new SqlServerMaptorGeometryTypeMapping(storeTypeName ?? DefaultStoreType);

        if (clrType is null && storeTypeName is not null && IsSpatialStoreType(storeTypeName))
            return new SqlServerMaptorGeometryTypeMapping(storeTypeName);

        return null;
    }

    private static bool IsSpatialStoreType(string storeTypeName)
        => storeTypeName.Equals("geography", StringComparison.OrdinalIgnoreCase)
            || storeTypeName.Equals("geometry", StringComparison.OrdinalIgnoreCase);
}

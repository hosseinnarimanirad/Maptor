using System.Data.SqlTypes;

using Microsoft.EntityFrameworkCore.Storage;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Infrastructure.EfCore.Storage;

/// <summary>
/// Supplies the <see cref="SqlServerMaptorGeometryTypeMapping"/> to the SQL Server provider's type-mapping source.
/// Claims the CLR type <see cref="Geometry{Point}"/> (defaulting an unspecified column to <c>geography</c>) and
/// the <c>geometry</c>/<c>geography</c> store types when no CLR type is known (e.g. raw SQL projections).
/// Additionally claims, via <see cref="SqlServerSpatialPassThroughTypeMapping"/>, spatial columns whose CLR type
/// comes from a compiled migration snapshot rather than an entity: <see cref="SqlBytes"/> (the provider type that
/// scaffolded snapshots record) and NetTopologySuite's <c>Geometry</c> (snapshots predating the switch from
/// <c>UseNetTopologySuite()</c>, matched by type name so no NetTopologySuite reference is needed) — without these
/// the migrations differ has no type mapping for the column and `dotnet ef migrations add`/`remove` fails with a
/// NullReferenceException in <c>ColumnBase.ProviderValueComparer</c>.
/// </summary>
public class SqlServerMaptorGeometryTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    private const string DefaultStoreType = "geography";

    private const string NetTopologySuiteGeometryTypeName = "NetTopologySuite.Geometries.Geometry";

    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        if (clrType == typeof(Geometry<Point>))
            return new SqlServerMaptorGeometryTypeMapping(storeTypeName ?? DefaultStoreType);

        if (storeTypeName is not null && IsSpatialStoreType(storeTypeName))
        {
            if (clrType is null)
                return new SqlServerMaptorGeometryTypeMapping(storeTypeName);

            if (clrType == typeof(SqlBytes) || clrType.FullName == NetTopologySuiteGeometryTypeName)
                return new SqlServerSpatialPassThroughTypeMapping(clrType, storeTypeName);
        }

        return null;
    }

    private static bool IsSpatialStoreType(string storeTypeName)
        => storeTypeName.Equals("geography", StringComparison.OrdinalIgnoreCase)
            || storeTypeName.Equals("geometry", StringComparison.OrdinalIgnoreCase);
}

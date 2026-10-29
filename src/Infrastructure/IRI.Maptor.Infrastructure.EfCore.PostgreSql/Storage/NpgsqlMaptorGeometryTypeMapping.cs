using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.TypeHandling;
using IRI.Maptor.Infrastructure.EfCore.ValueConversion;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Storage;

/// <summary>
/// Maps <see cref="Geometry{Point}"/> to a PostGIS <c>geometry</c> or <c>geography</c> column (with or without a
/// type modifier, e.g. <c>geometry(Point,4326)</c>).
/// </summary>
/// <remarks>
/// No value converter is involved. The <c>Geometry&lt;Point&gt;</c> value goes to Npgsql unchanged, and the Npgsql type
/// plugin registered through the data source (<see cref="Extensions.NpgsqlMaptorGeometryExtensions.UseMaptor"/>)
/// encodes it as EWKB. This mapping only sets the parameter's data type name. LINQ spatial method translation is
/// intentionally not provided; do server-side spatial work with raw SQL (<c>FromSql</c>).
/// </remarks>
public class NpgsqlMaptorGeometryTypeMapping : RelationalTypeMapping
{
    public NpgsqlMaptorGeometryTypeMapping(string storeType)
        : base(
            new RelationalTypeMappingParameters(
                // The provider value is the Geometry<Point> itself, so it needs the structural comparer as well:
                // the default provider comparer is reference equality, which makes the migrations differ treat
                // HasData geometries as changed on every build.
                new CoreTypeMappingParameters(
                    typeof(Geometry<Point>),
                    converter: null,
                    comparer: new MaptorGeometryValueComparer(),
                    keyComparer: new MaptorGeometryValueComparer(),
                    providerValueComparer: new MaptorGeometryValueComparer()),
                storeType))
    {
    }

    protected NpgsqlMaptorGeometryTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>The PostgreSQL data type name ("geography" or "geometry") without any type modifier.</summary>
    private string DataTypeName => IsGeographyStoreType(StoreType)
        ? MaptorGeometryTypeInfoResolverFactory.GeographyDataTypeName
        : MaptorGeometryTypeInfoResolverFactory.GeometryDataTypeName;

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new NpgsqlMaptorGeometryTypeMapping(parameters);

    protected override void ConfigureParameter(DbParameter parameter)
    {
        if (parameter is not NpgsqlParameter npgsqlParameter)
            throw new InvalidOperationException(
                $"PostGIS spatial columns require an {nameof(NpgsqlParameter)}; received {parameter.GetType().Name}.");

        npgsqlParameter.DataTypeName = DataTypeName;
    }

    protected override string GenerateNonNullSqlLiteral(object value)
    {
        if (value is not Geometry<Point> geometry)
            throw new InvalidOperationException(
                $"Unexpected value of type {value.GetType().Name} for a PostGIS spatial column.");

        // PostGIS text input accepts hex-encoded EWKB, so the literal keeps the SRID and exact coordinates.
        return $"'{Convert.ToHexString(PostGisGeometrySerializer.Serialize(geometry))}'::{DataTypeName}";
    }

    /// <summary>
    /// C# code for a value in scaffolded migrations and snapshots (<c>HasData</c> seed data):
    /// <c>Geometry&lt;Point&gt;.FromWkt("...", srid)</c>. The WKT writer uses .NET's round-trippable number
    /// formatting, so the seeded coordinates are exact.
    /// </summary>
    public override Expression GenerateCodeLiteral(object value)
    {
        if (value is not Geometry<Point> geometry)
            throw new InvalidOperationException(
                $"Unexpected value of type {value.GetType().Name} for a PostGIS spatial column.");

        return Expression.Call(
            _fromWktMethod,
            Expression.Constant(geometry.AsWkt()),
            Expression.Constant(geometry.Srid));
    }

    private static readonly MethodInfo _fromWktMethod =
        typeof(Geometry<Point>).GetMethod(nameof(Geometry<Point>.FromWkt), new[] { typeof(string), typeof(int) })!;

    internal static bool IsGeographyStoreType(string storeType)
        => storeType.StartsWith(MaptorGeometryTypeInfoResolverFactory.GeographyDataTypeName, StringComparison.OrdinalIgnoreCase);

    internal static bool IsSpatialStoreType(string storeType)
        => IsGeographyStoreType(storeType)
            || storeType.StartsWith(MaptorGeometryTypeInfoResolverFactory.GeometryDataTypeName, StringComparison.OrdinalIgnoreCase);
}

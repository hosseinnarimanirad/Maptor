using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Reflection;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace IRI.Maptor.Ket.EfCorePersistence.Storage;

/// <summary>
/// Maps a SQL Server <c>geometry</c>/<c>geography</c> column to the given CLR type as-is, with no conversion.
/// Exists for models built from compiled migration snapshots and <c>.Designer.cs</c> files rather than from the
/// application's entity types: snapshots scaffolded against <see cref="SqlServerMaptorGeometryTypeMapping"/>
/// record spatial columns with the provider type (<see cref="SqlBytes"/>), and snapshots that predate the switch
/// from <c>UseNetTopologySuite()</c> record them as NetTopologySuite's <c>Geometry</c>. Either way the migrations
/// differ dereferences the column's type mapping (a missing one surfaces as a NullReferenceException in
/// <c>ColumnBase.ProviderValueComparer</c>), while nothing is ever materialized or bound at design time — so a
/// pass-through mapping is sufficient and keeps `dotnet ef migrations add`/`remove` working.
/// </summary>
public class SqlServerSpatialPassThroughTypeMapping : RelationalTypeMapping
{
    private static readonly MethodInfo _getSqlBytesMethod =
        typeof(SqlDataReader).GetRuntimeMethod(nameof(SqlDataReader.GetSqlBytes), new[] { typeof(int) })!;

    private readonly bool _isGeography;

    /// <summary>The SQL Server UDT type name used for parameters and literals ("geography" or "geometry").</summary>
    private string UdtTypeName => _isGeography ? "geography" : "geometry";

    public SqlServerSpatialPassThroughTypeMapping(Type clrType, string storeType)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(clrType),
                storeType))
    {
        _isGeography = SqlServerMaptorGeometryTypeMapping.IsGeographyStoreType(storeType);
    }

    protected SqlServerSpatialPassThroughTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
        _isGeography = SqlServerMaptorGeometryTypeMapping.IsGeographyStoreType(parameters.StoreType);
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new SqlServerSpatialPassThroughTypeMapping(parameters);

    /// <summary>Read the column as raw UDT bytes; only meaningful when the CLR type is <see cref="SqlBytes"/>.</summary>
    public override MethodInfo GetDataReaderMethod()
        => _getSqlBytesMethod;

    protected override void ConfigureParameter(DbParameter parameter)
    {
        if (parameter is not SqlParameter sqlParameter)
            throw new InvalidOperationException(
                $"SQL Server spatial columns require a {nameof(SqlParameter)}; received {parameter.GetType().Name}.");

        sqlParameter.SqlDbType = SqlDbType.Udt;
        sqlParameter.UdtTypeName = UdtTypeName;

        if (sqlParameter.Value is null)
            sqlParameter.Value = DBNull.Value;
    }

    protected override string GenerateNonNullSqlLiteral(object value)
    {
        var bytes = value switch
        {
            SqlBytes sqlBytes => sqlBytes.Value,
            byte[] raw => raw,
            _ => throw new InvalidOperationException(
                $"Unexpected provider value of type {value.GetType().Name} for a SQL Server spatial column.")
        };

        // The varbinary -> geometry/geography cast parses the native binary form directly.
        return $"CAST(0x{Convert.ToHexString(bytes)} AS {UdtTypeName})";
    }
}

using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Reflection;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using IRI.Maptor.Infrastructure.EfCore.ValueConversion;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.IO.SqlServerNativeBinary;

namespace IRI.Maptor.Infrastructure.EfCore.Storage;

/// <summary>
/// Maps <see cref="Geometry{Point}"/> to a SQL Server <c>geometry</c> or <c>geography</c> column using the
/// MS-SSCLRT native binary format, with no dependency on NetTopologySuite or Microsoft.SqlServer.Types.
/// </summary>
/// <remarks>
/// The column is read as raw UDT bytes via <see cref="SqlDataReader.GetSqlBytes(int)"/> and decoded with
/// <see cref="SqlServerSpatialNativeBinary.DeserializeGeometryPoint(byte[], bool)"/>; writes bind a
/// <see cref="SqlDbType.Udt"/> parameter carrying the serialized native binary. This mirrors how
/// Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite reads/writes spatial UDTs. LINQ spatial method
/// translation is intentionally not provided — server-side spatial work is expected via raw SQL (<c>FromSql</c>).
/// </remarks>
public class SqlServerMaptorGeometryTypeMapping : RelationalTypeMapping
{
    private static readonly MethodInfo _getSqlBytesMethod =
        typeof(SqlDataReader).GetRuntimeMethod(nameof(SqlDataReader.GetSqlBytes), new[] { typeof(int) })!;

    private readonly bool _isGeography;

    /// <summary>The SQL Server UDT type name used for parameters and literals ("geography" or "geometry").</summary>
    private string UdtTypeName => _isGeography ? "geography" : "geometry";

    public SqlServerMaptorGeometryTypeMapping(string storeType)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(
                    typeof(Geometry<Point>),
                    CreateConverter(IsGeographyStoreType(storeType)),
                    new MaptorGeometryValueComparer()),
                storeType))
    {
        _isGeography = IsGeographyStoreType(storeType);
    }

    protected SqlServerMaptorGeometryTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
        _isGeography = IsGeographyStoreType(parameters.StoreType);
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new SqlServerMaptorGeometryTypeMapping(parameters);

    /// <summary>Read the column as raw UDT bytes (SqlBytes); the value converter decodes them into a geometry.</summary>
    public override MethodInfo GetDataReaderMethod()
        => _getSqlBytesMethod;

    protected override void ConfigureParameter(DbParameter parameter)
    {
        // The value has already been converted to SqlBytes (or DBNull) by the value converter / null handling.
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

    private static ValueConverter<Geometry<Point>, SqlBytes> CreateConverter(bool isGeography)
        => new(
            geometry => new SqlBytes(SqlServerSpatialNativeBinary.Serialize(geometry, isGeography)!),
            bytes => SqlServerSpatialNativeBinary.DeserializeGeometryPoint(bytes.Value, isGeography)!);

    internal static bool IsGeographyStoreType(string storeType)
        => storeType.IndexOf("geograph", StringComparison.OrdinalIgnoreCase) >= 0;
}

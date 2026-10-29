using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Storage;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Storage;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.TypeHandling;

namespace IRI.Maptor.Tests.PostgreSql;

/// <summary>
/// No-database tests for IRI.Maptor.Infrastructure.EfCore.PostgreSql: the EF Core type mapping (SQL and C# code
/// literals, comparers), the type-mapping source plugin and the EWKB serializer. The database round trips are in
/// <see cref="PostGisIntegrationTest"/>.
/// </summary>
public class NpgsqlMaptorGeometryTypeMappingTest
{
    private static RelationalTypeMappingInfo ClrMappingInfo(Type clrType, string? storeType)
        => new(type: clrType, storeTypeName: storeType, storeTypeNameBase: storeType?.Split('(')[0]);

    private static RelationalTypeMappingInfo StoreMappingInfo(string storeType)
        => new(storeTypeName: storeType, storeTypeNameBase: storeType.Split('(')[0], unicode: null, size: null, precision: null, scale: null);

    private static Geometry<Point> SamplePolygon(int srid = 4326)
        => Geometry<Point>.FromWkt("POLYGON ((51 35, 51.1 35, 51.1 35.1, 51 35.1, 51 35))", srid);

    [Theory]
    [InlineData("geography", "::geography")]
    [InlineData("GEOGRAPHY", "::geography")]
    [InlineData("geometry", "::geometry")]
    [InlineData("geometry(Polygon,4326)", "::geometry")]
    [InlineData("geography(Point,4326)", "::geography")]
    public void SqlLiteral_IsHexEwkbCastToBaseType(string storeType, string expectedCast)
    {
        var mapping = new NpgsqlMaptorGeometryTypeMapping(storeType);
        var geometry = SamplePolygon();

        var literal = mapping.GenerateSqlLiteral(geometry);

        Assert.Equal($"'{Convert.ToHexString(geometry.AsEwkb()!)}'{expectedCast}", literal);
    }

    [Fact]
    public void SqlLiteral_Null_IsNull()
        => Assert.Equal("NULL", new NpgsqlMaptorGeometryTypeMapping("geometry").GenerateSqlLiteral(null));

    [Theory]
    [InlineData("POLYGON ((51 35, 51.1 35, 51.1 35.1, 51 35.1, 51 35))", 4326)]
    [InlineData("POINT (0.30000000000000004 1E-300)", 0)]
    [InlineData("LINESTRING (51.123456789012345 35.98765432109876, -179.99999999999997 89.99999999999999)", 4326)]
    [InlineData("MULTIPOINT ((500000 3950000), (500000.5 3950000.25))", 32639)]
    public void CodeLiteral_RebuildsTheExactGeometry(string wkt, int srid)
    {
        var mapping = new NpgsqlMaptorGeometryTypeMapping("geography");
        var geometry = Geometry<Point>.FromWkt(wkt, srid);

        var expression = mapping.GenerateCodeLiteral(geometry);
        var rebuilt = Expression.Lambda<Func<Geometry<Point>>>(expression).Compile()();

        Assert.Equal(srid, rebuilt.Srid);
        Assert.Equal(geometry.AsEwkb(), rebuilt.AsEwkb());
    }

    [Fact]
    public void Comparers_AreStructural_ForValueKeyAndProvider()
    {
        // Regression: with a reference-equality provider comparer, the migrations differ reported HasData
        // geometries as changed on every build (PendingModelChangesWarning).
        var mapping = new NpgsqlMaptorGeometryTypeMapping("geography");
        var left = SamplePolygon();
        var right = SamplePolygon();
        var otherSrid = SamplePolygon(srid: 0);

        foreach (var comparer in new[] { mapping.Comparer, mapping.KeyComparer, mapping.ProviderValueComparer })
        {
            Assert.True(comparer.Equals(left, right));
            Assert.False(comparer.Equals(left, otherSrid));
            Assert.Equal(comparer.GetHashCode(left), comparer.GetHashCode(right));
        }
    }

    [Theory]
    [InlineData(null, "geography")]
    [InlineData("geometry", "geometry")]
    [InlineData("GEOGRAPHY", "GEOGRAPHY")]
    [InlineData("geometry(Point,4326)", "geometry(Point,4326)")]
    public void SourcePlugin_MapsGeometryOfPoint(string? storeType, string expectedStoreType)
    {
        var mapping = new NpgsqlMaptorGeometryTypeMappingSourcePlugin()
            .FindMapping(ClrMappingInfo(typeof(Geometry<Point>), storeType));

        Assert.IsType<NpgsqlMaptorGeometryTypeMapping>(mapping);
        Assert.Equal(expectedStoreType, mapping!.StoreType);
    }

    [Fact]
    public void SourcePlugin_ClaimsSpatialStoreTypeWithoutClrType()
    {
        var mapping = new NpgsqlMaptorGeometryTypeMappingSourcePlugin()
            .FindMapping(StoreMappingInfo("geography(Point,4326)"));

        Assert.IsType<NpgsqlMaptorGeometryTypeMapping>(mapping);
    }

    [Fact]
    public void SourcePlugin_IgnoresOtherTypes()
    {
        var plugin = new NpgsqlMaptorGeometryTypeMappingSourcePlugin();

        Assert.Null(plugin.FindMapping(ClrMappingInfo(typeof(string), "text")));
        Assert.Null(plugin.FindMapping(ClrMappingInfo(typeof(Geometry<Point>), "text")));
        Assert.Null(plugin.FindMapping(StoreMappingInfo("integer")));
    }

    public static IEnumerable<object[]> ZmEwkbData =>
    [
        // PostGIS ST_AsEWKB(..., 'NDR'), with the 2D WKT expected after Z/M are dropped
        [ "01010000a07f7f00000000000080841e4100000000d8224e410000000000c29240", "POINT (500000 3950000)", 32639 ],
        [ "0102000060e610000002000000000000000000000000000000000000000000000000002440000000000000f03f000000000000f03f0000000000003440", "LINESTRING (0 0, 1 1)", 4326 ],
        [ "01030000e0110f0000010000000500000000000000000000000000000000000000000000000000244000000000000059400000000000002440000000000000000000000000000024400000000000005940000000000000244000000000000024400000000000002440000000000000594000000000000000000000000000002440000000000000244000000000000059400000000000000000000000000000000000000000000024400000000000005940",
          "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))", 3857 ],
        [ "01050000a0e61000000200000001020000800200000000000000000000000000000000000000000000000000f03f000000000000f03f000000000000f03f0000000000000040010200008002000000000000000000004000000000000000400000000000000840000000000000084000000000000008400000000000001440",
          "MULTILINESTRING ((0 0, 1 1), (2 2, 3 3))", 4326 ],
    ];

    [Theory]
    [MemberData(nameof(ZmEwkbData))]
    public void Serializer_DropsZAndM_KeepsSrid(string ewkbHex, string expectedWkt, int expectedSrid)
    {
        var geometry = PostGisGeometrySerializer.Deserialize(Convert.FromHexString(ewkbHex));

        Assert.Equal(expectedWkt, geometry.AsWkt());
        Assert.Equal(expectedSrid, geometry.Srid);
        Assert.False(geometry.HasZ());
        Assert.False(geometry.HasM());
    }

    [Fact]
    public void Serializer_RoundTripsThroughEwkb()
    {
        var geometry = Geometry<Point>.Create(
            new List<Geometry<Point>> { SamplePolygon(), Geometry<Point>.FromWkt("POLYGON ((52 36, 52.2 36, 52.2 36.2, 52 36))", 4326) },
            GeometryType.MultiPolygon, 4326);

        var restored = PostGisGeometrySerializer.Deserialize(PostGisGeometrySerializer.Serialize(geometry));

        Assert.Equal(geometry.AsWkt(), restored.AsWkt());
        Assert.Equal(4326, restored.Srid);
        Assert.Equal(2, restored.Geometries!.Count());
    }
}

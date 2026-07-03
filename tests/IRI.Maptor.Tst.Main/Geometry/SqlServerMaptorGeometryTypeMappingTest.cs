using System.Collections.Generic;
using System.Data.SqlTypes;

using IRI.Maptor.Ket.EfCorePersistence.Storage;
using IRI.Maptor.Ket.EfCorePersistence.ValueConversion;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Tst.Main.TheGeometry;

/// <summary>
/// No-database tests for the EF Core type mapping in IRI.Maptor.Ket.EfCorePersistence: the value converter
/// (Geometry&lt;Point&gt; &lt;-&gt; SqlBytes native binary), the SQL literal form, and the change-tracking comparer.
/// The reader/parameter (DB) path requires an integration/smoke test against SQL Server.
/// </summary>
public class SqlServerMaptorGeometryTypeMappingTest
{
    private static Geometry<Point> SamplePolygon()
        => Geometry<Point>.Create(
            new List<Geometry<Point>>
            {
                Geometry<Point>.CreatePolygonRing(
                    new List<Point> { new(0, 0), new(30, 0), new(30, 30), new(0, 30) }, 4326)
            },
            GeometryType.Polygon, 4326);

    [Theory]
    [InlineData("geography")]
    [InlineData("geometry")]
    [InlineData("GEOGRAPHY")]
    public void Converter_RoundTripsGeometryThroughSqlBytes(string storeType)
    {
        var mapping = new SqlServerMaptorGeometryTypeMapping(storeType);
        var geometry = SamplePolygon();

        var provider = mapping.Converter!.ConvertToProvider(geometry);
        Assert.IsType<SqlBytes>(provider);

        var restored = (Geometry<Point>)mapping.Converter.ConvertFromProvider(provider)!;

        Assert.Equal(geometry.AsWkt(), restored.AsWkt());
        Assert.Equal(geometry.Srid, restored.Srid);
    }

    [Fact]
    public void GenerateSqlLiteral_EmitsNativeBinaryCast()
    {
        var mapping = new SqlServerMaptorGeometryTypeMapping("geography");
        var point = Geometry<Point>.Create(new List<Point> { new(51.4, 35.7) }, GeometryType.Point, 4326);

        var literal = mapping.GenerateSqlLiteral(point);

        Assert.StartsWith("CAST(0x", literal);
        Assert.EndsWith(" AS geography)", literal);
    }

    [Fact]
    public void GenerateSqlLiteral_UsesGeometryUdtForGeometryColumns()
    {
        var mapping = new SqlServerMaptorGeometryTypeMapping("geometry");
        var point = Geometry<Point>.Create(new List<Point> { new(1, 2) }, GeometryType.Point, 0);

        Assert.EndsWith(" AS geometry)", mapping.GenerateSqlLiteral(point));
    }

    [Fact]
    public void ValueComparer_UsesSridAndWkb()
    {
        var comparer = new MaptorGeometryValueComparer();
        var a = Geometry<Point>.Create(new List<Point> { new(1, 2) }, GeometryType.Point, 4326);
        var b = Geometry<Point>.Create(new List<Point> { new(1, 2) }, GeometryType.Point, 4326);
        var differentPoint = Geometry<Point>.Create(new List<Point> { new(9, 9) }, GeometryType.Point, 4326);
        var differentSrid = Geometry<Point>.Create(new List<Point> { new(1, 2) }, GeometryType.Point, 3857);

        Assert.True(comparer.Equals(a, b));
        Assert.False(comparer.Equals(a, differentPoint));
        Assert.False(comparer.Equals(a, differentSrid));

        var snapshot = comparer.Snapshot(a);
        Assert.NotSame(a, snapshot);
        Assert.True(comparer.Equals(a, snapshot));
    }
}

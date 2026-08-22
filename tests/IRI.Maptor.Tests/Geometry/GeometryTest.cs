using IRI.Maptor.Extensions;
using IRI.Maptor.Tests.Assets;
using IRI.Maptor.Core.Spatial.IO.OgcSFA;
using IRI.Maptor.Core.Spatial.Primitives;
using Xunit;

namespace IRI.Maptor.Tests.TheGeometry;


public class GeometryTest
{ 
    [Fact]
    public void TestAreaCalculation()
    {
        foreach (var item in GeometrySamples.AllGeometries)
        {
            var area1 = item.EuclideanArea;
            var area2 = item.AsSqlGeometry().STArea().Value;

            Assert.Equal(area1, area2);
        }
    }

    [Fact]
    public void TestLengthCalculation()
    {
        foreach (var item in GeometrySamples.AllGeometries)
        {
            var length1 = item.GetEuclideanLength();
            var length2 = item.AsSqlGeometry().STLength().Value;

            Assert.Equal(length1, length2);
        }
    }

    [Fact]
    public void TestMeanAngularChange()
    {
        var meanAngularChange = SqlGeometrySamples.LineString_ForAngularChange.AsGeometry().CalculateMeanAngularChange() * 180 / Math.PI;

        Assert.Equal(72.1864, meanAngularChange, 4 /*0.0001*/);


        var meanAngularChangeForPolygon = SqlGeometrySamples.Polygon_ForAngularChange.AsGeometry().CalculateMeanAngularChange() * 180 / Math.PI;

        Assert.Equal(75.687, meanAngularChangeForPolygon, 4 /*0.0001*/);
    }

    // 1401.03.12
    [Fact]
    public void TestTotalVectorDispalcement()
    {
        var original = SqlGeometrySamples.LineString_ForVectorDisplacement_Original.AsGeometry();
        var simplified = SqlGeometrySamples.LineString_ForVectorDisplacement_Simplified.AsGeometry();

        var dispacement = original.CalculateTotalVectorDisplacement(simplified);

        Assert.Equal(6.324, dispacement, 3 /*0.001*/);

    }

    [Theory]
    [InlineData("POINT (1 2)", 0)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 0)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 1)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 2)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 3)]
    [InlineData("MULTIPOINT ((2 3), (7 8))", 0)]
    [InlineData("MULTIPOINT ((2 3), (7 8))", 1)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 0)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 1)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 2)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 3)]
    [InlineData("LINESTRING (4 4, 9 0)", 0)]
    [InlineData("LINESTRING (4 4, 9 0)", 1)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 0)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 1)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 2)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 3)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 0)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 1)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 2)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 3)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 0)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 3)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 4)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 6)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 0)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 3)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 4)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 6)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 7)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 9)]
    public void TestPointAddressRoundTrip(string wkt, int globalIndex)
    {
        // Arrange
        var wktNormalized = wkt.Replace(", ", ",");
        var geometry = SqlServerWktReader.Parse(wktNormalized, 0);

        // Act: FindPointAddress -> ToGlobalPointIndex round trip
        var address = geometry.FindPointAddress(globalIndex);
        var roundTripGlobalIndex = geometry.ToGlobalPointIndex(address);

        // Assert: Round trip should return the same global index
        Assert.Equal(globalIndex, roundTripGlobalIndex);
    }

    [Theory]
    [InlineData("POINT (1 2)", 0)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 0)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 1)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 2)]
    [InlineData("MULTIPOINT ((0 0), (0 3), (3 3), (3 0))", 3)]
    [InlineData("MULTIPOINT ((2 3), (7 8))", 0)]
    [InlineData("MULTIPOINT ((2 3), (7 8))", 1)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 0)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 1)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 2)]
    [InlineData("LINESTRING (1 1, 2 0, 2 4, 3 3)", 3)]
    [InlineData("LINESTRING (4 4, 9 0)", 0)]
    [InlineData("LINESTRING (4 4, 9 0)", 1)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 0)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 1)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 2)]
    [InlineData("MULTILINESTRING ((1 1, 3 5), (-5 3, -8 -2))", 3)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 0)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 1)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 2)]
    [InlineData("POLYGON ((0 0, 30 0, 30 30, 0 30, 0 0))", 3)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 0)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 3)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 4)]
    [InlineData("POLYGON ((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0))", 6)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 0)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 3)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 4)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 6)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 7)]
    [InlineData("MULTIPOLYGON (((0 0, 0 3, 3 3, 3 0, 0 0), (2 1, 1 2, 1 1, 2 1)), ((9 9, 9 10, 10 9, 9 9)))", 9)]
    public void TestPointAddressReverseRoundTrip(string wkt, int globalIndex)
    {
        // Arrange
        var wktNormalized = wkt.Replace(", ", ",");
        var geometry = SqlServerWktReader.Parse(wktNormalized, 0);

        // Act: ToGlobalPointIndex -> FindPointAddress round trip
        // First, we need to construct a valid address from the global index
        var address = geometry.FindPointAddress(globalIndex);
        
        // Verify the address is valid (not -1, -1)
        Assert.True(address.PartIndex >= 0, $"Invalid address returned for globalIndex {globalIndex}");
        Assert.True(address.LocalPointIndex >= 0, $"Invalid address returned for globalIndex {globalIndex}");

        // Now test the reverse: address -> global index -> address
        var calculatedGlobalIndex = geometry.ToGlobalPointIndex(address);
        var roundTripAddress = geometry.FindPointAddress(calculatedGlobalIndex);

        // Assert: Round trip should return the same address
        Assert.Equal(address.PolygonIndex, roundTripAddress.PolygonIndex);
        Assert.Equal(address.PartIndex, roundTripAddress.PartIndex);
        Assert.Equal(address.LocalPointIndex, roundTripAddress.LocalPointIndex);
    }
     
}

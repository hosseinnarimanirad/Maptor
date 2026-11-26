using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

public class LocalMetricFrameTest
{
    [Fact]
    public void Projected_OnlyMovesTheOrigin()
    {
        var frame = LocalMetricFrame.At(new Point(1000, 2000), isGeodetic: false);

        Assert.Equal((250.0, -40.0), frame.ToMetres(new Point(1250, 1960)));
        Assert.Equal(1, frame.MetresPerUnitX);
        Assert.Equal(1, frame.MetresPerUnitY);
    }

    [Fact]
    public void Projected_SearchHalfSize_IsTheDistance()
    {
        var frame = LocalMetricFrame.At(0, 0, isGeodetic: false);

        Assert.Equal((0.5, 0.5), frame.SearchHalfSize(0.5));
    }

    [Theory]
    [InlineData(51.4, 35.7)]   // a mid-latitude place
    [InlineData(60.0, 25.0)]
    [InlineData(45.0, 39.5)]
    public void Geodetic_ShortDistances_MatchTheEllipsoid(double longitude, double latitude)
    {
        var origin = new Point(longitude, latitude);
        var frame = LocalMetricFrame.At(origin, isGeodetic: true);

        // About 150 m east and 150 m north.
        var target = new Point(longitude + 150 / frame.MetresPerUnitX, latitude + 150 / frame.MetresPerUnitY);

        var (east, north) = frame.ToMetres(target);
        var plane = Math.Sqrt(east * east + north * north);

        var ellipsoid = SpatialUtility.GetEllipsoidalLength(origin, target);

        Assert.True(Math.Abs(plane - ellipsoid) < 0.01, $"plane {plane:0.0000} m, ellipsoid {ellipsoid:0.0000} m");
    }

    [Theory]
    [InlineData(35.7)]
    [InlineData(25.0)]
    [InlineData(0.0)]
    public void Geodetic_SearchBox_HoldsEverythingAtTheDistance(double latitude)
    {
        const double metres = 50;

        var origin = new Point(51.0, latitude);
        var frame = LocalMetricFrame.At(origin, isGeodetic: true);
        var (halfWidth, halfHeight) = frame.SearchHalfSize(metres);

        var east = new Point(origin.X + halfWidth, origin.Y);
        var north = new Point(origin.X, origin.Y + halfHeight);

        Assert.True(SpatialUtility.GetEllipsoidalLength(origin, east) >= metres);
        Assert.True(SpatialUtility.GetEllipsoidalLength(origin, north) >= metres);
    }

    [Fact]
    public void Geodetic_DegreeOfLongitude_ShrinksWithLatitude()
    {
        var equator = LocalMetricFrame.At(51, 0, isGeodetic: true);
        var iran = LocalMetricFrame.At(51, 35.7, isGeodetic: true);

        Assert.True(iran.MetresPerUnitX < equator.MetresPerUnitX);
        Assert.InRange(iran.MetresPerUnitX / equator.MetresPerUnitX, 0.80, 0.82);
    }
}

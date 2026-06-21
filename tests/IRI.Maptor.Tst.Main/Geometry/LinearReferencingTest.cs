using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis.Network;
using IRI.Maptor.Sta.SpatialReferenceSystem;

using Xunit;

namespace IRI.Maptor.Tst.Main.TheGeometry;

public class LinearReferencingTest
{
    [Fact]
    public void StraightLine_MidPoint_Projected()
    {
        var line = Geometry<Point>.FromWkt("LINESTRING(0 0, 100 0)", 0);

        var p = line.GetPointAtDistance(25);

        Assert.Equal(25.0, p.X, 6);
        Assert.Equal(0.0, p.Y, 6);
    }

    [Fact]
    public void LShaped_AtVertexAndIntoSecondSegment_Projected()
    {
        // Horizontal segment length 10, then vertical segment length 10.
        var line = Geometry<Point>.FromWkt("LINESTRING(0 0, 10 0, 10 10)", 0);

        var atVertex = line.GetPointAtDistance(10);
        Assert.Equal(10.0, atVertex.X, 6);
        Assert.Equal(0.0, atVertex.Y, 6);

        var p = line.GetPointAtDistance(15);
        Assert.Equal(10.0, p.X, 6);
        Assert.Equal(5.0, p.Y, 6);
    }

    [Fact]
    public void DistanceZeroOrNegative_ReturnsStart()
    {
        var line = Geometry<Point>.FromWkt("LINESTRING(2 3, 100 3)", 0);

        Assert.Equal(2.0, line.GetPointAtDistance(0).X, 6);
        Assert.Equal(2.0, line.GetPointAtDistance(-5).X, 6);
    }

    [Fact]
    public void DistanceBeyondLength_ClampsToEnd()
    {
        var line = Geometry<Point>.FromWkt("LINESTRING(0 0, 10 0)", 0);

        var p = line.GetPointAtDistance(999);

        Assert.Equal(10.0, p.X, 6);
        Assert.Equal(0.0, p.Y, 6);
    }

    [Fact]
    public void FromEnd_MeasuresFromLastVertex()
    {
        var line = Geometry<Point>.FromWkt("LINESTRING(0 0, 100 0)", 0);

        var p = line.GetPointAtDistance(25, fromEnd: true);

        Assert.Equal(75.0, p.X, 6);
        Assert.Equal(0.0, p.Y, 6);
    }

    [Fact]
    public void PointGeometry_Throws()
    {
        var point = Geometry<Point>.FromWkt("POINT(0 0)", 0);

        Assert.Throws<System.ArgumentException>(() => point.GetPointAtDistance(5));
    }

    [Fact]
    public void Geodetic_ResultLiesRequestedDistanceAlongMeridian()
    {
        // Prime meridian, equator northward, WGS84.
        var line = Geometry<Point>.FromWkt("LINESTRING(0 0, 0 1)", SridHelper.GeodeticWGS84);

        double requested = 50000; // 50 km

        var p = line.GetPointAtDistance(requested);

        // Stays on the meridian, advances under one degree of latitude.
        Assert.Equal(0.0, p.X, 6);
        Assert.InRange(p.Y, 0.4, 0.5);

        // Geodesic distance from the start equals what was requested (to ~1 m).
        double actual = SpatialUtility.GetEllipsoidalLength(new Point(0, 0), p);
        Assert.Equal(requested, actual, 0);
    }

    [Fact]
    public void FromNode_PicksDirectionByEndpoint()
    {
        var lines = new List<Geometry<Point>>
        {
            Geometry<Point>.FromWkt("LINESTRING(0 0, 100 0)", 0),
        };

        var network = new LineNetworkBuilder<Point>(1e-6).Build(lines);
        var edge = network.Edges[0];

        var fromStart = edge.GetPointAtDistanceFromNode(edge.StartNodeId, 30);
        Assert.Equal(30.0, fromStart.X, 6);

        var fromEnd = edge.GetPointAtDistanceFromNode(edge.EndNodeId, 30);
        Assert.Equal(70.0, fromEnd.X, 6);
    }
}

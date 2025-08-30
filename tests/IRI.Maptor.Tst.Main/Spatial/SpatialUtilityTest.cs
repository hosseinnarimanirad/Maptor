using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Tst.NetFrameworkTest.Assets;
using System.Collections.Generic;
using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Extensions;
using static IRI.Maptor.Sta.Common.Enums.SpatialRelation;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Tst.NetFrameworkTest.Spatial;


public class SpatialUtilityTest
{
    public SpatialUtilityTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }

    [Theory]
    [MemberData(nameof(TestClockwiseData))]
    public void TestClockwise(bool isClockwise, List<Point> points)
    {
        // Arange

        // Act
        var actualValue = SpatialUtility.IsClockwise(points);

        // Assert
        Assert.Equal(isClockwise, actualValue);
    }

    public static List<object[]> TestClockwiseData()
    {
        //
        var clockWise1 = Geometry<Point>.FromWkt("LINESTRING(00 00, 10 10, 30 00)", 0).Points;
        var clockWise2 = Geometry<Point>.FromWkt("LINESTRING(30 00, 10 10, 30 30)", 0).Points;
        var clockWise3 = Geometry<Point>.FromWkt("LINESTRING(30 30, 10 10, 00 30)", 0).Points;
        var clockWise4 = Geometry<Point>.FromWkt("LINESTRING(00 30, 10 10, 00 00)", 0).Points;

        var counterClockWise1 = Geometry<Point>.FromWkt("LINESTRING(30 00, 10 10, 00 00)", 0).Points;
        var counterClockWise2 = Geometry<Point>.FromWkt("LINESTRING(00 00, 10 10, 00 30)", 0).Points;
        var counterClockWise3 = Geometry<Point>.FromWkt("LINESTRING(00 30, 10 10, 30 30)", 0).Points;
        var counterClockWise4 = Geometry<Point>.FromWkt("LINESTRING(30 30, 10 10, 30 00)", 0).Points;


        return new List<object[]>()
        {
            new object[]{ false, new List<Point>(){ new Point(5, 0), new Point(6, 4), new Point(4, 5), new Point(1, 5), new Point(1, 0) } },
            new object[]{ false, new List<Point>(){ new Point(5, 0), new Point(6, 4), new Point(4, 1), new Point(1, 5), new Point(1, 0) } },
            new object[]{ true,  new List<Point>(){ new Point(1, 0), new Point(1, 5), new Point(4, 1), new Point(6, 4), new Point(5, 0) } },

            new object[]{ true, clockWise1},
            new object[]{ true, clockWise2},
            new object[]{ true, clockWise3},
            new object[]{ true, clockWise4},

            new object[]{ false, counterClockWise1},
            new object[]{ false, counterClockWise2},
            new object[]{ false, counterClockWise3},
            new object[]{ false, counterClockWise4},

        };
    }

    [Fact]
    public void TestCircleRectangleIntersects()
    {
        //   -------
        //   |     |
        //   |     |
        //   |     |
        //   -------
        BoundingBox rectangle = new BoundingBox(0, 0, 10, 20);

        var radius = 5;

        Point insideRectangle = new Point(5, 9);
        Assert.Equal(true, SpatialUtility.CircleRectangleIntersects(insideRectangle, radius, rectangle));


        Point topOfRectangle = new Point(5, 25.1);
        Assert.Equal(false, SpatialUtility.CircleRectangleIntersects(topOfRectangle, radius, rectangle));


        Point bottomOfRectangle = new Point(5, -6);
        Assert.Equal(false, SpatialUtility.CircleRectangleIntersects(bottomOfRectangle, radius, rectangle));


        Point topRightCorner = new Point(13, 24);
        Assert.Equal(true, SpatialUtility.CircleRectangleIntersects(topRightCorner, radius, rectangle));

        Point topRightCorner2 = new Point(13.1, 24);
        Assert.Equal(false, SpatialUtility.CircleRectangleIntersects(topRightCorner2, radius, rectangle));


        Point centerRectangle = new Point(5, 10);
        Assert.Equal(false, SpatialUtility.IsAxisAlignedRectangleInsideCircle(centerRectangle, radius, rectangle));

        Point insideRectangle2 = new Point(5.1, 10);
        Assert.Equal(false, SpatialUtility.IsAxisAlignedRectangleInsideCircle(insideRectangle2, radius, rectangle));

        Point insideRectangle3 = new Point(5.1, 10);
        Assert.Equal(false, SpatialUtility.IsAxisAlignedRectangleInsideCircle(insideRectangle3, 11.19, rectangle));

        Point insideRectangle4 = new Point(5, 10);
        Assert.Equal(true, SpatialUtility.IsAxisAlignedRectangleInsideCircle(insideRectangle4, 11.19, rectangle));

        Assert.Equal(Intersects, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(insideRectangle, radius, rectangle));
        Assert.Equal(Disjoint, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(topOfRectangle, radius, rectangle));
        Assert.Equal(Disjoint, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(bottomOfRectangle, radius, rectangle));
        Assert.Equal(Intersects, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(topRightCorner, radius, rectangle));
        Assert.Equal(Disjoint, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(topRightCorner2, radius, rectangle));
        Assert.Equal(Intersects, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(centerRectangle, radius, rectangle));
        Assert.Equal(Intersects, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(insideRectangle2, radius, rectangle));
        Assert.Equal(Intersects, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(insideRectangle3, 11.19, rectangle));
        Assert.Equal(Contained, SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(insideRectangle4, 11.19, rectangle));

    }

    #region Area Methods

    [Fact]
    public void CalculateSignedTriangleAreaTest()
    {
        Assert.Equal(-0.5, SpatialUtility.GetSignedTriangleArea(new Point(0, 0), Point.Create(0, 1), Point.Create(1, 0)));
        Assert.Equal(0.5, SpatialUtility.GetSignedTriangleArea(new Point(0, 0), Point.Create(1, 0), Point.Create(0, 1)));
    }

    [Fact]
    public void CalculateSignedRingAreaTest()
    {
        Assert.Equal(-4, SpatialUtility.GetSignedRingArea(new List<Point>()
                {
            Point.Create(-1,-1),
            Point.Create(-1, 1),
            Point.Create(1, 1),
            Point.Create(1, -1)
                }));

        Assert.Equal(4, SpatialUtility.GetSignedRingArea(new List<Point>()
                {
            Point.Create(-1,-1),
            Point.Create(1, -1),
            Point.Create(1, 1),
            Point.Create(-1, 1)
                }));
    }

    #endregion


    #region Angle Methods

    [Fact]
    public void GetCosineOfAngleTest()
    {
        Assert.Equal(0.96, SpatialUtility.GetCosineOfOuterAngle(Point.Create(0, 0), Point.Create(3, 4), Point.Create(7, 7)));

        // +x axis
        Assert.Equal(1, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(1, 0)), 4  /*0.0005*/);

        // first quarter
        Assert.Equal(0.89442, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(2, 1)), 4);

        // +y axis
        Assert.Equal(0, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(0, 1)), 4  /*0.0005*/);

        // second quarter
        Assert.Equal(-0.7071, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(-1, 1)), 4  /*0.0005*/);

        Assert.Equal(-0.8944, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(-2, 1)), 4  /*0.0005*/);

        // -x axis
        Assert.Equal(-1, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(-2, 0)), 4  /*0.0005*/);

        // third quarter
        Assert.Equal(-0.8944, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(-2, -1)), 4 /*0.0005*/);

        Assert.Equal(-0.5547, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(-2, -3)), 4 /*0.0005*/);

        // -y axis
        Assert.Equal(0, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(0, -3)), 4 /*0.0005*/);

        // fourth quarter
        Assert.Equal(0.5547, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(2, -3)), 4 /*0.0005*/);

        Assert.Equal(0.8944, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(2, -1)), 4 /*0.0005*/);

        Assert.Equal(-1, SpatialUtility.GetCosineOfOuterAngle(Point.Create(-1, 0), Point.Create(0, 0), Point.Create(-1, 0)), 4 /*0.0005*/);
    }

    [Fact]
    public void TestGetAngle()
    {
        Assert.Equal(90, SpatialUtility.GetOuterAngle(new Point(0, 10), new Point(0, 0), new Point(10, 0), AngleMode.Degree));

        Assert.Equal(71.5650, SpatialUtility.GetOuterAngle(new Point(0, 0), new Point(10, 10), new Point(30, 0), AngleMode.Degree), 3 /*0.0005*/);

        Assert.Equal(135, SpatialUtility.GetOuterAngle(new Point(0, 0), new Point(10, 10), new Point(10, 0), AngleMode.Degree), 10);
    }

    #endregion


    [Fact]
    public void TestIsPointInPolygon()
    {

        //MultiPolygon01 : ("MULTIPOLYGON(((0 0, 0 3, 3 3, 3 0, 0 0), (1 1, 1 2, 2 1, 1 1)), ((9 9, 9 10, 10 9, 9 9)))"));

        //MultiPolygon02 : ("MULTIPOLYGON(((0 0, 0 6, 6 6, 6 0, 0 0), (5 5, 5 1, 1 1, 1 5, 5 5)), ((4 4, 2 4, 2 2, 4 2, 4 4),(3.5 3.5, 2.5 3.5, 2.5 2.5, 3.5 2.5, 3.5 3.5)))"));

        //GeometrySamples.MultiPolygon01

        //POLYGON((0 0, 30 0, 30 30, 0 30, 0 0))"));

        Assert.True(TopologyUtility.IsPointInRing(GeometrySamples.Polygon.Geometries[0], new Point(10, 10)));

        Assert.False(TopologyUtility.IsPointInRing(GeometrySamples.Polygon.Geometries[0], new Point(-1, 10)));
    }

    const string _multiPolygon = "MULTIPOLYGON (((8144645 2908406, 9401881 5007061, 6613458 5168496, 8106333 2964956, 8139913 2923903, 8134751 2923010, 8144645 2908406)), ((6681668 2012102, 7984240 2440998, 6486267 2433885, 6681668 2012102)), ((6678491 2008925, 5016918 2383812, 8134751 2923010, 8106333 2964956, 7040670 4267776, 5849293 4048562, 5134467 3241603, 5674558 4077155, 6440216 4315431, 5182122 4671255, 4737342 2218608, 6678491 2008925)))";
    const string _multiPolygon2 = "MULTIPOLYGON (((5983161 3877893, 5983314 3882479, 5978269 3884314, 5976588 3883244, 5983161 3877893)), ((5958549 3845789, 5972460 3853739, 5978728 3846401, 6001659 3898531, 5973836 3928494, 5921094 3920698, 5906418 3908621, 5907794 3890276, 5892201 3874071, 5907947 3870861, 5911005 3848847, 5927821 3856796, 5958549 3845789), (5948917 3862453, 5935617 3876517, 5950905 3875447, 5953351 3884314, 5960842 3879269, 5962982 3887065, 5969097 3882173, 5968638 3889053, 5981938 3889206, 5988053 3878810, 5975212 3863370, 5976282 3875906, 5954115 3868568, 5946166 3869485, 5948917 3862453), (5926904 3903576, 5927209 3914430, 5960230 3916264, 5960995 3905716, 5926904 3903576)))";

    //
    [Theory]
    [InlineData(true, "POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))", "POINT(10 10)")]
    [InlineData(false, "POLYGON((0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9))", "POINT(-1 10)")]

    [InlineData(false, _multiPolygon2, "POINT(5928349 3913730)")]
    [InlineData(false, _multiPolygon2, "POINT(5928808 3904099)")]
    [InlineData(false, _multiPolygon2, "POINT(5959994 3906086)")]
    [InlineData(false, _multiPolygon2, "POINT(5942566 3910367)")]
    [InlineData(false, _multiPolygon2, "POINT(5970542 3887435)")]
    [InlineData(false, _multiPolygon2, "POINT(5975213 3863373)")]
    [InlineData(false, _multiPolygon2, "POINT(5947764 3864199)")]
    [InlineData(false, _multiPolygon2, "POINT(5963357 3885907)")]
    [InlineData(false, _multiPolygon2, "POINT(5987053 3878722)")]
    [InlineData(false, _multiPolygon2, "POINT(5981473 3883690)")]
    [InlineData(false, _multiPolygon2, "POINT(5983766 3880097)")]
    [InlineData(false, _multiPolygon2, "POINT(5976810 3875970)")]
    [InlineData(false, _multiPolygon2, "POINT(5953726 3883002)")]
    [InlineData(false, _multiPolygon2, "POINT(5949904 3874670)")]
    [InlineData(false, _multiPolygon2, "POINT(5963969 3875970)")]
    [InlineData(false, _multiPolygon2, "POINT(5937368 3875817)")]
    [InlineData(false, _multiPolygon2, "POINT(5959994 3911437)")]
    [InlineData(false, _multiPolygon2, "POINT(5987358 3914953)")]
    [InlineData(false, _multiPolygon2, "POINT(5972224 3852733)")]
    [InlineData(false, _multiPolygon2, "POINT(6007996 3867256)")]
    [InlineData(false, _multiPolygon2, "POINT(5928043 3855943)")]
    [InlineData(false, _multiPolygon2, "POINT(5906794 3890799)")]
    [InlineData(false, _multiPolygon2, "POINT(5908322 3864046)")]
    [InlineData(false, _multiPolygon2, "POINT(5891659 3873983)")]

    [InlineData(true, _multiPolygon2, "POINT(5921493 3919903)")]
    [InlineData(true, _multiPolygon2, "POINT(5907187 3898370)")]
    [InlineData(true, _multiPolygon2, "POINT(6001141 3898195)")]
    [InlineData(true, _multiPolygon2, "POINT(5984172 3916693)")]
    [InlineData(true, _multiPolygon2, "POINT(5961240 3911801)")]
    [InlineData(true, _multiPolygon2, "POINT(5926691 3909813)")]
    [InlineData(true, _multiPolygon2, "POINT(5968578 3883519)")]
    [InlineData(true, _multiPolygon2, "POINT(5975763 3874958)")]
    [InlineData(true, _multiPolygon2, "POINT(5946870 3868843)")]
    [InlineData(true, _multiPolygon2, "POINT(5947023 3850192)")]
    [InlineData(true, _multiPolygon2, "POINT(5908346 3871136)")]
    [InlineData(true, _multiPolygon2, "POINT(5983160 3877895)")]
    [InlineData(true, _multiPolygon2, "POINT(5983160 3877895)")]
    [InlineData(true, _multiPolygon2, "POINT(5904218 3885812)")]
    [InlineData(true, _multiPolygon2, "POINT(5980350 3881532)")]
    [InlineData(true, _multiPolygon2, "POINT(5938462 3888870)")]
    [InlineData(true, _multiPolygon2, "POINT(5935617 3876517)")]

    [InlineData(false, _multiPolygon, "POINT(8106013 2965378)")]
    [InlineData(false, _multiPolygon, "POINT(8103993 2968354)")]
    [InlineData(false, _multiPolygon, "POINT(8095719 2979259)")]
    [InlineData(false, _multiPolygon, "POINT(7987738 3121407)")]
    [InlineData(false, _multiPolygon, "POINT(8131859 2913399)")]
    [InlineData(false, _multiPolygon, "POINT(8396274 3327573)")]
    [InlineData(false, _multiPolygon, "POINT(7972870 2451108)")]
    [InlineData(false, _multiPolygon, "POINT(8742132 2743403)")]
    [InlineData(false, _multiPolygon, "POINT(7510579 2674916)")]
    [InlineData(false, _multiPolygon, "POINT(7173531 4243329)")]
    [InlineData(false, _multiPolygon, "POINT(6420167 4157720)")]
    [InlineData(false, _multiPolygon, "POINT(5786962 4057498)")]
    [InlineData(false, _multiPolygon, "POINT(5283089 3431326)")]
    [InlineData(false, _multiPolygon, "POINT(6217455 4590723)")]
    [InlineData(false, _multiPolygon, "POINT(4568861 3450894)")]
    [InlineData(false, _multiPolygon, "POINT(5151006 2379553)")]
    [InlineData(false, _multiPolygon, "POINT(6647949 2032223)")]
    [InlineData(false, _multiPolygon, "POINT(6688207 2014252)")]
    [InlineData(false, _multiPolygon, "POINT(9426587 5026108)")]
    [InlineData(false, _multiPolygon, "POINT(6680101 2010578)")]
    [InlineData(false, _multiPolygon, "POINT(8136428 2925111)")]

    [InlineData(true, _multiPolygon, "POINT(8106335 2964957)")]
    [InlineData(true, _multiPolygon, "POINT(8106261 2965026)")]
    [InlineData(true, _multiPolygon, "POINT(6681703 2012127)")]
    [InlineData(true, _multiPolygon, "POINT(7040772 4223684)")]
    [InlineData(true, _multiPolygon, "POINT(7997152 4429146)")]
    [InlineData(true, _multiPolygon, "POINT(5165712 3323728)")]
    [InlineData(true, _multiPolygon, "POINT(5532609 3666166)")]
    [InlineData(true, _multiPolygon, "POINT(5077656 2345334)")]
    [InlineData(true, _multiPolygon, "POINT(6300649 3279701)")]
    [InlineData(true, _multiPolygon, "POINT(7034444 4233635)")]
    [InlineData(true, _multiPolygon, "POINT(6662655 5148433)")]
    [InlineData(true, _multiPolygon, "POINT(5204847 4644560)")]
    [InlineData(true, _multiPolygon, "POINT(9353238 4982106)")]
    [InlineData(true, _multiPolygon, "POINT(5527717 2810072)")]
    [InlineData(true, _multiPolygon, "POINT(5860371 4033064)")]
    [InlineData(true, _multiPolygon, "POINT(6711574 2042032)")]
    [InlineData(true, _multiPolygon, "POINT(5635341 4052632)")]
    [InlineData(true, _multiPolygon, "POINT(5134376.957172996 3241476.1388561386)")]
    //
    public void IsPointInPolygon(bool isPointInPolygon, string polygonWkt, string pointWkt)
    {
        // Arrange 
        var sut = WktParser.Parse(polygonWkt);
        var point = WktParser.Parse(pointWkt).AsPoint();

        // Act
        bool isPointInPolygonActually = TopologyUtility.IsPointInPolygon<Point>(sut, point);

        // Assert
        Assert.Equal(isPointInPolygon, isPointInPolygonActually);
    }

    [Theory]
    [InlineData(30.0000, 44.0000, 30.0001, 44.0001, 6)] // ~15 m
    [InlineData(30.0000, 44.0000, 30.0010, 44.0010, 6)] // ~150 m
    [InlineData(30.0000, 44.0000, 30.0100, 44.0100, 4)] // ~1.5 km
    [InlineData(30.0000, 44.0000, 30.0500, 44.0500, 5)] // ~7.5 km
    [InlineData(30.0000, 44.0000, 30.5000, 44.5000, 4)] // ~68 km
    [InlineData(30.0000, 44.0000, 31.0000, 45.0000, 3)] // ~137 km
    [InlineData(66.0000, 70.0000, 95.0000, 33.1234, 0)] // ~137 km
    public void VincentyDistance_ShouldMatchSqlGeography(double longitude1, double latitude1, double longitude2, double latitude2, int precision)
    {
        // Arrange
        var p1 = new Point(longitude1, latitude1);
        var p2 = new Point(longitude2, latitude2);

        var expected = p1.AsSqlGeography().STDistance(p2.AsSqlGeography()).Value;

        // Act
        var actual = SpatialUtility.VincentyDistance(p1, p2);

        // Assert
        Assert.Equal(expected, actual, precision: precision);
    }


}

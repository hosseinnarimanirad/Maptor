using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Collections.Generic;
using Xunit;

namespace IRI.Maptor.Tests.TheGeometry;

// RemoveConsecutiveDuplicatePoints is the first step of every simplification experiment in
// research/ (LRSimplification, TrajectoryCompression, FastSimplification). Between 2026-05-22 and
// 2026-09-26 it threw on one-ring polygons and turned multi-ring polygons into MultiLineStrings.
public class Geometry_RemoveConsecutiveDuplicatePointsTest
{
    private static List<Point> Square(double repeatIndex = -1)
    {
        var points = new List<Point> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };

        if (repeatIndex >= 0)
            points.Insert((int)repeatIndex + 1, points[(int)repeatIndex]);

        return points;
    }

    [Fact]
    public void LineString_RemovesConsecutiveDuplicates_AndKeepsType()
    {
        var line = Geometry<Point>.Create(new List<Point> { new(0, 0), new(1, 1), new(1, 1), new(2, 0), new(2, 0), new(2, 0), new(3, 1) }, GeometryType.LineString);

        line.RemoveConsecutiveDuplicatePoints();

        Assert.Equal(GeometryType.LineString, line.Type);
        Assert.Equal(new List<Point> { new(0, 0), new(1, 1), new(2, 0), new(3, 1) }, line.Points);
    }

    [Fact]
    public void LineString_CollapsingToOnePoint_BecomesPoint()
    {
        var line = Geometry<Point>.Create(new List<Point> { new(5, 5), new(5, 5), new(5, 5) }, GeometryType.LineString);

        line.RemoveConsecutiveDuplicatePoints();

        Assert.Equal(GeometryType.Point, line.Type);
        Assert.Single(line.Points!);
    }

    [Fact]
    public void OneRingPolygon_KeepsPolygonType_AndRemovesDuplicates()
    {
        var polygon = Geometry<Point>.Create(Square(repeatIndex: 1), GeometryType.Polygon);

        Assert.Equal(5, polygon.TotalNumberOfPoints);

        polygon.RemoveConsecutiveDuplicatePoints();

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.Equal(1, polygon.NumberOfGeometries);
        Assert.Equal(4, polygon.TotalNumberOfPoints);
        Assert.Null(polygon.Points);
    }

    [Fact]
    public void MultiRingPolygon_KeepsPolygonType()
    {
        var polygon = Geometry<Point>.FromWkt("POLYGON((0 0, 10 0, 10 10, 10 10, 0 10, 0 0), (2 2, 2 4, 2 4, 4 4, 4 2, 2 2))", 0);

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.True(polygon.HasDuplicatePoints());

        polygon.RemoveConsecutiveDuplicatePoints();

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.Equal(2, polygon.NumberOfGeometries);
        Assert.Equal(4, polygon.Geometries![0].NumberOfPoints);
        Assert.Equal(4, polygon.Geometries![1].NumberOfPoints);
        Assert.False(polygon.HasDuplicatePoints());
    }

    [Fact]
    public void MultiPolygon_KeepsMultiPolygonType()
    {
        var multiPolygon = Geometry<Point>.FromWkt("MULTIPOLYGON(((0 0, 0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 30 30, 20 30, 20 20)))", 0);

        Assert.Equal(GeometryType.MultiPolygon, multiPolygon.Type);

        multiPolygon.RemoveConsecutiveDuplicatePoints();

        Assert.Equal(GeometryType.MultiPolygon, multiPolygon.Type);
        Assert.Equal(2, multiPolygon.NumberOfGeometries);
        Assert.All(multiPolygon.Geometries!, p => Assert.Equal(GeometryType.Polygon, p.Type));
        Assert.Equal(8, multiPolygon.TotalNumberOfPoints);
    }

    [Fact]
    public void MultiLineString_WithOneLine_KeepsMultiLineStringType()
    {
        var multiLine = Geometry<Point>.FromWkt("MULTILINESTRING((0 0, 1 1, 1 1, 2 2))", 0);

        Assert.Equal(GeometryType.MultiLineString, multiLine.Type);

        multiLine.RemoveConsecutiveDuplicatePoints();

        Assert.Equal(GeometryType.MultiLineString, multiLine.Type);
        Assert.Equal(3, multiLine.TotalNumberOfPoints);
    }
}

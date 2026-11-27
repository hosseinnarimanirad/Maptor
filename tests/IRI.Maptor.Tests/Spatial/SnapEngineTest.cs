using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.Spatial;

public class SnapEngineTest
{
    private static SnapIndex<Point> Index(params string?[] wkts)
    {
        var geometries = new List<Geometry<Point>?>();

        foreach (var wkt in wkts)
            geometries.Add(wkt is null ? null : Geometry<Point>.FromWkt(wkt, 3857));

        return new SnapIndex<Point>(geometries);
    }

    private static SnapResult<Point>? Snap(SnapIndex<Point> index, double x, double y, double tolerance, SnapOptions? options = null, System.Func<int, bool>? accept = null)
        => SnapEngine.Snap(index, new Point(x, y), tolerance, options, accept);

    #region Vertices

    [Fact]
    public void Snap_VertexWithinTolerance_ReturnsTheVertex()
    {
        var index = Index("LINESTRING(0 0, 100 0, 100 100)");

        var result = Snap(index, 98, 3, 5);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Vertex, result!.Kind);
        Assert.Equal(100, result.Location.X);
        Assert.Equal(0, result.Location.Y);
        Assert.Equal(0, result.GeometryIndex);
        Assert.Equal(System.Math.Sqrt(13), result.Distance, 9);
    }

    [Fact]
    public void Snap_NothingWithinTolerance_ReturnsNull()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        Assert.Null(Snap(index, 50, 20, 5));
    }

    [Fact]
    public void Snap_ExactlyAtTolerance_Snaps()
    {
        var index = Index("POINT(10 10)");

        var result = Snap(index, 13, 14, 5);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Vertex, result!.Kind);
    }

    [Fact]
    public void Snap_VertexBeatsNearerEdge()
    {
        // the edge (y = 0) is 1 away, the vertex (100 0) about 4.1 away
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = Snap(index, 96, 1, 5);

        Assert.Equal(SnapKind.Vertex, result!.Kind);
        Assert.Equal(100, result.Location.X);
    }

    [Fact]
    public void Snap_NearestOfSeveralVertices_Wins()
    {
        var index = Index("POINT(0 0)", "POINT(3 0)", "POINT(10 0)");

        var result = Snap(index, 2, 0, 5);

        Assert.Equal(1, result!.GeometryIndex);
        Assert.Equal(3, result.Location.X);
    }

    [Fact]
    public void Snap_EqualDistances_FirstGeometryWins()
    {
        var index = Index("POINT(0 0)", "POINT(4 0)");

        var result = Snap(index, 2, 0, 5);

        Assert.Equal(0, result!.GeometryIndex);
    }

    [Fact]
    public void Snap_ReturnsTheIndexedVertexItself()
    {
        var geometry = Geometry<Point>.FromWkt("POINT(7 8)", 3857);
        var index = new SnapIndex<Point>(new List<Geometry<Point>?> { geometry });

        var result = SnapEngine.Snap(index, new Point(7.5, 8.5), 1);

        // the stored coordinate, bit for bit, so a snapped vertex equals its target
        Assert.Equal(geometry.Points![0].X, result!.Location.X);
        Assert.Equal(geometry.Points[0].Y, result.Location.Y);
    }

    #endregion

    #region Edges

    [Fact]
    public void Snap_NearEdgeOnly_ReturnsProjectedPoint()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = Snap(index, 40, 3, 5);

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(40, result.Location.X, 9);
        Assert.Equal(0, result.Location.Y, 9);
        Assert.Equal(3, result.Distance, 9);
    }

    [Fact]
    public void Snap_EdgeProjection_IsClampedToTheSegment()
    {
        // past the end of the segment the nearest place is the end itself; turn vertices off to see the edge result
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = Snap(index, 103, 0, 5, new SnapOptions { SnapToVertices = false });

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(100, result.Location.X, 9);
        Assert.Equal(0, result.Location.Y, 9);
    }

    [Fact]
    public void Snap_PolygonClosingSegment_IsAnEdge()
    {
        // WKT repeats the first point; the geometry stores the ring without it, so the edge from (0 100) back to
        // (0 0) is the implicit closing segment
        var index = Index("POLYGON((0 0, 100 0, 100 100, 0 100, 0 0))");

        var result = Snap(index, 2, 50, 5);

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(0, result.Location.X, 9);
        Assert.Equal(50, result.Location.Y, 9);
    }

    [Fact]
    public void Snap_PolygonHole_IsSnappedTo()
    {
        var index = Index("POLYGON((0 0, 100 0, 100 100, 0 100, 0 0), (40 40, 40 60, 60 60, 60 40, 40 40))");

        var edge = Snap(index, 50, 41, 5);
        Assert.Equal(SnapKind.Edge, edge!.Kind);
        Assert.Equal(40, edge.Location.Y, 9);

        var vertex = Snap(index, 61, 61, 5);
        Assert.Equal(SnapKind.Vertex, vertex!.Kind);
        Assert.Equal(60, vertex.Location.X);
    }

    [Fact]
    public void Snap_MultiLineString_EveryPartIsSnappedTo()
    {
        var index = Index("MULTILINESTRING((0 0, 100 0), (0 50, 100 50))");

        var result = Snap(index, 30, 48, 5);

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(50, result.Location.Y, 9);
        Assert.Equal(0, result.GeometryIndex);
    }

    [Fact]
    public void Snap_MultiPolygon_SecondPolygonIsSnappedTo()
    {
        var index = Index("MULTIPOLYGON(((0 0, 10 0, 10 10, 0 10, 0 0)), ((50 0, 60 0, 60 10, 50 10, 50 0)))");

        var result = Snap(index, 55, 11, 3);

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(10, result.Location.Y, 9);
    }

    [Fact]
    public void Snap_PointFeature_HasNoEdges()
    {
        var index = Index("MULTIPOINT((0 0), (100 0))");

        Assert.Null(Snap(index, 50, 0, 5));
    }

    [Fact]
    public void Snap_NearestEdge_ReportsItsGeometry()
    {
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(0 10, 100 10)");

        var result = Snap(index, 50, 7, 5);

        Assert.Equal(1, result!.GeometryIndex);
        Assert.Equal(10, result.Location.Y, 9);
    }

    [Fact]
    public void Index_PolygonVertices_AreNotDoubled()
    {
        // the stored ring holds each corner once, so four corners give four vertices
        var index = Index("POLYGON((0 0, 100 0, 100 100, 0 100, 0 0))");

        Assert.Equal(4, index.VertexCount);
    }

    #endregion

    #region Options, filter and input

    [Fact]
    public void Snap_VerticesOff_SnapsToTheEdge()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = Snap(index, 96, 1, 5, new SnapOptions { SnapToVertices = false });

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(96, result.Location.X, 9);
    }

    [Fact]
    public void Snap_EdgesOff_IgnoresEdges()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        Assert.Null(Snap(index, 50, 1, 5, new SnapOptions { SnapToEdges = false }));
    }

    [Fact]
    public void Snap_BothOff_ReturnsNull()
    {
        var index = Index("POINT(0 0)");

        Assert.Null(Snap(index, 0, 0, 5, new SnapOptions { SnapToVertices = false, SnapToEdges = false }));
    }

    [Fact]
    public void Snap_Accept_LeavesTheEditedGeometryOut()
    {
        // dragging a vertex of geometry 0: its own old vertex must not pull it back
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(0 10, 100 10)");

        var result = Snap(index, 100, 1, 15, accept: g => g != 0);

        Assert.Equal(1, result!.GeometryIndex);
        Assert.Equal(SnapKind.Vertex, result.Kind);
        Assert.Equal(10, result.Location.Y);
    }

    [Fact]
    public void Snap_Accept_AppliesToEdgesToo()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        Assert.Null(Snap(index, 50, 1, 5, accept: _ => false));
    }

    [Fact]
    public void Index_SkipsNullAndEmptyGeometries_KeepsPositions()
    {
        var index = new SnapIndex<Point>(new List<Geometry<Point>?>
        {
            null,
            Geometry<Point>.Empty,
            Geometry<Point>.FromWkt("POINT(5 5)", 3857),
        });

        Assert.Equal(3, index.GeometryCount);
        Assert.Equal(1, index.VertexCount);
        Assert.Equal(2, SnapEngine.Snap(index, new Point(5, 6), 2)!.GeometryIndex);
    }

    [Fact]
    public void Snap_EmptyIndex_ReturnsNull()
    {
        var index = new SnapIndex<Point>(new List<Geometry<Point>?>());

        Assert.Null(SnapEngine.Snap(index, new Point(0, 0), 10));
    }

    [Fact]
    public void Snap_NaNPoint_ReturnsNull()
    {
        var index = Index("POINT(0 0)");

        Assert.Null(SnapEngine.Snap(index, new Point(double.NaN, 0), 10));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Snap_BadTolerance_Throws(double tolerance)
    {
        var index = Index("POINT(0 0)");

        Assert.Throws<System.ArgumentOutOfRangeException>(() => SnapEngine.Snap(index, new Point(0, 0), tolerance));
    }

    [Fact]
    public void Snap_ZeroTolerance_SnapsOnlyOnExactHit()
    {
        var index = Index("POINT(3 4)");

        Assert.NotNull(Snap(index, 3, 4, 0));
        Assert.Null(Snap(index, 3, 4.001, 0));
    }

    [Fact]
    public void Snap_ManyVertices_FindsTheRightOne()
    {
        // a grid of 10 000 points one unit apart exercises the cell size of the vertex index
        var geometries = new List<Geometry<Point>?>();

        for (int x = 0; x < 100; x++)
        {
            for (int y = 0; y < 100; y++)
                geometries.Add(Geometry<Point>.FromWkt($"POINT({x} {y})", 3857));
        }

        var index = new SnapIndex<Point>(geometries);

        var result = SnapEngine.Snap(index, new Point(42.2, 17.1), 0.4);

        Assert.Equal(42, result!.Location.X);
        Assert.Equal(17, result.Location.Y);
        Assert.Equal(42 * 100 + 17, result.GeometryIndex);
    }

    #endregion
}

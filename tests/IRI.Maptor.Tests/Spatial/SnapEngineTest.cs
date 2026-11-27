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

    /// <summary>Vertices and edges only, as the engine was before line ends, midpoints and intersections.</summary>
    private static SnapOptions Classic(bool vertices = true, bool edges = true)
        => new() { SnapToVertices = vertices, SnapToEdges = edges, SnapToEnds = false, SnapToIntersections = false, SnapToMidpoints = false };

    /// <summary>Snaps with <see cref="Classic"/> options unless others are given: these tests are about vertices and edges.</summary>
    private static SnapResult<Point>? Snap(SnapIndex<Point> index, double x, double y, double tolerance, SnapOptions? options = null, System.Func<int, bool>? accept = null)
        => SnapEngine.Snap(index, new Point(x, y), tolerance, options ?? Classic(), accept);

    private static SnapResult<Point>? SnapAll(SnapIndex<Point> index, double x, double y, double tolerance, SnapOptions? options = null, System.Func<int, bool>? accept = null)
        => SnapEngine.Snap(index, new Point(x, y), tolerance, options ?? SnapOptions.Default, accept);

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

        var result = Snap(index, 103, 0, 5, Classic(vertices: false));

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

    #region Line ends

    [Fact]
    public void Snap_LineEnd_IsItsOwnKind_AndInnerVerticesStayVertices()
    {
        var index = Index("LINESTRING(0 0, 50 0, 100 0)");

        var end = SnapAll(index, 99, 2, 5);
        Assert.Equal(SnapKind.End, end!.Kind);
        Assert.Equal(100, end.Location.X);

        var start = SnapAll(index, 1, -2, 5);
        Assert.Equal(SnapKind.End, start!.Kind);
        Assert.Equal(0, start.Location.X);

        var inner = SnapAll(index, 51, 2, 5);
        Assert.Equal(SnapKind.Vertex, inner!.Kind);
        Assert.Equal(50, inner.Location.X);
    }

    [Fact]
    public void Snap_LineEnd_BeatsANearerVertex()
    {
        // the inner vertex (102 1) of the second line is 1 away, the end (100 0) of the first about 2.8
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(102 -50, 102 1, 102 50)");

        var result = SnapAll(index, 102, 2, 5);

        Assert.Equal(SnapKind.End, result!.Kind);
        Assert.Equal(100, result.Location.X);
        Assert.Equal(0, result.GeometryIndex);
    }

    [Fact]
    public void Snap_EndsOff_ALineEndIsAnOrdinaryVertex()
    {
        var index = Index("LINESTRING(0 0, 50 0, 100 0)");

        var result = SnapAll(index, 99, 2, 5, new SnapOptions { SnapToEnds = false });

        Assert.Equal(SnapKind.Vertex, result!.Kind);
        Assert.Equal(100, result.Location.X);
    }

    [Fact]
    public void Snap_OnlyEndsOn_InnerVerticesAreIgnored()
    {
        var index = Index("LINESTRING(0 0, 50 0, 100 0)");

        var onlyEnds = new SnapOptions { SnapToVertices = false, SnapToIntersections = false, SnapToMidpoints = false, SnapToEdges = false };

        Assert.Null(SnapAll(index, 51, 2, 5, onlyEnds));
        Assert.Equal(SnapKind.End, SnapAll(index, 99, 2, 5, onlyEnds)!.Kind);
    }

    [Fact]
    public void Snap_EveryPartOfAMultiLineHasEnds_PolygonsAndPointsHaveNone()
    {
        var lines = Index("MULTILINESTRING((0 0, 100 0), (0 50, 100 50))");
        Assert.Equal(SnapKind.End, SnapAll(lines, 1, 51, 5)!.Kind);
        Assert.Equal(SnapKind.End, SnapAll(lines, 99, 1, 5)!.Kind);

        var polygon = Index("POLYGON((0 0, 100 0, 100 100, 0 100, 0 0))");
        Assert.Equal(SnapKind.Vertex, SnapAll(polygon, 1, 1, 5)!.Kind);
        Assert.Equal(SnapKind.Vertex, SnapAll(polygon, 1, 99, 5)!.Kind);

        var point = Index("POINT(10 10)");
        Assert.Equal(SnapKind.Vertex, SnapAll(point, 11, 11, 5)!.Kind);
    }

    #endregion

    #region Midpoints

    [Fact]
    public void Snap_Midpoint_OfASegment_AndAnEdgeElsewhere()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        var midpoint = SnapAll(index, 52, 3, 5);
        Assert.Equal(SnapKind.Midpoint, midpoint!.Kind);
        Assert.Equal(50, midpoint.Location.X, 9);
        Assert.Equal(0, midpoint.Location.Y, 9);
        Assert.Equal(System.Math.Sqrt(13), midpoint.Distance, 9);

        // 20 from the midpoint: the edge under the pointer
        var edge = SnapAll(index, 30, 3, 5);
        Assert.Equal(SnapKind.Edge, edge!.Kind);
        Assert.Equal(30, edge.Location.X, 9);
    }

    [Fact]
    public void Snap_Midpoint_OfAPolygonsClosingSegment()
    {
        var index = Index("POLYGON((0 0, 100 0, 100 100, 0 100, 0 0))");

        var result = SnapAll(index, 3, 52, 5);

        Assert.Equal(SnapKind.Midpoint, result!.Kind);
        Assert.Equal(0, result.Location.X, 9);
        Assert.Equal(50, result.Location.Y, 9);
    }

    [Fact]
    public void Snap_MidpointsOff_FallsThroughToTheEdge()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = SnapAll(index, 52, 3, 5, new SnapOptions { SnapToMidpoints = false });

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(52, result.Location.X, 9);
    }

    [Fact]
    public void Snap_AStoredPoint_BeatsANearerMidpoint()
    {
        // the midpoint (3 0) of the short first segment is about 1.4 away, the line's end (0 0) about 4.1
        var index = Index("LINESTRING(0 0, 6 0, 100 0)");

        var result = SnapAll(index, 4, 1, 5);

        Assert.Equal(SnapKind.End, result!.Kind);
        Assert.Equal(0, result.Location.X);
    }

    #endregion

    #region Intersections

    [Fact]
    public void Snap_Intersection_WhereTwoGeometriesCross()
    {
        // they cross at (30 0), where neither has a vertex
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(30 -40, 30 60)");

        var result = SnapAll(index, 31, 2, 5);

        Assert.Equal(SnapKind.Intersection, result!.Kind);
        Assert.Equal(30, result.Location.X, 9);
        Assert.Equal(0, result.Location.Y, 9);
        Assert.Equal(0, result.GeometryIndex);
    }

    [Fact]
    public void Snap_Intersection_BeatsANearerMidpoint()
    {
        // the first line's midpoint (50 0) is 1 away, the crossing (47 0) about 3.2
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(47 -40, 47 60)");

        var result = SnapAll(index, 50, 1, 5);

        Assert.Equal(SnapKind.Intersection, result!.Kind);
        Assert.Equal(47, result.Location.X, 9);
    }

    [Fact]
    public void Snap_Intersection_OfALineWithItself()
    {
        // the first and third segments cross at (50 50)
        var index = Index("LINESTRING(0 0, 100 100, 100 0, 0 100)");

        var result = SnapAll(index, 51, 51, 5);

        Assert.Equal(SnapKind.Intersection, result!.Kind);
        Assert.Equal(50, result.Location.X, 9);
        Assert.Equal(50, result.Location.Y, 9);
    }

    [Fact]
    public void Snap_Intersection_NeedsMoreThanASharedEndVertex()
    {
        var onlyIntersections = new SnapOptions { SnapToEnds = false, SnapToVertices = false, SnapToMidpoints = false, SnapToEdges = false };

        // two lines that only meet at their ends, and two consecutive segments of one line: a vertex, not an intersection
        Assert.Null(SnapAll(Index("LINESTRING(0 0, 50 0)", "LINESTRING(50 0, 50 50)"), 50, 1, 5, onlyIntersections));
        Assert.Null(SnapAll(Index("LINESTRING(0 0, 50 0, 50 50)"), 50, 1, 5, onlyIntersections));

        // one line's end on the inside of another (a T junction) is an intersection
        var junction = SnapAll(Index("LINESTRING(0 0, 100 0)", "LINESTRING(40 0, 40 50)"), 41, 1, 5, onlyIntersections);
        Assert.Equal(SnapKind.Intersection, junction!.Kind);
        Assert.Equal(40, junction.Location.X, 9);

        // lines lying along each other have no single meeting point
        Assert.Null(SnapAll(Index("LINESTRING(0 0, 100 0)", "LINESTRING(50 0, 150 0)"), 75, 1, 5, onlyIntersections));
    }

    [Fact]
    public void Snap_Intersection_NeedsBothGeometriesAccepted()
    {
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(30 -40, 30 60)");

        var onlyIntersections = new SnapOptions { SnapToEnds = false, SnapToVertices = false, SnapToMidpoints = false, SnapToEdges = false };

        Assert.NotNull(SnapAll(index, 31, 2, 5, onlyIntersections));
        Assert.Null(SnapAll(index, 31, 2, 5, onlyIntersections, accept: g => g != 1));
    }

    [Fact]
    public void Snap_IntersectionsOff_FallsThroughToTheEdge()
    {
        var index = Index("LINESTRING(0 0, 100 0)", "LINESTRING(30 -40, 30 60)");

        var result = SnapAll(index, 31, 2, 5, new SnapOptions { SnapToIntersections = false });

        Assert.Equal(SnapKind.Edge, result!.Kind);
    }

    [Fact]
    public void Snap_Intersection_InADensePlace_IsStillFound()
    {
        // 200 parallel lines 0.01 apart, all crossed by one line: far more segments in range than are paired
        var wkts = new List<string?>();

        for (int i = 0; i < 200; i++)
            wkts.Add($"LINESTRING(0 {i * 0.01}, 100 {i * 0.01})");

        wkts.Add("LINESTRING(30 -40, 30 60)");

        var index = Index(wkts.ToArray());

        var result = SnapAll(index, 30.4, 0.004, 5, new SnapOptions { SnapToEnds = false, SnapToVertices = false, SnapToMidpoints = false, SnapToEdges = false });

        Assert.Equal(SnapKind.Intersection, result!.Kind);
        Assert.Equal(30, result.Location.X, 9);
    }

    #endregion

    #region Options, filter and input

    [Fact]
    public void Snap_VerticesOff_SnapsToTheEdge()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        var result = Snap(index, 96, 1, 5, Classic(vertices: false));

        Assert.Equal(SnapKind.Edge, result!.Kind);
        Assert.Equal(96, result.Location.X, 9);
    }

    [Fact]
    public void Snap_EdgesOff_IgnoresEdges()
    {
        var index = Index("LINESTRING(0 0, 100 0)");

        Assert.Null(Snap(index, 50, 1, 5, Classic(edges: false)));
    }

    [Fact]
    public void Snap_BothOff_ReturnsNull()
    {
        var index = Index("POINT(0 0)");

        Assert.Null(Snap(index, 0, 0, 5, Classic(vertices: false, edges: false)));
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

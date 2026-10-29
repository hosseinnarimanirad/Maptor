using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.TheGeometry;

/// <summary>
/// <see cref="Geometry{T}.CreatePolygonOrMultiPolygon"/>: grouping rings into polygons with holes
/// and OGC orientation (outer counter-clockwise, holes clockwise).
/// </summary>
public class Geometry_PolygonBuilderTest
{
    // open rings (no repeated closing vertex), as the builder expects
    private static Geometry<Point> Ring(params double[] xy)
    {
        var points = new List<Point>();
        for (int i = 0; i < xy.Length; i += 2)
            points.Add(new Point(xy[i], xy[i + 1]));

        return Geometry<Point>.Create(points, GeometryType.LineString, 0);
    }

    private static Geometry<Point> SquareCw(double x, double y, double size) => Ring(x, y, x, y + size, x + size, y + size, x + size, y);

    private static Geometry<Point> SquareCcw(double x, double y, double size) => Ring(x, y, x + size, y, x + size, y + size, x, y + size);

    private static bool IsCw(Geometry<Point> ring) => SpatialUtility.IsClockwise(ring.Points);

    [Fact]
    public void SingleRing_IsOrientedCounterClockwise()
    {
        var polygon = Geometry<Point>.CreatePolygonOrMultiPolygon([SquareCw(0, 0, 10)], 0);

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.False(IsCw(polygon.Geometries[0]));
    }

    [Fact]
    public void SingleRing_KeepsOrientation_WhenFixingIsOff()
    {
        var polygon = Geometry<Point>.CreatePolygonOrMultiPolygon([SquareCw(0, 0, 10)], 0, fixOrientation: false);

        Assert.True(IsCw(polygon.Geometries[0]));
    }

    [Fact]
    public void HoleBeforeItsOuterRing_IsStillAHole()
    {
        var polygon = Geometry<Point>.CreatePolygonOrMultiPolygon([SquareCcw(4, 4, 2), SquareCw(0, 0, 10)], 0);

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.Equal(2, polygon.Geometries.Count);
        Assert.Equal(100, SpatialUtility.GetUnsignedEuclideanArea(polygon.Geometries[0].Points));
        Assert.False(IsCw(polygon.Geometries[0]));
        Assert.True(IsCw(polygon.Geometries[1]));
    }

    [Fact]
    public void HoleTouchingTheOuterRing_IsStillAHole()
    {
        // the hole's first vertex (0, 4) lies on the outer ring's left edge
        var hole = Ring(0, 4, 3, 6, 3, 2);

        var polygon = Geometry<Point>.CreatePolygonOrMultiPolygon([SquareCw(0, 0, 10), hole], 0);

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.Equal(2, polygon.Geometries.Count);
    }

    [Fact]
    public void IslandInsideAHole_IsItsOwnPolygon()
    {
        var polygon = Geometry<Point>.CreatePolygonOrMultiPolygon(
            [SquareCw(0, 0, 10), SquareCcw(2, 2, 6), SquareCw(4, 4, 2)], 0);

        Assert.Equal(GeometryType.MultiPolygon, polygon.Type);
        Assert.Equal(new[] { 2, 1 }, polygon.Geometries.Select(p => p.Geometries.Count));
    }

    [Fact]
    public void SeparateRings_AreSeparatePolygons()
    {
        var polygon = Geometry<Point>.CreatePolygonOrMultiPolygon([SquareCw(0, 0, 1), SquareCw(5, 5, 2)], 0);

        Assert.Equal(GeometryType.MultiPolygon, polygon.Type);
        Assert.All(polygon.Geometries, p => Assert.False(IsCw(p.Geometries[0])));
    }
}

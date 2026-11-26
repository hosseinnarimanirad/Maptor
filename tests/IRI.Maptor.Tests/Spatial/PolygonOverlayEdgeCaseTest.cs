using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

using Xunit;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// How <see cref="Geometry{T}.Intersection"/> and <see cref="Geometry{T}.Union"/> behave on the cases polygon
/// topology rules meet: shared edges and vertices, slivers, containment, duplicates, holes. Projected
/// coordinates in metres. Found 2026-11-26 (risk W11): <c>Intersection</c> throws <see cref="System.NotImplementedException"/>
/// when the boundaries share edges or vertices, so the topology rules measure overlaps with
/// <see cref="PolygonOverlap"/> instead. The two tests marked "documents a limitation" fail once that is implemented,
/// as a reminder to look again.
/// </summary>
public class PolygonOverlayEdgeCaseTest
{
    private static Geometry<Point> Wkt(string wkt) => Geometry<Point>.FromWkt(wkt, 0);

    private static readonly Geometry<Point> Square = Wkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))");

    private static double OverlapArea(string other) => PolygonMeasure.Area(Square.Intersection(Wkt(other)), isGeodetic: false);

    [Fact]
    public void SharedEdge_HasNoOverlapArea() => Assert.Equal(0, OverlapArea("POLYGON((10 0, 20 0, 20 10, 10 10, 10 0))"), 6);

    [Fact]
    public void SharedVertex_HasNoOverlapArea() => Assert.Equal(0, OverlapArea("POLYGON((10 10, 20 10, 20 20, 10 20, 10 10))"), 6);

    [Fact]
    public void ProperOverlap_WithCollinearEdges_Throws_DocumentsALimitation()
        => Assert.Throws<System.NotImplementedException>(() => OverlapArea("POLYGON((5 0, 15 0, 15 10, 5 10, 5 0))"));

    [Fact]
    public void SliverOverlap_WithCollinearEdges_Throws_DocumentsALimitation()
        => Assert.Throws<System.NotImplementedException>(() => OverlapArea("POLYGON((9.99 0, 20 0, 20 10, 9.99 10, 9.99 0))"));

    [Fact]
    public void PolygonInside_OverlapsByItsWholeArea() => Assert.Equal(4, OverlapArea("POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))"), 6);

    [Fact]
    public void Duplicate_OverlapsByTheWholeArea() => Assert.Equal(100, OverlapArea("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))"), 6);

    [Fact]
    public void PolygonInAHole_DoesNotOverlap()
    {
        var withHole = Wkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 8, 8 8, 8 2, 2 2))");

        Assert.Equal(0, PolygonMeasure.Area(withHole.Intersection(Wkt("POLYGON((3 3, 7 3, 7 7, 3 7, 3 3))")), false), 6);
    }

    [Fact]
    public void Area_TakesHolesAway_AndPerimeterAddsThem()
    {
        var withHole = Wkt("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 2 8, 8 8, 8 2, 2 2))");

        Assert.Equal(64, PolygonMeasure.Area(withHole, false), 6);
        Assert.Equal(64, PolygonMeasure.Perimeter(withHole, false), 6);
    }

    [Fact]
    public void Union_OfFourSquaresAroundAGap_KeepsTheGapAsAHole()
    {
        // Four 10 × 10 squares around a 2 × 2 gap in the middle of a 22 × 22 block.
        var union = Wkt("POLYGON((0 0, 12 0, 12 10, 10 10, 10 12, 0 12, 0 0))")
            .Union(Wkt("POLYGON((12 0, 22 0, 22 12, 12 12, 12 10, 12 0))"))
            .Union(Wkt("POLYGON((12 12, 22 12, 22 22, 10 22, 10 12, 12 12))"))
            .Union(Wkt("POLYGON((0 12, 10 12, 10 22, 0 22, 0 12))"));

        Assert.Equal(22 * 22 - 4, PolygonMeasure.Area(union, false), 6);
    }
}

using System.Linq;
using IRI.Maptor.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Tests.Common;

// 2026-10-06: RDP on a ring divides it at the vertex farthest from the first one; on a long thin polygon both halves lie
// within the tolerance of their chords, so the ring keeps two vertices and the polygon degenerates. The comparison used
// to skip such a result (another geometry type, or a structure TLVD cannot pair) and the 1c runs lost those rows; with
// measureCollapsed it is measured like any other result.
public class CollapsedSimplificationMeasureTest
{
    // a 100 × 1 strip, ring stored without its closing vertex
    private static Geometry<Point> ThinStrip()
    {
        var ring = Enumerable.Range(0, 21).Select(i => new Point(i * 5, 0))
                             .Concat(Enumerable.Range(0, 21).Select(i => new Point(100 - i * 5, 1)))
                             .ToList();

        return Geometry<Point>.Create(new List<Geometry<Point>> { Geometry<Point>.Create(ring, GeometryType.LineString, IRI.Maptor.Core.SpatialReferenceSystem.SridHelper.WebMercator) }, GeometryType.Polygon, IRI.Maptor.Core.SpatialReferenceSystem.SridHelper.WebMercator);
    }

    private static Geometry<Point> Rdp(Geometry<Point> g) => Rdp(g, false);

    private static Geometry<Point> Rdp(Geometry<Point> g, bool retain3Points) =>
        g.Simplify(SimplificationType.RamerDouglasPeucker, new SimplificationParameters() { DistanceThreshold = 3, AreaThreshold = 9, Retain3Points = retain3Points });

    [Fact]
    public void RamerDouglasPeucker_ThinStrip_CollapsesWithoutRetain3Points()
    {
        var simplified = Rdp(ThinStrip(), false);

        Assert.True(simplified.IsNullOrEmpty() || simplified.Type != GeometryType.Polygon || simplified.Geometries.Any(r => r.Points is null || r.Points.Count < 3) || simplified.TotalNumberOfPoints < 3);
    }

    // with Retain3Points the ring keeps a third vertex: the one farthest from the line through the two RDP keeps
    [Fact]
    public void RamerDouglasPeucker_ThinStrip_KeepsATriangleWithRetain3Points()
    {
        var simplified = Rdp(ThinStrip(), true);

        Assert.Equal(GeometryType.Polygon, simplified.Type);

        var ring = simplified.Geometries[0].Points;

        Assert.Equal(3, ring.Count);
        Assert.Equal(new Point(0, 0), ring[0]);
        Assert.Contains(new Point(100, 1), ring);
    }

    // a tolerance larger than the whole ring: RDP's closed line keeps only its first vertex; Retain3Points still gives three
    [Fact]
    public void RamerDouglasPeucker_ToleranceLargerThanTheRing_KeepsATriangleWithRetain3Points()
    {
        var simplified = ThinStrip().Simplify(SimplificationType.RamerDouglasPeucker, new SimplificationParameters() { DistanceThreshold = 1000, AreaThreshold = 1e6, Retain3Points = true });

        Assert.Equal(GeometryType.Polygon, simplified.Type);
        Assert.Equal(3, simplified.Geometries[0].Points.Count);
    }

    [Fact]
    public async Task CompareBySingleFeature_CollapsedResult_IsMeasuredOnlyWhenAsked()
    {
        var feature = ThinStrip();

        var box = new BoundingBox(0, -49.5, 100, 50.5);
        var size = new System.Drawing.Size(128, 128);

        using var original = SimplificationHelper.RenderForMetric(new() { feature }, box, size);

        var skipped = await SimplificationHelper.CompareBySingleFeature(feature, Rdp, box, size, original, "strip", "RDP", 0, null, false);

        var measured = await SimplificationHelper.CompareBySingleFeature(feature, Rdp, box, size, original, "strip", "RDP", 0, null, false, measureCollapsed: true);

        Assert.NotNull(measured.Item1);
        Assert.NotNull(measured.Item1.Raster);
        Assert.True(measured.Item1.Compression > 0.9);

        // the published behaviour is kept when not asked (skipped when the type changed; measured otherwise)
        if (measured.simplified.Type != feature.Type)
            Assert.Null(skipped.Item1);
    }
}

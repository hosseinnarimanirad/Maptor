using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.Common;

// The raster of the visual metric: aliased 1-px lines as in the published run, independent of the map renderer.
public class MetricRasterizerTest
{
    private static int DrawnPixels(System.Drawing.Bitmap bitmap)
    {
        var mask = ImageHelper.ToMask(bitmap);

        return mask.Cast<bool>().Count(b => b);
    }

    private static Geometry<Point> Line(params (double x, double y)[] points)
    {
        return Geometry<Point>.Create(points.Select(p => new Point(p.x, p.y)).ToList(), GeometryType.LineString);
    }

    // extent 0..32 x 0..32 on a 32 x 32 bitmap: one map unit = one pixel, y up
    private static readonly BoundingBox Extent = new(0, 0, 32, 32);

    [Fact]
    public void Published_DrawsADiagonalLineOnePixelWide()
    {
        using var bitmap = MetricRasterizer.Render(new List<Geometry<Point>> { Line((2, 2), (22, 22)) }, Extent, 32, 32, MetricRenderOptions.Published);

        // an aliased 1-px diagonal covers one pixel per column
        Assert.InRange(DrawnPixels(bitmap), 20, 22);
    }

    [Fact]
    public void AntiAliased_CoversMorePixelsThanPublished()
    {
        var line = new List<Geometry<Point>> { Line((2.3, 5.5), (29.1, 17.2)) };

        using var aliased = MetricRasterizer.Render(line, Extent, 32, 32, MetricRenderOptions.Published);
        using var smooth = MetricRasterizer.Render(line, Extent, 32, 32, MetricRenderOptions.AntiAliased);

        Assert.True(DrawnPixels(smooth) > 1.5 * DrawnPixels(aliased));
    }

    [Fact]
    public void Polygon_IsDrawnAsClosedOutlineWithoutFill()
    {
        var polygon = Geometry<Point>.Create(new List<Point> { new(4, 4), new(24, 4), new(24, 24), new(4, 24) }, GeometryType.Polygon);

        using var bitmap = MetricRasterizer.Render(new List<Geometry<Point>> { polygon }, Extent, 32, 32, MetricRenderOptions.Published);

        var mask = ImageHelper.ToMask(bitmap);

        // outline of a 20 x 20 square: about 80 pixels, the interior stays empty
        Assert.InRange(DrawnPixels(bitmap), 76, 84);
        Assert.False(mask[14, 14]);
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Infrastructure.GdiPlus.Helpers;

/// <summary>
/// How a geometry is drawn for the visual metric (and for the rasterization oracle that labels triads).
/// The metric must not depend on the map renderer, whose settings change for cartographic reasons.
/// </summary>
/// <param name="AntiAlias">GDI+ <see cref="SmoothingMode.AntiAlias"/>; otherwise the GDI+ default (aliased).</param>
/// <param name="StrokeWidth">Pen width in pixels.</param>
public sealed record MetricRenderOptions(bool AntiAlias, float StrokeWidth)
{
    /// <summary>
    /// The rendering of the published LAS run (2024-10): a 1-pixel pen with the GDI+ default smoothing (none),
    /// so a line covers the pixels it passes through. Any non-transparent pixel counts as drawn.
    /// </summary>
    public static readonly MetricRenderOptions Published = new(AntiAlias: false, StrokeWidth: 1);

    /// <summary>
    /// The map renderer since 2025 (<c>GdiBitmapRenderStrategy</c>): anti-aliased 1-pixel pen. Because any
    /// partially covered pixel counts as drawn, a line covers about twice as many pixels as with <see cref="Published"/>.
    /// The 2026-09-28 revision runs and the first oracle file used this rendering.
    /// </summary>
    public static readonly MetricRenderOptions AntiAliased = new(AntiAlias: true, StrokeWidth: 1);

    public override string ToString() => $"GDI+ DrawLines/DrawPolygon, pen {StrokeWidth} px, {(AntiAlias ? "SmoothingMode.AntiAlias" : "SmoothingMode.Default (aliased)")}, drawn = ARGB != 0 && != white";
}

/// <summary>
/// Draws geometries onto a transparent bitmap for the pixel comparison of the visual metric
/// (<see cref="ImageHelper.CalculateConfusionMatrixBitmaps"/>, <see cref="ImageHelper.CompareRasters"/>).
/// The map-to-screen transform is the one of the map views: the extent is fitted into the bitmap with one
/// scale for both axes, the top-left corner of the extent at pixel (0, 0).
/// Line strings are drawn as open polylines, polygon rings as closed outlines (no fill).
/// </summary>
public static class MetricRasterizer
{
    public static Bitmap Render(IEnumerable<Geometry<Point>> geometries, BoundingBox mapExtent, int width, int height, MetricRenderOptions options)
    {
        double xScale = width / mapExtent.Width;
        double yScale = height / mapExtent.Height;
        double scale = xScale > yScale ? yScale : xScale;

        PointF ToScreen(Point p) => new((float)((p.X - mapExtent.XMin) * scale), (float)(-(p.Y - mapExtent.YMax) * scale));

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using var graphics = CreateGraphics(bitmap, options);

        using var pen = CreatePen(options);

        foreach (var geometry in geometries)
        {
            Draw(graphics, pen, geometry, ToScreen, isRing: false);
        }

        return bitmap;
    }

    /// <summary>Draws one polyline given in screen coordinates (pixels) on a cleared bitmap.</summary>
    public static void RenderScreenPolyline(Bitmap bitmap, PointF[] points, MetricRenderOptions options)
    {
        using var graphics = CreateGraphics(bitmap, options);

        using var pen = CreatePen(options);

        graphics.Clear(Color.Transparent);

        graphics.DrawLines(pen, points);
    }

    public static Graphics CreateGraphics(Bitmap bitmap, MetricRenderOptions options)
    {
        var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = options.AntiAlias ? SmoothingMode.AntiAlias : SmoothingMode.Default;

        return graphics;
    }

    public static Pen CreatePen(MetricRenderOptions options) => new(Color.Blue, options.StrokeWidth);

    private static void Draw(Graphics graphics, Pen pen, Geometry<Point> geometry, Func<Point, PointF> toScreen, bool isRing)
    {
        if (geometry is null || geometry.IsEmpty())
            return;

        if (geometry.Points is not null)
        {
            if (geometry.Points.Count < 2)
                return;

            var points = new PointF[geometry.Points.Count];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = toScreen(geometry.Points[i]);
            }

            if (isRing || geometry.Type == GeometryType.Polygon)
                graphics.DrawPolygon(pen, points);
            else
                graphics.DrawLines(pen, points);

            return;
        }

        // the parts of a polygon are its rings
        var rings = geometry.Type == GeometryType.Polygon;

        foreach (var part in geometry.Geometries!)
        {
            Draw(graphics, pen, part, toScreen, rings);
        }
    }
}

using System;
using System.Windows.Media;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using System.Linq;

namespace IRI.Maptor.Extensions;

public static class GeometryExtensions
{
    public static void DrawGeometry(this StreamGeometryContext ctx, Geometry geo)
    {
        var pathGeometry = geo as PathGeometry ?? PathGeometry.CreateFromGeometry(geo);

        foreach (var figure in pathGeometry.Figures)
        {
            ctx.DrawFigure(figure);
        }
    }

    public static void DrawFigure(this StreamGeometryContext ctx, PathFigure figure)
    {
        ctx.BeginFigure(figure.StartPoint, figure.IsFilled, figure.IsClosed);

        foreach (var segment in figure.Segments)
        {
            var lineSegment = segment as LineSegment;

            if (lineSegment != null)
            {
                ctx.LineTo(lineSegment.Point, lineSegment.IsStroked, lineSegment.IsSmoothJoin);
                continue;
            }

            var bezierSegment = segment as BezierSegment;

            if (bezierSegment != null)
            {
                ctx.BezierTo(bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3, bezierSegment.IsStroked, bezierSegment.IsSmoothJoin);
                continue;
            }

            var quadraticSegment = segment as QuadraticBezierSegment;

            if (quadraticSegment != null)
            {
                ctx.QuadraticBezierTo(quadraticSegment.Point1, quadraticSegment.Point2, quadraticSegment.IsStroked, quadraticSegment.IsSmoothJoin);
                continue;
            }

            var polylineSegment = segment as PolyLineSegment;

            if (polylineSegment != null)
            {
                ctx.PolyLineTo(polylineSegment.Points, polylineSegment.IsStroked, polylineSegment.IsSmoothJoin);
                continue;
            }

            var polyBezierSegment = segment as PolyBezierSegment;

            if (polyBezierSegment != null)
            {
                ctx.PolyBezierTo(polyBezierSegment.Points, polyBezierSegment.IsStroked, polyBezierSegment.IsSmoothJoin);
                continue;
            }

            var polyQuadraticSegment = segment as PolyQuadraticBezierSegment;

            if (polyQuadraticSegment != null)
            {
                ctx.PolyQuadraticBezierTo(polyQuadraticSegment.Points, polyQuadraticSegment.IsStroked, polyQuadraticSegment.IsSmoothJoin);
                continue;
            }

            var arcSegment = segment as ArcSegment;

            if (arcSegment != null)
            {
                ctx.ArcTo(arcSegment.Point, arcSegment.Size, arcSegment.RotationAngle, arcSegment.IsLargeArc, arcSegment.SweepDirection, arcSegment.IsStroked, arcSegment.IsSmoothJoin);
                continue;
            }
        }
    }

    public static DrawingVisual? AsDrawingVisual(this Geometry<Point> geometry, VisualParameters visualParameters, int imageWidth, int imageHeight, BoundingBox? mapBoundary = null)
    {
        if (geometry.IsNullOrEmpty())
            return null;

        if (imageWidth <= 0 || imageHeight <= 0)
            return null;

        BoundingBox mapExtent = mapBoundary ?? geometry.GetBoundingBox();

        double xScale = imageWidth / mapExtent.Width;
        double yScale = imageHeight / mapExtent.Height;
        double scale = xScale > yScale ? yScale : xScale;

        var mapToScreen = new Func<Point, Point>(p => new Point() { X = (p.X - mapExtent.XMin) * scale, Y = -(p.Y - mapExtent.YMax) * scale });

        var pen = visualParameters.GetWpfPen();

        if (pen is not null)
        {
            pen.LineJoin = PenLineJoin.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.StartLineCap = PenLineCap.Round;
        }

        Brush brush = visualParameters.Fill;

        var drawingVisuals = new DrawingVisualRenderStrategy([new SimpleSymbolizer(visualParameters)])
                                            .AsDrawingVisual([geometry.Transform(mapToScreen, geometry.Srid).AsFeature()], 0);

        var drawingVisual = drawingVisuals.First();

        drawingVisual.Opacity = visualParameters.Opacity;

        return drawingVisual;
    }

    /// <summary>
    /// Convert to drawing visual based on Google Zoom Level
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="geometry"></param>
    /// <param name="visualParameters"></param>
    /// <param name="googleZoomLevel"></param>
    /// <param name="mapBoundary"></param>
    /// <returns></returns>
    public static DrawingVisual? AsDrawingVisual(this Geometry<Point> geometry, VisualParameters visualParameters, int googleZoomLevel, BoundingBox? mapBoundary = null)
    {
        if (geometry.IsNullOrEmpty())
            return null;

        BoundingBox mapExtent = mapBoundary ?? geometry.GetBoundingBox();

        var screenSize = WebMercatorUtility.ToScreenSize(googleZoomLevel, mapExtent);

        return geometry.AsDrawingVisual(visualParameters, screenSize.Width, screenSize.Height, mapExtent);
    }
}

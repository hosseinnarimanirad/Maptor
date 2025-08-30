using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

using Drawing = System.Drawing;

namespace IRI.Maptor.Jab.Common.Cartography.Helpers;

public class GeometryHelper
{
    static int pointSize = 4;

    internal static void Transform(Drawing.Graphics graphics, Geometry<Point> original, Point location, Drawing.Pen pen, Drawing.Brush brush)
    {
        if (original.Geometries != null)
        {
            foreach (var geometry in original.Geometries)
            {
                Transform(graphics, geometry, location, pen, brush);
            }
        }
        else
        {
            if (original.NumberOfPoints < 1)
                return;

            var firstPoint = original.Points[0];

            if (original.Type == GeometryType.Point)
            {
                graphics.DrawEllipse(pen, (float)(firstPoint.X + location.X), (float)(firstPoint.Y + location.Y), pointSize, pointSize);
            }
            else if (original.Type == GeometryType.LineString)
            {
                AddLineString(graphics, original, location, pen, brush);
            }
        }
    }

    private static void AddLineString(Drawing.Graphics graphics, Geometry<Point> original, Point location, Drawing.Pen pen, Drawing.Brush brush)
    {
        if (original.NumberOfPoints < 1)
            return;

        for (int i = 1; i < original.NumberOfPoints; i++)
        {
            graphics.DrawLine(pen,
                (float)(original.Points[i - 1].X + location.X),
                (float)(original.Points[i - 1].Y + location.Y),
                (float)(original.Points[i].X + location.X),
                (float)(original.Points[i].Y + location.Y));
        }
    }


    internal static void Transform(WriteableBitmap context, Geometry<Point> original, Point location, int border, int fill)
    {
        if (original.Geometries != null)
        {
            foreach (var geometry in original.Geometries)
            {
                Transform(context, geometry, location, border, fill);
            }
        }
        else
        {
            if (original.NumberOfPoints < 1)
                return;

            var firstPoint = original.Points[0];

            if (original.Type == GeometryType.Point)
            {
                context.DrawEllipseCentered(border, (int)(firstPoint.X + location.X), (int)(firstPoint.Y + location.Y), pointSize, pointSize);
            }
            else if (original.Type == GeometryType.LineString)
            {
                AddLineString(context, original, location, border, fill);
            }
        }
    }

    private static void AddLineString(WriteableBitmap context, Geometry<Point> original, Point location, int border, int fill)
    {
        if (original.NumberOfPoints < 1)
            return;

        for (int i = 1; i < original.NumberOfPoints; i++)
        {
            context.DrawLine(
                        (int)(original.Points[i - 1].X + location.X),
                        (int)(original.Points[i - 1].Y + location.Y),
                        (int)(original.Points[i].X + location.X),
                        (int)(original.Points[i].Y + location.Y),
                        border);
        }
    }


    internal static void Transform(Drawing.Graphics graphics, Geometry original, Point location, Drawing.Pen pen, Drawing.Brush brush)
    {
        var geometry = original.GetFlattenedPathGeometry();

        foreach (var figure in geometry.Figures)
        {
            System.Windows.Point firstLocalPoint = ((PolyLineSegment)figure.Segments[0]).Points[0];

            var firstPoint = new Drawing.PointF((float)(firstLocalPoint.X + location.X), (float)(firstLocalPoint.Y + location.Y));

            foreach (var segment in figure.Segments)
            {
                if (segment is PolyLineSegment)
                {
                    var points = ((PolyLineSegment)segment).Points.Select(i => new Drawing.PointF((float)(i.X + location.X), (float)(i.Y + location.Y))).ToList();

                    points.Add(firstPoint);

                    graphics.DrawLines(pen, points.ToArray());
                }
                else if (segment is LineSegment)
                {
                    var x2 = (float)(((LineSegment)segment).Point.X + location.X);

                    var y2 = (float)(((LineSegment)segment).Point.Y + location.Y);

                    graphics.DrawLine(pen, firstPoint.X, firstPoint.Y, x2, y2);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

    }

    internal static void Transform(WriteableBitmap context, Geometry original, Point location, int border, int fill)
    {
        var geometry = original.GetFlattenedPathGeometry();

        foreach (var figure in geometry.Figures)
        {
            System.Windows.Point firstLocalPoint = ((PolyLineSegment)figure.Segments[0]).Points[0];

            var firstPoint = new Point(firstLocalPoint.X + location.X, firstLocalPoint.Y + location.Y);

            foreach (var segment in figure.Segments)
            {
                if (segment is PolyLineSegment)
                {
                    var points = ((PolyLineSegment)segment).Points.Select(i => new Point(i.X + location.X, i.Y + location.Y)).ToList();

                    points.Insert(0, firstPoint);

                    AddLineString(context, points, border, fill);
                }
                else if (segment is LineSegment)
                {
                    var x2 = ((LineSegment)segment).Point.X + location.X;

                    var y2 = ((LineSegment)segment).Point.Y + location.Y;

                    context.DrawLine((int)firstPoint.X, (int)firstPoint.Y, (int)x2, (int)y2, border);
                }
                else
                {
                    throw new NotImplementedException();
                }

            }
        }

    }

    private static void AddLineString(WriteableBitmap context, List<Point> points, int border, int fill)
    {
        if (points.Count < 1)
            return;

        for (int i = 1; i < points.Count; i++)
        {
            context.DrawLine((int)points[i - 1].X, (int)points[i - 1].Y, (int)points[i].X, (int)points[i].Y, border);
        }
    }
}

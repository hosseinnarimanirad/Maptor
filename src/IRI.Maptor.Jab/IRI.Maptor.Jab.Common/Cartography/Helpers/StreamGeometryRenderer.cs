using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using WpfPoint = System.Windows.Point;

namespace IRI.Maptor.Jab.Common.Cartography.Helpers;

public static class StreamGeometryRenderer
{
    const double pointSize = 4;

    //static Pen pen = new Pen(new SolidColorBrush(Colors.Black), 0);

    #region Geometry to StreamGeometry

    public static StreamGeometry ParseSqlGeometry<T>(List<Feature<T>> features, /*Func<WpfPoint, WpfPoint> transform,*/ Geometry? pointSymbol = null)
        where T : IPoint, new()
    {
        StreamGeometry result = new StreamGeometry();

        result.FillRule = FillRule.Nonzero;

        if (features.IsNullOrEmpty())
            return result;

        int p = 0;


        using (StreamGeometryContext context = result.Open())
        {
            foreach (var item in features)
            {
                p += AddGeometry(context, item.TheGeometry, /*transform,*/ pointSymbol);
            }
        }
        //result.Freeze();

        return result;
    }

    private static int AddGeometry<T>(StreamGeometryContext context, Geometry<T> geometry, /*Func<WpfPoint, WpfPoint> transform,*/ Geometry? pointSymbol)
        where T : IPoint, new()
    {
        if (geometry.IsNotValidOrEmpty())
            return 1;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                if (pointSymbol != null)
                {
                    AddPoint(context, geometry, pointSymbol/*, transform*/);
                }
                else
                {
                    AddPoint(context, geometry/*, transform*/);
                }
                break;

            case GeometryType.LineString:
                AddLineString(context, geometry, /*transform, */false);
                break;

            case GeometryType.Polygon:
                AddPolygon(context, geometry/*, transform*/);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(context, geometry/*, transform*/);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(context, geometry/*, transform*/);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(context, geometry/*, transform*/);
                break;

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                break;
        }


        return 0;
    }

    private static void AddLineString<T>(StreamGeometryContext context, Geometry<T> lineString, /*Func<WpfPoint, WpfPoint> transform,*/ bool isClosed)
        where T : IPoint, new()
    {
        if (lineString.IsNullOrEmpty())
            return;

        int numberOfPoints = lineString.NumberOfPoints;

        context.BeginFigure(/*transform*/lineString.Points[0].AsWpfPoint(), isFilled: true, isClosed: isClosed);

        for (int i = 1; i < numberOfPoints; i++)
        {
            var point = /*transform*/lineString.Points[i].AsWpfPoint();

            context.LineTo(point, isStroked: true, isSmoothJoin: false);
        }

    }

    private static void AddMultiLineString<T>(StreamGeometryContext context, Geometry<T> multiLineString/*, Func<WpfPoint, WpfPoint> transform*/)
        where T : IPoint, new()
    {
        int numberOfLineStrings = multiLineString.NumberOfGeometries;

        for (int i = 0; i < numberOfLineStrings; i++)
        {
            var lineString = multiLineString.Geometries[i];

            AddLineString(context, lineString, /*transform, */false);
        }
    }

    private static void AddPolygon<T>(StreamGeometryContext context, Geometry<T> polygon/*, Func<WpfPoint, WpfPoint> transform*/)
        where T : IPoint, new()
    {
        //int numberOfInteriorRings = polygon.STNumInteriorRing().Value;
        //AddLineString(context, polygon.STExteriorRing(), transform, true);
        //for (int i = 1; i <= numberOfInteriorRings; i++)
        //{
        //    AddLineString(context, polygon.STInteriorRingN(i), transform, true);
        //}

        for (int i = 0; i < polygon.NumberOfGeometries; i++)
        {
            AddLineString(context, polygon.Geometries[i], /*transform, */true);
        }

    }

    private static void AddMultiPolygon<T>(StreamGeometryContext context, Geometry<T> multiPolygon/*, Func<WpfPoint, WpfPoint> transform*/)
        where T : IPoint, new()
    {
        int numberOfPolygons = multiPolygon.NumberOfGeometries;

        for (int i = 0; i < numberOfPolygons; i++)
        {
            Geometry<T> polygon = multiPolygon.Geometries[i];

            AddPolygon(context, polygon/*, transform*/);
        }
    }

    private static void AddPoint<T>(StreamGeometryContext context, Geometry<T> point/*, Func<WpfPoint, WpfPoint> transform*/)
        where T : IPoint, new()
    {
        var center = /*transform*/point.AsWpfPoint();

        context.DrawGeometry(new EllipseGeometry(new Rect(center.X - pointSize / 2.0, center.Y - pointSize / 2.0, pointSize, pointSize)));
        //context.DrawGeometry(new EllipseGeometry(transform(new Point(point.STX.Value, point.STY.Value)), pointSize, pointSize));
    }

    // todo: why not using DrawGeometry or passing PathGeometry
    // in every call pointSymbol.GetFlattenedPathGeometry() is called
    private static void AddPoint<T>(StreamGeometryContext context, Geometry<T> point, Geometry pointSymbol/*, Func<WpfPoint, WpfPoint> transform*/)
        where T : IPoint, new()
    {
        var location = /*transform*/point.AsWpfPoint();

        var geometry = pointSymbol.GetFlattenedPathGeometry();

        //context.DrawGeometry(pointSymbol);

        foreach (var figure in geometry.Figures)
        {
            var firstPoint = ((PolyLineSegment)figure.Segments[0]).Points[0];

            context.BeginFigure(new WpfPoint(firstPoint.X + location.X, firstPoint.Y + location.Y), figure.IsFilled, figure.IsClosed);

            foreach (var segment in figure.Segments)
            {
                context.PolyLineTo(((PolyLineSegment)segment).Points.Select(i => new WpfPoint(i.X + location.X, i.Y + location.Y)).ToList(), segment.IsStroked, segment.IsSmoothJoin);
            }
        }
    }

    private static void AddMultiPoint<T>(StreamGeometryContext context, Geometry<T> multiPoint/*, Func<WpfPoint, WpfPoint> transform*/)
        where T : IPoint, new()
    {
        int numberOfPoints = multiPoint.NumberOfGeometries;

        for (int i = 0; i < numberOfPoints; i++)
        {
            Geometry<T> point = multiPoint.Geometries[i];

            AddPoint(context, point/*, transform*/);
        }
    }

    #endregion

    internal static StreamGeometry Transform(Geometry original, WpfPoint location)
    {
        StreamGeometry result = new StreamGeometry();

        using (StreamGeometryContext context = result.Open())
        {
            var geometry = original.GetFlattenedPathGeometry();

            foreach (var figure in geometry.Figures)
            {
                var firstPoint = ((PolyLineSegment)figure.Segments[0]).Points[0];

                context.BeginFigure(new WpfPoint(firstPoint.X + location.X, firstPoint.Y + location.Y), figure.IsFilled, figure.IsClosed);

                foreach (var segment in figure.Segments)
                {
                    if (segment is PolyLineSegment)
                    {
                        context.PolyLineTo(((PolyLineSegment)segment).Points.Select(i => new WpfPoint(i.X + location.X, i.Y + location.Y)).ToList(), segment.IsStroked, segment.IsSmoothJoin);
                    }
                    else if (segment is LineSegment)
                    {
                        context.LineTo(
                            new WpfPoint(
                                ((LineSegment)segment).Point.X + location.X,
                                ((LineSegment)segment).Point.Y + location.Y), segment.IsStroked, segment.IsSmoothJoin);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                }
            }
        }

        return result;
    }


    #region SqlGeometry to StreamGeometry 

    //public static StreamGeometry ParseSqlGeometry(List<SqlGeometry> geometries, Func<WpfPoint, WpfPoint> transform, Geometry pointSymbol = null)
    //{
    //    StreamGeometry result = new StreamGeometry();

    //    result.FillRule = FillRule.Nonzero;

    //    int p = 0;

    //    if (geometries != null)
    //    {
    //        using (StreamGeometryContext context = result.Open())
    //        {
    //            foreach (SqlGeometry item in geometries)
    //            {
    //                p += AddGeometry(context, item, transform, pointSymbol);
    //            }
    //        }
    //    }

    //    //result.Freeze();

    //    return result;
    //}

    //private static int AddGeometry(StreamGeometryContext context, SqlGeometry geometry, Func<WpfPoint, WpfPoint> transform, Geometry pointSymbol)
    //{
    //    if (geometry.IsNotValidOrEmpty())
    //        return 1;

    //    var type = geometry.GetOpenGisType();

    //    switch (type)
    //    {
    //        case OpenGisGeometryType.Point:
    //            if (pointSymbol != null)
    //            {
    //                AddPoint(context, geometry, pointSymbol, transform);
    //            }
    //            else
    //            {
    //                AddPoint(context, geometry, transform);
    //            }
    //            break;

    //        case OpenGisGeometryType.LineString:
    //            AddLineString(context, geometry, transform, false);
    //            break;

    //        case OpenGisGeometryType.Polygon:
    //            AddPolygon(context, geometry, transform);
    //            break;

    //        case OpenGisGeometryType.MultiPoint:
    //            AddMultiPoint(context, geometry, transform);
    //            break;

    //        case OpenGisGeometryType.MultiLineString:
    //            AddMultiLineString(context, geometry, transform);
    //            break;

    //        case OpenGisGeometryType.MultiPolygon:
    //            AddMultiPolygon(context, geometry, transform);
    //            break;

    //        case OpenGisGeometryType.GeometryCollection:
    //        case OpenGisGeometryType.CircularString:
    //        case OpenGisGeometryType.CompoundCurve:
    //        case OpenGisGeometryType.CurvePolygon:
    //        default:
    //            break;
    //    }


    //    return 0;
    //}

    //private static void AddLineString(StreamGeometryContext context, SqlGeometry lineString, Func<WpfPoint, WpfPoint> transform, bool isClosed)
    //{
    //    if (lineString.IsNull)
    //        return;

    //    int numberOfPoints = lineString.STNumPoints().Value;

    //    //context.BeginFigure(transform(
    //    //    new Point(lineString.STStartPoint().STX.Value,
    //    //                                lineString.STStartPoint().STY.Value)).AsWpfPoint(),

    //    context.BeginFigure(transform(lineString.STStartPoint().AsWpfPoint()), isFilled: true, isClosed: isClosed);

    //    //STPointN(index): index is between 1 and number of points
    //    for (int i = 2; i <= numberOfPoints; i++)
    //    {
    //        var point = transform(lineString.STPointN(i).AsWpfPoint());

    //        context.LineTo(point, isStroked: true, isSmoothJoin: false);
    //    }

    //}

    //private static void AddMultiLineString(StreamGeometryContext context, SqlGeometry multiLineString, Func<WpfPoint, WpfPoint> transform)
    //{
    //    int numberOfLineStrings = multiLineString.STNumGeometries().Value;

    //    for (int i = 1; i <= numberOfLineStrings; i++)
    //    {
    //        SqlGeometry lineString = multiLineString.STGeometryN(i);

    //        AddLineString(context, lineString, transform, false);
    //    }
    //}

    //private static void AddPolygon(StreamGeometryContext context, SqlGeometry polygon, Func<WpfPoint, WpfPoint> transform)
    //{
    //    int numberOfInteriorRings = polygon.STNumInteriorRing().Value;

    //    AddLineString(context, polygon.STExteriorRing(), transform, true);

    //    for (int i = 1; i <= numberOfInteriorRings; i++)
    //    {
    //        AddLineString(context, polygon.STInteriorRingN(i), transform, true);
    //    }

    //}

    //private static void AddMultiPolygon(StreamGeometryContext context, SqlGeometry multiPolygon, Func<WpfPoint, WpfPoint> transform)
    //{
    //    int numberOfPolygons = multiPolygon.STNumGeometries().Value;

    //    for (int i = 1; i <= numberOfPolygons; i++)
    //    {
    //        SqlGeometry polygon = multiPolygon.STGeometryN(i);

    //        AddPolygon(context, polygon, transform);
    //    }
    //}

    //private static void AddPoint(StreamGeometryContext context, SqlGeometry point, Func<WpfPoint, WpfPoint> transform)
    //{
    //    var center = transform(point.AsWpfPoint());

    //    context.DrawGeometry(new EllipseGeometry(new Rect(center.X - pointSize / 2.0, center.Y - pointSize / 2.0, pointSize, pointSize)));
    //    //context.DrawGeometry(new EllipseGeometry(transform(new Point(point.STX.Value, point.STY.Value)), pointSize, pointSize));
    //}

    //private static void AddPoint(StreamGeometryContext context, SqlGeometry point, Geometry pointSymbol, Func<WpfPoint, WpfPoint> transform)
    //{
    //    var location = transform(point.AsWpfPoint());

    //    var geometry = pointSymbol.GetFlattenedPathGeometry();

    //    foreach (var figure in geometry.Figures)
    //    {
    //        var firstPoint = ((PolyLineSegment)figure.Segments[0]).Points[0];

    //        context.BeginFigure(new WpfPoint(firstPoint.X + location.X, firstPoint.Y + location.Y), figure.IsFilled, figure.IsClosed);

    //        foreach (var segment in figure.Segments)
    //        {
    //            context.PolyLineTo(((PolyLineSegment)segment).Points.Select(i => new WpfPoint(i.X + location.X, i.Y + location.Y)).ToList(), segment.IsStroked, segment.IsSmoothJoin);
    //        }
    //    }
    //}

    //private static void AddMultiPoint(StreamGeometryContext context, SqlGeometry multiPoint, Func<WpfPoint, WpfPoint> transform)
    //{
    //    int numberOfPoints = multiPoint.STNumGeometries().Value;

    //    for (int i = 1; i <= numberOfPoints; i++)
    //    {
    //        SqlGeometry point = multiPoint.STGeometryN(i);

    //        AddPoint(context, point, transform);
    //    }
    //}

    #endregion
}

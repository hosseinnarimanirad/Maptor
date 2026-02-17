using System;
using System.Linq;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Cartography.Helpers;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Sta.Common.Enums;


namespace IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;

public class WriteableBitmapRenderStrategy : RenderStrategy
{
    int pointSize = 4;

    public WriteableBitmapRenderStrategy(IEnumerable<ISymbolizer> symbolizer) : base(symbolizer)
    {
    }

    public override ImageBrush? Render(List<Feature<Point>> features, double mapScale, double screenWidth, double screenHeight)
    {
        if (features.IsNullOrEmpty())
            return null;

        WriteableBitmap? image = null;

        foreach (var symbolizer in _symbolizers)
        {
            // check scale
            if (!symbolizer.IsInScaleRange(mapScale))
                continue;

            // filter features
            var filteredFeatures = features.Where(symbolizer.IsFilterPassed).ToList();

            switch (symbolizer)
            {
                case SimplePointSymbolizer simplePointSymbolizer:
                    break;

                case SimpleSymbolizer simpleSymbolizer:
                    image = ParseSqlGeometry(
                        filteredFeatures,
                        //mapToScreen,
                        (int)screenWidth,
                        (int)screenHeight,
                        simpleSymbolizer.Param.Stroke.AsSolidColor().Value,
                        simpleSymbolizer.Param.Fill.AsSolidColor().Value);

                    break;

                case LabelSymbolizer labelSymbolizer:
                    if (labelSymbolizer.Param?.IsInScaleRangeAndSelected(1.0 / mapScale) == true)
                    {
                        //this.DrawLabel(labels, geometries, image, transform);
                    }
                    break;

                default:
                    break;
            }
        }

        if (image is null)
            return null;

        image.Freeze();

        return new ImageBrush(image);
    }


    private WriteableBitmap ParseSqlGeometry(List<Feature<Point>> features, int width, int height, Color border, Color fill, ImageSource? pointSymbol = null, Geometry<Point>? symbol = null)
    {

        WriteableBitmap result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);

        if (features.IsNullOrEmpty())
            return result;


        int intBorderColor = WriteableBitmapExtensions.ConvertColor(border);

        int intFillColor = WriteableBitmapExtensions.ConvertColor(fill);

        using (result.GetBitmapContext())
        {
            foreach (var item in features)
            {
                AddGeometry(result, item.TheGeometry, intBorderColor, intFillColor, pointSymbol, symbol);
            }
        }

        //result.Freeze();

        return result;
    }

    private int AddGeometry(WriteableBitmap context, Geometry<Point> geometry, int border, int fill, ImageSource? imageSymbol, Geometry<Point>? geometrySymbol)
    {
        if (geometry.IsNotValidOrEmpty())
        {
            return 1;
        }

        switch (geometry.Type)
        {
            case GeometryType.Point:
                AddPoint(context, geometry, border, fill, imageSymbol, geometrySymbol);
                break;

            case GeometryType.LineString:
                AddLineString(context, geometry, border, fill);
                break;

            case GeometryType.Polygon:
                AddPolygon(context, geometry, border, fill);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(context, geometry, border, fill, imageSymbol, geometrySymbol);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(context, geometry, border, fill);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(context, geometry, border, fill);
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

    private void AddPoint(WriteableBitmap context, Geometry<Point> point, int border, int fill, ImageSource? imageSymbol, Geometry<Point>? geometrySymbol)
    {
        //var center = transform(point.AsWpfPoint()).AsPoint();
        var center = point.AsPoint();

        if (geometrySymbol != null)
        {
            GeometryHelper.Transform(context, geometrySymbol, center, border, fill);
        }
        else if (imageSymbol != null)
        {
            throw new NotImplementedException();
        }
        else
        {
            context.DrawEllipseCentered((int)center.X, (int)center.Y, pointSize, pointSize, border);
        }
    }

    private void AddMultiPoint(WriteableBitmap context, Geometry<Point> multiPoint, int border, int fill, ImageSource? imageSymbol, Geometry<Point>? geometrySymbol)
    {
        int numberOfPoints = multiPoint.NumberOfGeometries;

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = multiPoint.Geometries[i];

            if (point.IsNotValidOrEmpty())
                continue;

            AddPoint(context, point, border, fill, imageSymbol, geometrySymbol);
        }
    }

    private void AddLineString(WriteableBitmap context, Geometry<Point> lineString, int border, int fill)
    {
        int numberOfPoints = lineString.NumberOfPoints;

        int[] points = new int[2 * numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = lineString.Points[i].AsWpfPoint();

            points[2 * i] = (int)point.X;

            points[2 * i + 1] = (int)point.Y;
        }

        //context.DrawPolyline(points, intColor);
        //if (border.HasValue)
        //{
        context.DrawPolylineAa(points, border);
        //}
    }

    private void AddMultiLineString(WriteableBitmap context, Geometry<Point> multiLineString, int border, int fill)
    {
        int numberOfLineStrings = multiLineString.NumberOfGeometries;

        for (int i = 0; i < numberOfLineStrings; i++)
        {
            var lineString = multiLineString.Geometries[i];

            AddLineString(context, lineString, border, fill);
        }
    }

    private void AddPolygon(WriteableBitmap context, Geometry<Point> polygon, int border, int fill)
    {
        int numberOfInteriorRings = polygon.NumberOfGeometries;

        for (int i = 0; i < numberOfInteriorRings; i++)
        {
            var ring = polygon.Geometries[i];

            AddPolygonRing(context, ring, border, fill);
        }
    }

    private void AddPolygonRing(WriteableBitmap context, Geometry<Point> polygon, int border, int fill)
    {
        int numberOfPoints = polygon.NumberOfPoints;

        int[] points = new int[2 * numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = polygon.Points[i].AsWpfPoint();

            points[2 * i] = (int)point.X;

            points[2 * i + 1] = (int)point.Y;
        }

        context.DrawPolylineAa(points, border);

        context.FillPolygon(points, fill);
    }

    private void AddMultiPolygon(WriteableBitmap context, Geometry<Point> multiPolygon, int border, int fill)
    {
        int numberOfLineStrings = multiPolygon.NumberOfGeometries;

        for (int i = 0; i < numberOfLineStrings; i++)
        {
            var polygon = multiPolygon.Geometries[i];

            AddPolygon(context, polygon, border, fill);
        }
    }

}

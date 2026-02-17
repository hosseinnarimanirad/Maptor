using System;
using System.Linq;
using System.IO;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Cartography.Helpers;

using Drawing = System.Drawing;
using Point = IRI.Maptor.Sta.Common.Primitives.Point;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;

public class GdiBitmapRenderStrategy : RenderStrategy
{
    static readonly Drawing.SolidBrush _labelBackground = new Drawing.SolidBrush(Drawing.Color.FromArgb(150, 255, 255, 255));

    public GdiBitmapRenderStrategy(IEnumerable<ISymbolizer> symbolizer) : base(symbolizer)
    {
    }

    public override ImageBrush? Render(IEnumerable<Feature<Point>> features, double mapScale, double screenWidth, double screenHeight)
    {
        var bitmap = AsGdiBitmap(features, mapScale, screenWidth, screenHeight);

        if (bitmap is null)
            return null;

        BitmapImage image = ImageUtility.CreateBitmapImage(bitmap, Drawing.Imaging.ImageFormat.Png);

        bitmap.Dispose();

        image.Freeze();

        return new ImageBrush(image);
    }

    public Drawing.Bitmap? AsGdiBitmap(IEnumerable<Feature<Point>> features, double mapScale, double imageWidth, double imageHeight)
    {
        if (features.IsNullOrEmpty())
            return null;

        Drawing.Bitmap image = new Drawing.Bitmap((int)imageWidth, (int)imageHeight);

        using (Drawing.Graphics graphics = Drawing.Graphics.FromImage(image))
        {
            foreach (var symbolizer in _symbolizers)
            {
                // check scale
                if (!symbolizer.IsInScaleRange(mapScale))
                    continue;

                // filter features
                var filteredFeatures = features.Where(symbolizer.IsFilterPassed).ToList();

                if (filteredFeatures.IsNullOrEmpty())
                    continue;

                switch (symbolizer)
                {
                    case SimplePointSymbolizer simplePointSymbolizer:
                        break;

                    case SimpleSymbolizer simpleSymbolizer:

                        Render(
                            graphics,
                            filteredFeatures,
                            simpleSymbolizer.Param.GetGdiPlusPen(),
                            simpleSymbolizer.Param.Fill.AsGdiBrush(),
                            simpleSymbolizer.Param.PointSymbol);

                        if (image is null)
                            return null;

                        break;

                    case LabelSymbolizer labelSymbolizer:
                        if (labelSymbolizer.Param?.IsInScaleRangeAndSelected(1.0 / mapScale) == true)
                        {
                            DrawLabels(filteredFeatures, graphics, labelSymbolizer.Param, labelSymbolizer.LabelAttribute);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        return image;
    }


    #region Private Methods

    private void Render(Drawing.Graphics graphics, IEnumerable<Feature<Point>> features, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol)
    {
        if (features.IsNullOrEmpty())
            return;

        foreach (var item in features)
        {
            AddGeometry(graphics, item.TheGeometry, pen, brush, pointSymbol);
        }
    }

    private int AddGeometry(Drawing.Graphics graphics, Geometry<Point> geometry, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol)
    {
        if (geometry.IsNotValidOrEmpty())
            return 1;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                AddPoint(graphics, geometry, pen, brush, pointSymbol);
                break;

            case GeometryType.LineString:
                AddLineString(graphics, geometry, pen, brush);
                break;

            case GeometryType.Polygon:
                AddPolygon(graphics, geometry, pen, brush);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(graphics, geometry, pen, brush, pointSymbol);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(graphics, geometry, pen, brush);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(graphics, geometry, pen, brush);
                break;

            case GeometryType.GeometryCollection:
                AddGeometryCollection(graphics, geometry, pen, brush, pointSymbol);
                break;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
        return 0;
    }

    private void AddPoint(Drawing.Graphics graphics, Geometry<Point> point, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol)
    {
        //pointSymbol?.EnsureIconLoaded();

        var parsedPoint = point.AsWpfPoint().AsPoint();

        if (pointSymbol?.GeometrySymbol != null)
        {
            GeometryHelper.Transform(graphics, pointSymbol.GeometrySymbol, parsedPoint, pen, brush);
        }
        else if (pointSymbol?.ImageSymbolGdiPlus != null)
        {
            graphics.DrawImage(pointSymbol.ImageSymbolGdiPlus, new Drawing.RectangleF((float)(parsedPoint.X - pointSymbol.SymbolWidth / 2.0), (float)(parsedPoint.Y - pointSymbol.SymbolHeight), (float)pointSymbol.SymbolWidth, (float)pointSymbol.SymbolHeight));
        }
        else
        {
            if (pen != null)
            {
                graphics.DrawEllipse(pen, (float)(parsedPoint.X - pointSymbol.SymbolWidth / 2.0), (float)(parsedPoint.Y - pointSymbol.SymbolHeight / 2.0), (float)pointSymbol.SymbolWidth, (float)pointSymbol.SymbolHeight);
            }
            if (brush != null)
            {
                graphics.FillEllipse(brush, (float)(parsedPoint.X - pointSymbol.SymbolWidth / 2.0), (float)(parsedPoint.Y - pointSymbol.SymbolHeight / 2.0), (float)pointSymbol.SymbolWidth, (float)pointSymbol.SymbolHeight);
            }
        }
    }

    private void AddMultiPoint(Drawing.Graphics graphics, Geometry<Point> multiPoint, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol)//, ImageSource pointSymbol, Geometry symbol)
    {
        int numberOfPoints = multiPoint.NumberOfGeometries;

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = multiPoint.Geometries[i];

            if (point.IsNotValidOrEmpty())
                continue;

            AddPoint(graphics, point, pen, brush, pointSymbol);
        }
    }

    private void AddLineString(Drawing.Graphics graphics, Geometry<Point> lineString, Drawing.Pen pen, Drawing.Brush brush)
    {
        int numberOfPoints = lineString.NumberOfPoints;

        Drawing.PointF[] points = new Drawing.PointF[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            var parsedPoint = lineString.Points[i].AsWpfPoint();

            points[i] = new Drawing.PointF((float)parsedPoint.X, (float)parsedPoint.Y);
        }

        graphics.DrawLines(pen, points);
    }

    private void AddMultiLineString(Drawing.Graphics graphics, Geometry<Point> multiLineString, Drawing.Pen pen, Drawing.Brush brush)
    {
        int numberOfLineStrings = multiLineString.NumberOfGeometries;

        for (int i = 0; i < numberOfLineStrings; i++)
        {
            var lineString = multiLineString.Geometries[i];

            if (lineString.IsNotValidOrEmpty())
                continue;

            AddLineString(graphics, lineString, pen, brush);
        }
    }

    private void AddPolygonRing(Drawing.Graphics graphics, Geometry<Point> ring, Drawing.Pen pen, Drawing.Brush brush)
    {
        int numberOfPoints = ring.NumberOfPoints;

        Drawing.PointF[] points = new Drawing.PointF[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            var parsedPoint = ring.Points[i].AsWpfPoint();

            points[i] = new Drawing.PointF((float)parsedPoint.X, (float)parsedPoint.Y);
        }

        if (pen != null)
        {
            graphics.DrawPolygon(pen, points);
        }

        if (brush != null)
        {
            graphics.FillPolygon(brush, points);
        }
    }

    private void AddPolygon(Drawing.Graphics graphics, Geometry<Point> polygon, Drawing.Pen pen, Drawing.Brush brush)
    {
        int numberOfRings = polygon.NumberOfGeometries;

        for (int i = 0; i < numberOfRings; i++)
        {
            var ring = polygon.Geometries[i];

            AddPolygonRing(graphics, ring, pen, brush);
        }
    }

    private void AddMultiPolygon(Drawing.Graphics graphics, Geometry<Point> multiPolygon, Drawing.Pen pen, Drawing.Brush brush)
    {
        int numberOfPolygons = multiPolygon.NumberOfGeometries;

        for (int i = 0; i < numberOfPolygons; i++)
        {
            var polygon = multiPolygon.Geometries[i];

            if (polygon.IsNotValidOrEmpty())
                continue;

            AddPolygon(graphics, polygon, pen, brush);
        }
    }

    private void AddGeometryCollection(Drawing.Graphics graphics, Geometry<Point> multiPolygon, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol)
    {
        int numberOfPolygons = multiPolygon.NumberOfGeometries;

        for (int i = 0; i < numberOfPolygons; i++)
        {
            var polygon = multiPolygon.Geometries[i];

            if (polygon.IsNotValidOrEmpty())
                continue;

            AddGeometry(graphics, polygon, pen, brush, pointSymbol);
        }
    }

    private void DrawLabels(IEnumerable<Feature<Point>> features, Drawing.Graphics graphic, VisualParameters labelParameters, string? labelAttribute = null)
    {
        var featureList = features.ToList();
        if (featureList.IsNullOrEmpty())
            return;

        var mapCoordinates = featureList.Select(g => labelParameters.PositionFunc(g.TheGeometry).AsWpfPoint()).ToList();

        var font = new Drawing.Font(labelParameters.FontFamily.FamilyNames.First().Value, labelParameters.FontSize, Drawing.FontStyle.Bold);

        graphic.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;

        graphic.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        graphic.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        graphic.TextRenderingHint = Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        var brush = labelParameters.Foreground.AsGdiBrush();

        Drawing.StringFormat format = new Drawing.StringFormat();

        if (labelParameters.IsRtl)
        {
            format.FormatFlags = Drawing.StringFormatFlags.DirectionRightToLeft;
        }

        for (int i = 0; i < featureList.Count; i++)
        {
            var location = mapCoordinates[i];

            var labelValue = (string.IsNullOrEmpty(labelAttribute) ? featureList[i]?.Label : featureList[i]?.Attributes[labelAttribute]?.ToString()) ?? string.Empty;

            var stringSize = graphic.MeasureString(/*features[i].Label*/labelValue, font);

            Drawing.PointF locationF = new Drawing.PointF((float)(location.X - stringSize.Width / 2.0), (float)(location.Y - stringSize.Height / 2.0));

            var rectangleF = labelParameters.IsRtl ?
                 new Drawing.RectangleF(locationF.X - stringSize.Width, locationF.Y, stringSize.Width, stringSize.Height) :
                 new Drawing.RectangleF(locationF.X, locationF.Y, stringSize.Width, stringSize.Height);

            graphic.FillRectangle(_labelBackground, rectangleF);

            graphic.DrawString(/*features[i].Label*/labelValue, font, brush, locationF, format);
        }

        graphic.Flush();

        graphic.Dispose();
        brush.Dispose();
    }

    //internal Drawing.Bitmap ParseSqlGeometry(
    //  List<Feature<Point>> features,
    //  double width,
    //  double height,
    //  Func<Feature<Point>, VisualParameters> symbologyRule)
    //{
    //    var result = new Drawing.Bitmap((int)width, (int)height);
    //    Drawing.Graphics graphics = Drawing.Graphics.FromImage(result);
    //    if (features != null)
    //    {
    //        foreach (var item in features)
    //        {
    //            if (item.TheGeometry is null)
    //                continue;
    //            var symbology = symbologyRule(item);
    //            var pen = symbology.GetGdiPlusPen(symbology.Opacity);
    //            var brush = symbology.GetGdiPlusFillBrush(symbology.Opacity);
    //            AddGeometry(graphics, item.TheGeometry, pen, brush, symbology.PointSymbol);
    //        }
    //    }
    //    return result;
    //}

    #endregion
}

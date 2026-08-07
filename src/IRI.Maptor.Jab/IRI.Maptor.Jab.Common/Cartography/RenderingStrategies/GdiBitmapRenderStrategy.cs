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

using Drawing = System.Drawing;
using Point = IRI.Maptor.Sta.Common.Primitives.Point;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Cartography;

public class GdiBitmapRenderStrategy : RenderStrategy
{
    static readonly Drawing.Color _labelBackgroundColor = Drawing.Color.FromArgb(150, 255, 255, 255);

    // Serializes cloning of a symbolizer's shared point-symbol image. See CloneSymbolImage.
    static readonly object _symbolImageCloneLock = new object();

    public GdiBitmapRenderStrategy(IEnumerable<ISymbolizer> symbolizer) : base(symbolizer)
    {
    }

    /// <summary>
    /// Returns a private copy of a symbolizer's point-symbol image for the duration of one render.
    /// <para>
    /// <see cref="SimplePointSymbolizer.ImageSymbolGdiPlus"/> is a single GDI+ image shared by every
    /// feature of the layer. GDI+ objects are not thread safe and rendering runs on thread pool
    /// threads (<see cref="CanRenderOffUiThread"/>), so two concurrent renders of the same layer -
    /// which fast zooming produces constantly - calling DrawImage on it throw
    /// "Object is currently in use elsewhere". Each render draws with its own clone instead.
    /// </para>
    /// <para>
    /// Cloning touches the shared source, so it is the one operation that has to be locked. That
    /// costs one short lock per render rather than one per point, and it is the only place the
    /// shared image is read.
    /// </para>
    /// </summary>
    private static Drawing.Image? CloneSymbolImage(Drawing.Image? sharedSymbolImage)
    {
        if (sharedSymbolImage is null)
            return null;

        lock (_symbolImageCloneLock)
        {
            return (Drawing.Image)sharedSymbolImage.Clone();
        }
    }

    // System.Drawing has no thread affinity, so the whole of Render can run on a
    // thread pool thread as long as the brush it hands back is frozen.
    public override bool CanRenderOffUiThread => true;

    public override ImageBrush? Render(IEnumerable<Feature<Point>> features, double mapScale, double screenWidth, double screenHeight)
    {
        var bitmap = AsGdiBitmap(features, mapScale, screenWidth, screenHeight);

        if (bitmap is null)
            return null;

        BitmapImage image = ImageUtility.CreateBitmapImage(bitmap, Drawing.Imaging.ImageFormat.Png);

        bitmap.Dispose();

        image.Freeze();

        var brush = new ImageBrush(image);

        // without this the brush keeps the affinity of whichever thread created it and
        // cannot be used by the ui thread. Layer opacity and visibility are bound to the
        // Path (BaseLayer.BindWithFrameworkElement), never to the brush, so freezing it
        // costs nothing.
        brush.Freeze();

        return brush;
    }

    public Drawing.Bitmap? AsGdiBitmap(IEnumerable<Feature<Point>> features, double mapScale, double imageWidth, double imageHeight)
    {
        if (features.IsNullOrEmpty())
            return null;

        Drawing.Bitmap image = new Drawing.Bitmap((int)imageWidth, (int)imageHeight);

        using (Drawing.Graphics graphics = Drawing.Graphics.FromImage(image))
        {
            graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;

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
                        {
                            // all three are created per render and disposed here: the pen and the
                            // brush each leaked a gdi handle on every render, and the symbol image
                            // has to be a private clone (see CloneSymbolImage)
                            using var pen = simpleSymbolizer.Param.GetGdiPlusPen();

                            using var fill = simpleSymbolizer.Param.Fill.AsGdiBrush();

                            using var symbolImage = CloneSymbolImage(simpleSymbolizer.Param.PointSymbol?.ImageSymbolGdiPlus);

                            Render(
                                graphics,
                                filteredFeatures,
                                pen,
                                fill,
                                simpleSymbolizer.Param.PointSymbol,
                                symbolImage);
                        }

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

    private void Render(Drawing.Graphics graphics, IEnumerable<Feature<Point>> features, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol, Drawing.Image? symbolImage)
    {
        if (features.IsNullOrEmpty())
            return;

        foreach (var item in features)
        {
            AddGeometry(graphics, item.TheGeometry, pen, brush, pointSymbol, symbolImage);
        }
    }

    private int AddGeometry(Drawing.Graphics graphics, Geometry<Point> geometry, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol, Drawing.Image? symbolImage)
    {
        if (geometry.IsNotValidOrEmpty())
            return 1;

        switch (geometry.Type)
        {
            case GeometryType.Point:
                AddPoint(graphics, geometry, pen, brush, pointSymbol, symbolImage);
                break;

            case GeometryType.LineString:
                AddLineString(graphics, geometry, pen, brush);
                break;

            case GeometryType.Polygon:
                AddPolygon(graphics, geometry, pen, brush);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(graphics, geometry, pen, brush, pointSymbol, symbolImage);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(graphics, geometry, pen, brush);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(graphics, geometry, pen, brush);
                break;

            case GeometryType.GeometryCollection:
                AddGeometryCollection(graphics, geometry, pen, brush, pointSymbol, symbolImage);
                break;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
        return 0;
    }

    private void AddPoint(Drawing.Graphics graphics, Geometry<Point> point, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol, Drawing.Image? symbolImage)
    {
        //pointSymbol?.EnsureIconLoaded();

        var parsedPoint = point.AsWpfPoint().AsPoint();

        if (pointSymbol?.GeometrySymbol != null)
        {
            GeometryHelper.Transform(graphics, pointSymbol.GeometrySymbol, parsedPoint, pen, brush);
        }
        else if (symbolImage != null)
        {
            // the render-local clone, never pointSymbol.ImageSymbolGdiPlus: that one is shared
            // across threads and DrawImage on it is not thread safe
            graphics.DrawImage(symbolImage, new Drawing.RectangleF((float)(parsedPoint.X - pointSymbol.SymbolWidth / 2.0), (float)(parsedPoint.Y - pointSymbol.SymbolHeight), (float)pointSymbol.SymbolWidth, (float)pointSymbol.SymbolHeight));
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

    private void AddMultiPoint(Drawing.Graphics graphics, Geometry<Point> multiPoint, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol, Drawing.Image? symbolImage)//, ImageSource pointSymbol, Geometry symbol)
    {
        int numberOfPoints = multiPoint.NumberOfGeometries;

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = multiPoint.Geometries[i];

            if (point.IsNotValidOrEmpty())
                continue;

            AddPoint(graphics, point, pen, brush, pointSymbol, symbolImage);
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

        // Fill first, then outline — the other way round the fill paints over
        // the inner half of the stroke.
        if (brush != null)
        {
            graphics.FillPolygon(brush, points);
        }

        if (pen != null)
        {
            graphics.DrawPolygon(pen, points);
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

    private void AddGeometryCollection(Drawing.Graphics graphics, Geometry<Point> multiPolygon, Drawing.Pen pen, Drawing.Brush brush, SimplePointSymbolizer pointSymbol, Drawing.Image? symbolImage)
    {
        int numberOfPolygons = multiPolygon.NumberOfGeometries;

        for (int i = 0; i < numberOfPolygons; i++)
        {
            var polygon = multiPolygon.Geometries[i];

            if (polygon.IsNotValidOrEmpty())
                continue;

            AddGeometry(graphics, polygon, pen, brush, pointSymbol, symbolImage);
        }
    }

    private void DrawLabels(IEnumerable<Feature<Point>> features, Drawing.Graphics graphic, VisualParameters labelParameters, string? labelAttribute = null)
    {
        var featureList = features.ToList();
        if (featureList.IsNullOrEmpty())
            return;

        var mapCoordinates = featureList.Select(g => labelParameters.PositionFunc(g.TheGeometry).AsWpfPoint()).ToList();

        graphic.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;

        graphic.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        graphic.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        graphic.TextRenderingHint = Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        // GDI+ objects have no thread safety, and rendering runs on thread pool threads
        // (CanRenderOffUiThread). Every drawing object here must therefore be created per call:
        // the label background used to be a shared static SolidBrush, which threw
        // "Object is currently in use elsewhere" as soon as two layers drew labels at the same
        // time - which zooming does constantly. They are also disposed, which the font and the
        // string format previously were not.
        using var labelBackground = new Drawing.SolidBrush(_labelBackgroundColor);

        using var font = new Drawing.Font(labelParameters.FontFamily.FamilyNames.First().Value, labelParameters.FontSize, Drawing.FontStyle.Bold);

        using var brush = labelParameters.Foreground.AsGdiBrush();

        using var format = new Drawing.StringFormat();

        if (labelParameters.IsRtl)
        {
            format.FormatFlags = Drawing.StringFormatFlags.DirectionRightToLeft;
        }

        for (int i = 0; i < featureList.Count; i++)
        {
            var location = mapCoordinates[i];

            var labelValue = (string.IsNullOrEmpty(labelAttribute) ? featureList[i]?.Label : featureList[i]?.Attributes[labelAttribute]?.ToString()) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(labelValue))
                continue;

            if (labelParameters.IsRtl && double.TryParse(labelValue, out double doubleValue))
                labelValue = labelValue.LatinNumbersToFarsiNumbers();

            var stringSize = graphic.MeasureString(/*features[i].Label*/labelValue, font);

            Drawing.PointF locationF = new Drawing.PointF((float)(location.X - stringSize.Width / 2.0), (float)(location.Y - stringSize.Height / 2.0));

            var rectangleF = labelParameters.IsRtl ?
                 new Drawing.RectangleF(locationF.X - stringSize.Width, locationF.Y, stringSize.Width, stringSize.Height) :
                 new Drawing.RectangleF(locationF.X, locationF.Y, stringSize.Width, stringSize.Height);

            graphic.FillRectangle(labelBackground, rectangleF);

            graphic.DrawString(/*features[i].Label*/labelValue, font, brush, locationF, format);
        }

        graphic.Flush();

        // deliberately not disposed here: AsGdiBitmap owns this Graphics through a using block and
        // keeps drawing the remaining symbolizers on it. Disposing it meant every symbolizer that
        // happened to come after a label one drew on a disposed Graphics.
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

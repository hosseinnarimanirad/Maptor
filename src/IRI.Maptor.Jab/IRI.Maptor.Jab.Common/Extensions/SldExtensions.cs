using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using System.Windows.Media;
using IRI.Maptor.Sta.Ogc;
using MahApps.Metro.IconPacks;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Extensions;

public static class SldExtensions
{
    public static List<ISymbolizer> ParseToSymbolizers(this StyledLayerDescriptor sld)
    {
        var result = new List<ISymbolizer>();

        var featureTypeStyles = sld.UserLayers.SelectMany(u => u.UserStyles.SelectMany(us => us.FeatureTypeStyles)).ToList();

        if (!featureTypeStyles.IsNullOrEmpty())
        {
            foreach (var item in featureTypeStyles)
            {
                result.AddRange(item.Parse());
            }
        }

        featureTypeStyles = sld.NamedLayers.SelectMany(n => n.UserStyles.SelectMany(us => us.FeatureTypeStyles)).ToList();

        if (!featureTypeStyles.IsNullOrEmpty())
        {
            foreach (var item in featureTypeStyles)
            {
                result.AddRange(item.Parse());
            }
        }

        return result;
    }


    public static System.Windows.Media.PenLineJoin Parse(this Sld_StrokeLineJoin sld_StrokeLineJoin)
    {
        return sld_StrokeLineJoin switch
        {
            Sld_StrokeLineJoin.Bevel => System.Windows.Media.PenLineJoin.Bevel,
            Sld_StrokeLineJoin.Mitre => System.Windows.Media.PenLineJoin.Miter,
            Sld_StrokeLineJoin.Round => System.Windows.Media.PenLineJoin.Round,
            _ => throw new NotImplementedException()
        };
    }

    public static System.Windows.Media.PenLineCap Parse(this Sld_StrokeLineCap sld_StrokeLineCap)
    {
        return sld_StrokeLineCap switch
        {
            Sld_StrokeLineCap.Butt => System.Windows.Media.PenLineCap.Flat,
            Sld_StrokeLineCap.Square => System.Windows.Media.PenLineCap.Square,
            Sld_StrokeLineCap.Round => System.Windows.Media.PenLineCap.Round,
            _ => throw new NotImplementedException()
        };
    }

    public static System.Windows.FontStyle Parse(this Sld_FontStyle sld_FontStyle)
    {
        return sld_FontStyle switch
        {
            Sld_FontStyle.Normal => System.Windows.FontStyles.Normal,
            Sld_FontStyle.Italic => System.Windows.FontStyles.Italic,
            Sld_FontStyle.Oblique => System.Windows.FontStyles.Oblique,
            _ => throw new NotImplementedException()
        };
    }

    public static System.Windows.FontWeight Parse(this Sld_FontWeight sld_FontWeight)
    {
        return sld_FontWeight switch
        {
            Sld_FontWeight.Normal => System.Windows.FontWeights.Normal,
            Sld_FontWeight.Bold => System.Windows.FontWeights.Bold,
            _ => throw new NotImplementedException()
        };
    }

    public static System.Windows.Media.Geometry? ParseToGeometry(this Mark mark, int width, int height)
    {
        System.Windows.Media.Geometry? geometry = mark.WellKnownNameValue switch
        {
            WellKnownMark.x => System.Windows.Media.Geometry.Parse("M90 390l120-120 130 120 30-30-130-120 130-120-30-30-130 120-120-120-30 30 120 120-120 120 30 30z"),
            WellKnownMark.circle => null, 
            WellKnownMark.star => System.Windows.Media.Geometry.Parse(new PackIconMaterial() { Kind = PackIconMaterialKind.Star, Height = height, Width = width }.Data),
            WellKnownMark.triangle => System.Windows.Media.Geometry.Parse("M192 704h640l-320-448z"),
            WellKnownMark.square => System.Windows.Media.Geometry.Parse("M10,14V10H14V14H10Z"),
            _ => null
        };

        if (geometry != null)
        {
            // Clone so we can modify it
            geometry = geometry.Clone();

            // Get original bounds
            var bounds = geometry.Bounds;

            double scaleX = width / bounds.Width;
            double scaleY = height / bounds.Height;

            // To preserve aspect ratio, pick min(scaleX, scaleY)
            double scale = Math.Min(scaleX, scaleY);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(scale, scale));
            transformGroup.Children.Add(new TranslateTransform(-bounds.X * scale - bounds.Width / 2.0 * scale,
                                                                -bounds.Y * scale - bounds.Height / 2.0 * scale));

            geometry.Transform = transformGroup;
        }

        return geometry;
    }

    public static List<ISymbolizer> Parse(this FeatureTypeStyle featureTypeStyle)
    {
        var result = new List<ISymbolizer>();

        if (featureTypeStyle is null || featureTypeStyle.Rules.IsNullOrEmpty())
            return result;

        foreach (var rule in featureTypeStyle.Rules)
        {
            foreach (var sldSymbolizer in rule.Symbolizers)
            {
                var symbolizer = ParseSymbolizer(sldSymbolizer);

                symbolizer.MaxScaleDenominator = rule.MaxScaleDenominator;
                symbolizer.MinScaleDenominator = rule.MinScaleDenominator;

                if (rule.Filter is null)
                {
                    symbolizer.IsFilterPassed = f => true;
                }
                else
                {
                    symbolizer.IsFilterPassed = rule.Filter.ParseFilter();
                }


                result.Add(symbolizer);
            }
        }

        return result;
    }

    private static ISymbolizer ParseSymbolizer(Symbolizer symbolizer)
    {
        switch (symbolizer)
        {
            case PointSymbolizer pointSymbolizer:
                return Parse(pointSymbolizer);

            case LineSymbolizer lineSymbolizer:
                return Parse(lineSymbolizer);

            case PolygonSymbolizer polygonSymbolizer:
                return Parse(polygonSymbolizer);

            case TextSymbolizer textSymbolizer:
                return Parse(textSymbolizer);

            default:
                throw new NotImplementedException("SldExtensions > ");
        }
    }

    private static ISymbolizer Parse(LineSymbolizer lineSymbolizer)
    {
        var visualParameters = VisualParameters.CreateNew();

        visualParameters.Build(lineSymbolizer.Stroke);

        //var stroke = lineSymbolizer.Stroke.StrokeValue;
        //var strokeThickness = lineSymbolizer.Stroke.StrokeThicknessValue;
        //var strokeOpacity = lineSymbolizer.Stroke.StrokeOpacityValue;
        //var strokeLineJoin = lineSymbolizer.Stroke.StrokeLineJoinValue;
        //var strokeLineCap = lineSymbolizer.Stroke.StrokeLineCapValue;
        //var strokeDashArray = lineSymbolizer.Stroke.StrokeDashArrayValue;
        //var strokeDashOffset = lineSymbolizer.Stroke.StrokeDashOffsetValue;

        //var visualParameters = VisualParameters.GetStroke(stroke, strokeThickness, strokeOpacity);

        //visualParameters.PenLineJoin = strokeLineJoin.Parse();
        //visualParameters.PenLineCap = strokeLineCap.Parse();

        //if (strokeDashArray is not null)
        //    visualParameters.DashStyle = new System.Windows.Media.DashStyle(strokeDashArray.ToList(), strokeDashOffset);

        return new SimpleSymbolizer(visualParameters);
    }

    private static ISymbolizer Parse(PolygonSymbolizer polygonSymbolizer)
    {
        var visualParameters = VisualParameters.CreateNew();

        visualParameters.Build(polygonSymbolizer.Stroke);

        //var stroke = polygonSymbolizer.Stroke.StrokeValue;
        //var strokeThickness = polygonSymbolizer.Stroke.StrokeThicknessValue;
        //var strokeOpacity = polygonSymbolizer.Stroke.StrokeOpacityValue;
        //var strokeLineJoin = polygonSymbolizer.Stroke.StrokeLineJoinValue;
        //var strokeLineCap = polygonSymbolizer.Stroke.StrokeLineCapValue;
        //var strokeDashArray = polygonSymbolizer.Stroke.StrokeDashArrayValue;
        //var strokeDashOffset = polygonSymbolizer.Stroke.StrokeDashOffsetValue;

        //var fill = polygonSymbolizer.Fill.FillValue;
        //var fillOpacity = polygonSymbolizer.Fill.FillOpacityValue;

        visualParameters.Build(polygonSymbolizer.Fill);

        // todo:  
        //var visualParameters = VisualParameters.Get(fill, stroke, strokeThickness, fillOpacity, strokeOpacity);

        //visualParameters.PenLineJoin = strokeLineJoin.Parse();
        //visualParameters.PenLineCap = strokeLineCap.Parse();

        //if (strokeDashArray is not null)
        //    visualParameters.DashStyle = new System.Windows.Media.DashStyle(strokeDashArray.ToList(), strokeDashOffset);

        return new SimpleSymbolizer(visualParameters);
    }

    public static ISymbolizer Parse(PointSymbolizer pointSymbolizer)
    {
        var visualParameters = VisualParameters.CreateNew();

        if (pointSymbolizer.Graphic is null)
            return new SimpleSymbolizer(visualParameters);

        if (pointSymbolizer.Graphic.Marks.IsNullOrEmpty())
            return new SimpleSymbolizer(visualParameters);

        // todo
        var opacity = pointSymbolizer.Graphic.Opacity ?? SldConstants.DefaultGraphicOpacity;
        var size = pointSymbolizer.Graphic.Size ?? 16;

        // todo
        var rotation = pointSymbolizer.Graphic.Rotation ?? SldConstants.DefaultGraphicRotation;

        var mark = pointSymbolizer.Graphic.Marks.FirstOrDefault();

        if (mark is null)
            return new SimpleSymbolizer(visualParameters);

        visualParameters.Build(mark.Stroke);
        visualParameters.Build(mark.Fill);

        System.Windows.Media.Geometry? geometry = mark.ParseToGeometry(size, size);

        visualParameters.PointSymbol = new SimplePointSymbolizer(size) { GeometrySymbol = geometry };

        return new SimpleSymbolizer(visualParameters);
    }

    public static ISymbolizer Parse(TextSymbolizer textSymbolizer)
    {
        var fontFamilyValue = textSymbolizer.Font.FontFamilyValue;
        var fontSize = (int)textSymbolizer.Font.FontSizeValue;
        var fontWeight = textSymbolizer.Font.FontWeightValue;
        var fontStyle = textSymbolizer.Font.FontStyleValue;

        var fill = textSymbolizer.Fill.FillValue;
        var fillOpacity = textSymbolizer.Fill.FillOpacityValue;

        var fillColor = ColorHelper.ToWpfColor(fill, fillOpacity);

        var fontFamily = new FontFamily(fontFamilyValue);

        if (textSymbolizer.LabelPlacement?.PointPlacement != null)
        {
            // todo
        }

        if (textSymbolizer.LabelPlacement?.LinePlacement != null)
        {
            // todo
        }

        // A halo creates a colored background around the label text,
        // which improves readability in low contrast situations.
        // Within the <Halo> element there are two sub-elements which
        // control the appearance of the halo: Radius, Fill
        if (textSymbolizer.Halo != null)
        {
            // todo
        }

        var labelParameters = VisualParameters.CreateLabel(ScaleInterval.All, fontSize, new SolidColorBrush(fillColor), fontFamily, p => p.AsPoint(), isRtl: false);

        var labelAttribute = textSymbolizer.Label?.PropertyName ?? string.Empty;

        return new LabelSymbolizer(labelParameters, labelAttribute);
    }


}

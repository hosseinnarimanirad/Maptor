using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Helpers;
using System.Windows.Media;
using IRI.Maptor.Sta.Ogc;
using MahApps.Metro.IconPacks;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using System.Globalization;
using IRI.Maptor.Jab.Core.Models;

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

    #region Helper methods

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

    #endregion

    public static StyledLayerDescriptor ParseToSld(this IEnumerable<ISymbolizer> symbolizers)
    {
        var sld = new StyledLayerDescriptor
        {
            Name = "GeneratedSLD",
            Title = "Generated SLD"
        };

        var namedLayer = new NamedLayer
        {
            Name = "GeneratedLayer"
        };

        var userStyle = new UserStyle
        {
            Name = "GeneratedStyle"
        };

        var featureTypeStyle = new FeatureTypeStyle
        {
            Name = "GeneratedFeatureTypeStyle"
        };

        if (!symbolizers.IsNullOrEmpty())
        {
            foreach (var symbolizer in symbolizers)
            {
                var sldSymbolizer = ToSldSymbolizer(symbolizer);

                if (sldSymbolizer is null)
                    continue;

                featureTypeStyle.Rules.Add(new Rule
                {
                    MinScaleDenominator = symbolizer.MinScaleDenominator,
                    MaxScaleDenominator = symbolizer.MaxScaleDenominator,
                    Symbolizers = new List<Symbolizer> { sldSymbolizer }
                });
            }
        }

        userStyle.FeatureTypeStyles.Add(featureTypeStyle);
        namedLayer.UserStyles.Add(userStyle);
        sld.NamedLayers.Add(namedLayer);

        return sld;
    }

    private static Symbolizer? ToSldSymbolizer(ISymbolizer symbolizer)
    {
        if (symbolizer is null)
            return null;

        if (symbolizer is LabelSymbolizer labelSymbolizer)
            return ToTextSymbolizer(labelSymbolizer);

        if (symbolizer is SimplePointSymbolizer pointSymbolizer)
            return ToPointSymbolizer(pointSymbolizer);

        if (symbolizer is SimpleSymbolizer simpleSymbolizer)
            return ToSimpleSymbolizer(simpleSymbolizer);

        return null;
    }

    private static Symbolizer ToSimpleSymbolizer(SimpleSymbolizer symbolizer)
    {
        var param = symbolizer.Param ?? VisualParameters.CreateNew();

        var hasPointSymbol = param.PointSymbol?.GeometrySymbol != null || param.PointSymbol?.ImageSymbol != null || param.PointSymbol?.ImageSymbolGdiPlus != null;
        if (hasPointSymbol)
            return ToPointSymbolizer(param);

        var hasFill = (param.Fill as SolidColorBrush)?.Color.A > 0;
        if (hasFill)
        {
            return new PolygonSymbolizer
            {
                Fill = CreateFill(param.Fill),
                Stroke = CreateStroke(param)
            };
        }

        return new LineSymbolizer
        {
            Stroke = CreateStroke(param)
        };
    }

    private static PointSymbolizer ToPointSymbolizer(SimplePointSymbolizer pointSymbolizer)
    {
        var pointSize = (int)Math.Max(1, Math.Round(Math.Max(pointSymbolizer.SymbolWidth, pointSymbolizer.SymbolHeight)));
        var defaultParam = VisualParameters.CreateNew();
        return ToPointSymbolizer(defaultParam, pointSize);
    }

    private static PointSymbolizer ToPointSymbolizer(VisualParameters param, int? sizeOverride = null)
    {
        var size = sizeOverride ?? (int)Math.Max(1, Math.Round(Math.Max(param.PointSymbol?.SymbolWidth ?? 8, param.PointSymbol?.SymbolHeight ?? 8)));
        return new PointSymbolizer
        {
            Graphic = new Graphic
            {
                Size = size,
                Marks = new List<Mark>
                {
                    new Mark
                    {
                        WellKnownName = WellKnownMark.circle.ToString(),
                        Fill = CreateFill(param.Fill),
                        Stroke = CreateStroke(param)
                    }
                }
            }
        };
    }

    private static TextSymbolizer ToTextSymbolizer(LabelSymbolizer symbolizer)
    {
        var param = symbolizer.Param ?? VisualParameters.CreateLabel(ScaleInterval.All, 10, Brushes.Black, new FontFamily("Arial"), p => p.AsPoint(), isRtl: false);
        var fontColor = (param.Foreground as SolidColorBrush)?.Color ?? Colors.Black;

        return new TextSymbolizer
        {
            Label = new Label { PropertyName = symbolizer.LabelAttribute ?? string.Empty },
            Font = CreateFont(param),
            Fill = new Fill
            {
                CssParameters = new List<CssParameter>
                {
                    new CssParameter { Name = SldHelper.CssParameter_Fill, Value = ToRgbHex(fontColor) }
                }
            }
        };
    }

    private static Fill CreateFill(Brush? brush)
    {
        var color = (brush as SolidColorBrush)?.Color ?? Colors.Transparent;
        return new Fill
        {
            CssParameters = new List<CssParameter>
            {
                new CssParameter { Name = SldHelper.CssParameter_Fill, Value = ToRgbHex(color) },
                new CssParameter { Name = SldHelper.CssParameter_FillOpacity, Value = ToOpacity(color.A) }
            }
        };
    }

    private static Stroke CreateStroke(VisualParameters param)
    {
        var color = (param.Stroke as SolidColorBrush)?.Color ?? Colors.Black;
        var dashStyle = param.DashStyle;
        var cssParameters = new List<CssParameter>
        {
            new CssParameter { Name = SldHelper.CssParameter_Stroke, Value = ToRgbHex(color) },
            new CssParameter { Name = SldHelper.CssParameter_StrokeWidth, Value = Math.Max(0, param.StrokeThickness).ToString("0.##", CultureInfo.InvariantCulture) },
            new CssParameter { Name = SldHelper.CssParameter_StrokeOpacity, Value = ToOpacity(color.A) },
            new CssParameter { Name = SldHelper.CssParameter_StrokeLineCap, Value = Parse(param.PenDashCap).ToString().ToLowerInvariant() },
            new CssParameter { Name = SldHelper.CssParameter_StrokeLineJoin, Value = Parse(param.PenLineJoin).ToString().ToLowerInvariant() }
        };

        if (dashStyle?.Dashes?.Any() == true)
        {
            cssParameters.Add(new CssParameter
            {
                Name = SldHelper.CssParameter_StrokeDashArray,
                Value = string.Join(" ", dashStyle.Dashes.Select(d => d.ToString("0.##", CultureInfo.InvariantCulture)))
            });
        }

        if (dashStyle is not null && dashStyle.Offset != 0)
        {
            cssParameters.Add(new CssParameter
            {
                Name = SldHelper.CssParameter_StrokeDashOffset,
                Value = dashStyle.Offset.ToString("0.##", CultureInfo.InvariantCulture)
            });
        }

        return new Stroke { CssParameters = cssParameters };
    }

    private static Font CreateFont(VisualParameters param)
    {
        return new Font
        {
            CssParameters = new List<CssParameter>
            {
                new CssParameter { Name = SldHelper.CssParameter_FontFamily, Value = param.FontFamily?.Source ?? "Arial" },
                new CssParameter { Name = SldHelper.CssParameter_FontSize, Value = Math.Max(1, param.FontSize).ToString("0.##", CultureInfo.InvariantCulture) },
                new CssParameter { Name = SldHelper.CssParameter_FontStyle, Value = Sld_FontStyle.Normal.ToString().ToLowerInvariant() },
                new CssParameter { Name = SldHelper.CssParameter_FontWeight, Value = Sld_FontWeight.Normal.ToString().ToLowerInvariant() }
            }
        };
    }

    private static string ToRgbHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static string ToOpacity(byte alpha)
    {
        return (alpha / 255.0).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static Sld_StrokeLineJoin Parse(this System.Windows.Media.PenLineJoin penLineJoin)
    {
        return penLineJoin switch
        {
            System.Windows.Media.PenLineJoin.Bevel => Sld_StrokeLineJoin.Bevel,
            System.Windows.Media.PenLineJoin.Miter => Sld_StrokeLineJoin.Mitre,
            System.Windows.Media.PenLineJoin.Round => Sld_StrokeLineJoin.Round,
            _ => Sld_StrokeLineJoin.Mitre
        };
    }

    private static Sld_StrokeLineCap Parse(this System.Windows.Media.PenLineCap penLineCap)
    {
        return penLineCap switch
        {
            System.Windows.Media.PenLineCap.Round => Sld_StrokeLineCap.Round,
            System.Windows.Media.PenLineCap.Square => Sld_StrokeLineCap.Square,
            _ => Sld_StrokeLineCap.Butt
        };
    }
}

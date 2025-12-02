using System;
using System.Linq;
using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Jab.Common.ViewModels.Symbology;

public class PointSymbolizerViewModel : SymbolizerViewModelBase
{
    public override string SymbolizerType => "Point";

    private WellKnownMark _wellKnownMark = WellKnownMark.circle;
    public WellKnownMark WellKnownMarkType
    {
        get => _wellKnownMark;
        set
        {
            _wellKnownMark = value;
            RaisePropertyChanged();
        }
    }

    private System.Windows.Media.Color _fillColor = System.Windows.Media.Colors.Red;
    public System.Windows.Media.Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            RaisePropertyChanged();
        }
    }

    private double _fillOpacity = 1.0;
    public double FillOpacity
    {
        get => _fillOpacity;
        set
        {
            _fillOpacity = Math.Clamp(value, 0.0, 1.0);
            RaisePropertyChanged();
        }
    }

    private System.Windows.Media.Color _strokeColor = System.Windows.Media.Colors.Black;
    public System.Windows.Media.Color StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            RaisePropertyChanged();
        }
    }

    private double _strokeWidth = 1.0;
    public double StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    private double _strokeOpacity = 1.0;
    public double StrokeOpacity
    {
        get => _strokeOpacity;
        set
        {
            _strokeOpacity = Math.Clamp(value, 0.0, 1.0);
            RaisePropertyChanged();
        }
    }

    private int _size = 8;
    public int Size
    {
        get => _size;
        set
        {
            _size = Math.Max(1, value);
            RaisePropertyChanged();
        }
    }

    private double _rotation = 0.0;
    public double Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            RaisePropertyChanged();
        }
    }

    public override Symbolizer ToSymbolizer()
    {
        var mark = new Mark
        {
            WellKnownName = WellKnownMarkType.ToString().ToLowerInvariant(),
            Fill = new Fill
            {
                CssParameters = new System.Collections.Generic.List<CssParameter>
                {
                    new CssParameter
                    {
                        Name = SldHelper.CssParameter_Fill,
                        Value = $"#{FillColor.R:X2}{FillColor.G:X2}{FillColor.B:X2}"
                    },
                    new CssParameter
                    {
                        Name = SldHelper.CssParameter_FillOpacity,
                        Value = FillOpacity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            },
            Stroke = new Stroke
            {
                CssParameters = new System.Collections.Generic.List<CssParameter>
                {
                    new CssParameter
                    {
                        Name = SldHelper.CssParameter_Stroke,
                        Value = $"#{StrokeColor.R:X2}{StrokeColor.G:X2}{StrokeColor.B:X2}"
                    },
                    new CssParameter
                    {
                        Name = SldHelper.CssParameter_StrokeWidth,
                        Value = StrokeWidth.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    },
                    new CssParameter
                    {
                        Name = SldHelper.CssParameter_StrokeOpacity,
                        Value = StrokeOpacity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            }
        };

        var graphic = new Graphic
        {
            Marks = new System.Collections.Generic.List<Mark> { mark },
            Size = Size,
            Rotation = Rotation != 0 ? Rotation : null
        };

        var symbolizer = new PointSymbolizer
        {
            Graphic = graphic
        };

        if (!string.IsNullOrWhiteSpace(GeometryPropertyName))
        {
            symbolizer.Geometry = new Geometry { PropertyName = GeometryPropertyName };
        }

        return symbolizer;
    }

    public override void FromSymbolizer(Symbolizer symbolizer)
    {
        if (symbolizer is not PointSymbolizer pointSymbolizer)
            return;

        GeometryPropertyName = pointSymbolizer.Geometry?.PropertyName;

        if (pointSymbolizer.Graphic?.Marks?.FirstOrDefault() is Mark mark)
        {
            if (Enum.TryParse<WellKnownMark>(mark.WellKnownName, true, out var wkm))
                WellKnownMarkType = wkm;

            if (mark.Fill != null)
            {
                var fillParam = mark.Fill.GetParameter(SldHelper.CssParameter_Fill);
                if (fillParam != null && TryParseHexColor(fillParam.Value, out var fillColor))
                    FillColor = fillColor;

                var fillOpacityParam = mark.Fill.GetParameter(SldHelper.CssParameter_FillOpacity);
                if (fillOpacityParam?.DoubleValue.HasValue == true)
                    FillOpacity = fillOpacityParam.DoubleValue.Value;
            }

            if (mark.Stroke != null)
            {
                var strokeParam = mark.Stroke.GetParameter(SldHelper.CssParameter_Stroke);
                if (strokeParam != null && TryParseHexColor(strokeParam.Value, out var strokeColor))
                    StrokeColor = strokeColor;

                var strokeWidthParam = mark.Stroke.GetParameter(SldHelper.CssParameter_StrokeWidth);
                if (strokeWidthParam?.DoubleValue.HasValue == true)
                    StrokeWidth = strokeWidthParam.DoubleValue.Value;

                var strokeOpacityParam = mark.Stroke.GetParameter(SldHelper.CssParameter_StrokeOpacity);
                if (strokeOpacityParam?.DoubleValue.HasValue == true)
                    StrokeOpacity = strokeOpacityParam.DoubleValue.Value;
            }
        }

        if (pointSymbolizer.Graphic != null)
        {
            if (pointSymbolizer.Graphic.Size.HasValue)
                Size = pointSymbolizer.Graphic.Size.Value;

            if (pointSymbolizer.Graphic.Rotation.HasValue)
                Rotation = pointSymbolizer.Graphic.Rotation.Value;
        }
    }

    private bool TryParseHexColor(string hex, out System.Windows.Media.Color color)
    {
        color = System.Windows.Media.Colors.Black;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.TrimStart('#');
        if (hex.Length == 6 &&
            byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            color = System.Windows.Media.Color.FromRgb(r, g, b);
            return true;
        }

        return false;
    }
}


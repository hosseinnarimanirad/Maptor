using System;
using System.Linq; 
using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Jab.Controls.ViewModel.Symbology.Sld;

public class LineSymbolizerViewModel : SymbolizerViewModelBase
{
    public override string SymbolizerType => "Line";

    private System.Windows.Media.Color _strokeColor = System.Windows.Media.Colors.Blue;
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

    private Sld_StrokeLineCap _lineCap = Sld_StrokeLineCap.Butt;
    public Sld_StrokeLineCap LineCap
    {
        get => _lineCap;
        set
        {
            _lineCap = value;
            RaisePropertyChanged();
        }
    }

    private Sld_StrokeLineJoin _lineJoin = Sld_StrokeLineJoin.Mitre;
    public Sld_StrokeLineJoin LineJoin
    {
        get => _lineJoin;
        set
        {
            _lineJoin = value;
            RaisePropertyChanged();
        }
    }

    private string _dashArray = "";
    public string DashArray
    {
        get => _dashArray;
        set
        {
            _dashArray = value ?? "";
            RaisePropertyChanged();
        }
    }

    private double _dashOffset = 0.0;
    public double DashOffset
    {
        get => _dashOffset;
        set
        {
            _dashOffset = value;
            RaisePropertyChanged();
        }
    }

    public override Symbolizer ToSymbolizer()
    {
        var cssParameters = new System.Collections.Generic.List<CssParameter>
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
            },
            new CssParameter
            {
                Name = SldHelper.CssParameter_StrokeLineCap,
                Value = LineCap.ToString().ToLowerInvariant()
            },
            new CssParameter
            {
                Name = SldHelper.CssParameter_StrokeLineJoin,
                Value = LineJoin.ToString().ToLowerInvariant()
            }
        };

        if (!string.IsNullOrWhiteSpace(DashArray))
        {
            cssParameters.Add(new CssParameter
            {
                Name = SldHelper.CssParameter_StrokeDashArray,
                Value = DashArray
            });
        }

        if (DashOffset != 0)
        {
            cssParameters.Add(new CssParameter
            {
                Name = SldHelper.CssParameter_StrokeDashOffset,
                Value = DashOffset.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        var stroke = new Stroke { CssParameters = cssParameters };

        var symbolizer = new LineSymbolizer { Stroke = stroke };

        if (!string.IsNullOrWhiteSpace(GeometryPropertyName))
        {
            symbolizer.Geometry = new Geometry { PropertyName = GeometryPropertyName };
        }

        return symbolizer;
    }

    public override void FromSymbolizer(Symbolizer symbolizer)
    {
        if (symbolizer is not LineSymbolizer lineSymbolizer || lineSymbolizer.Stroke == null)
            return;

        GeometryPropertyName = lineSymbolizer.Geometry?.PropertyName;

        var strokeParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_Stroke);
        if (strokeParam != null && TryParseHexColor(strokeParam.Value, out var strokeColor))
            StrokeColor = strokeColor;

        var strokeWidthParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_StrokeWidth);
        if (strokeWidthParam?.DoubleValue.HasValue == true)
            StrokeWidth = strokeWidthParam.DoubleValue.Value;

        var strokeOpacityParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_StrokeOpacity);
        if (strokeOpacityParam?.DoubleValue.HasValue == true)
            StrokeOpacity = strokeOpacityParam.DoubleValue.Value;

        var lineCapParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_StrokeLineCap);
        if (lineCapParam?.StrokeLineCap.HasValue == true)
            LineCap = lineCapParam.StrokeLineCap.Value;

        var lineJoinParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_StrokeLineJoin);
        if (lineJoinParam?.StrokeLineJoin.HasValue == true)
            LineJoin = lineJoinParam.StrokeLineJoin.Value;

        var dashArrayParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_StrokeDashArray);
        if (dashArrayParam != null)
            DashArray = dashArrayParam.Value ?? "";

        var dashOffsetParam = lineSymbolizer.Stroke.GetParameter(SldHelper.CssParameter_StrokeDashOffset);
        if (dashOffsetParam?.DoubleValue.HasValue == true)
            DashOffset = dashOffsetParam.DoubleValue.Value;
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


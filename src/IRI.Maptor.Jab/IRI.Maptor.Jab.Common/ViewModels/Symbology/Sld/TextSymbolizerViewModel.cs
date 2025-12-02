using System;
using System.Linq; 
using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Jab.Common.ViewModels.Symbology.Sld;

public class TextSymbolizerViewModel : SymbolizerViewModelBase
{
    public override string SymbolizerType => "Text";

    private string _labelPropertyName = "";
    public string LabelPropertyName
    {
        get => _labelPropertyName;
        set
        {
            _labelPropertyName = value ?? "";
            RaisePropertyChanged();
        }
    }

    private string _fontFamily = "Arial";
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            _fontFamily = value ?? "Arial";
            RaisePropertyChanged();
        }
    }

    private double _fontSize = 10;
    public double FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = Math.Max(1, value);
            RaisePropertyChanged();
        }
    }

    private Sld_FontStyle _fontStyle = Sld_FontStyle.Normal;
    public Sld_FontStyle FontStyle
    {
        get => _fontStyle;
        set
        {
            _fontStyle = value;
            RaisePropertyChanged();
        }
    }

    private Sld_FontWeight _fontWeight = Sld_FontWeight.Normal;
    public Sld_FontWeight FontWeight
    {
        get => _fontWeight;
        set
        {
            _fontWeight = value;
            RaisePropertyChanged();
        }
    }

    private System.Windows.Media.Color _fontColor = System.Windows.Media.Colors.Black;
    public System.Windows.Media.Color FontColor
    {
        get => _fontColor;
        set
        {
            _fontColor = value;
            RaisePropertyChanged();
        }
    }

    private bool _enableHalo = false;
    public bool EnableHalo
    {
        get => _enableHalo;
        set
        {
            _enableHalo = value;
            RaisePropertyChanged();
        }
    }

    private double _haloRadius = 1.0;
    public double HaloRadius
    {
        get => _haloRadius;
        set
        {
            _haloRadius = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    private System.Windows.Media.Color _haloColor = System.Windows.Media.Colors.White;
    public System.Windows.Media.Color HaloColor
    {
        get => _haloColor;
        set
        {
            _haloColor = value;
            RaisePropertyChanged();
        }
    }

    private double _haloOpacity = 1.0;
    public double HaloOpacity
    {
        get => _haloOpacity;
        set
        {
            _haloOpacity = Math.Clamp(value, 0.0, 1.0);
            RaisePropertyChanged();
        }
    }

    public override Symbolizer ToSymbolizer()
    {
        var font = new Font
        {
            CssParameters = new System.Collections.Generic.List<CssParameter>
            {
                new CssParameter
                {
                    Name = SldHelper.CssParameter_FontFamily,
                    Value = FontFamily
                },
                new CssParameter
                {
                    Name = SldHelper.CssParameter_FontSize,
                    Value = FontSize.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                },
                new CssParameter
                {
                    Name = SldHelper.CssParameter_FontStyle,
                    Value = FontStyle.ToString().ToLowerInvariant()
                },
                new CssParameter
                {
                    Name = SldHelper.CssParameter_FontWeight,
                    Value = FontWeight.ToString().ToLowerInvariant()
                }
            }
        };

        var fill = new Fill
        {
            CssParameters = new System.Collections.Generic.List<CssParameter>
            {
                new CssParameter
                {
                    Name = SldHelper.CssParameter_Fill,
                    Value = $"#{FontColor.R:X2}{FontColor.G:X2}{FontColor.B:X2}"
                }
            }
        };

        var symbolizer = new TextSymbolizer
        {
            Label = new Label { PropertyName = LabelPropertyName },
            Font = font,
            Fill = fill
        };

        if (EnableHalo)
        {
            symbolizer.Halo = new Halo
            {
                Radius = new ParameterValueType
                {
                    Value = HaloRadius.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                },
                Fill = new Fill
                {
                    CssParameters = new System.Collections.Generic.List<CssParameter>
                    {
                        new CssParameter
                        {
                            Name = SldHelper.CssParameter_Fill,
                            Value = $"#{HaloColor.R:X2}{HaloColor.G:X2}{HaloColor.B:X2}"
                        },
                        new CssParameter
                        {
                            Name = SldHelper.CssParameter_FillOpacity,
                            Value = HaloOpacity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(GeometryPropertyName))
        {
            symbolizer.Geometry = new Geometry { PropertyName = GeometryPropertyName };
        }

        return symbolizer;
    }

    public override void FromSymbolizer(Symbolizer symbolizer)
    {
        if (symbolizer is not TextSymbolizer textSymbolizer)
            return;

        GeometryPropertyName = textSymbolizer.Geometry?.PropertyName;
        LabelPropertyName = textSymbolizer.Label?.PropertyName ?? "";

        if (textSymbolizer.Font != null)
        {
            var fontFamilyParam = textSymbolizer.Font.GetParameter(SldHelper.CssParameter_FontFamily);
            if (fontFamilyParam != null)
                FontFamily = fontFamilyParam.Value;

            var fontSizeParam = textSymbolizer.Font.GetParameter(SldHelper.CssParameter_FontSize);
            if (fontSizeParam?.DoubleValue.HasValue == true)
                FontSize = fontSizeParam.DoubleValue.Value;

            var fontStyleParam = textSymbolizer.Font.GetParameter(SldHelper.CssParameter_FontStyle);
            if (fontStyleParam?.FontStyle.HasValue == true)
                FontStyle = fontStyleParam.FontStyle.Value;

            var fontWeightParam = textSymbolizer.Font.GetParameter(SldHelper.CssParameter_FontWeight);
            if (fontWeightParam?.FontWeight.HasValue == true)
                FontWeight = fontWeightParam.FontWeight.Value;
        }

        if (textSymbolizer.Fill != null)
        {
            var fillParam = textSymbolizer.Fill.GetParameter(SldHelper.CssParameter_Fill);
            if (fillParam != null && TryParseHexColor(fillParam.Value, out var fontColor))
                FontColor = fontColor;
        }

        if (textSymbolizer.Halo != null)
        {
            EnableHalo = true;

            if (double.TryParse(textSymbolizer.Halo.Radius?.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var radius))
                HaloRadius = radius;

            if (textSymbolizer.Halo.Fill != null)
            {
                var haloFillParam = textSymbolizer.Halo.Fill.GetParameter(SldHelper.CssParameter_Fill);
                if (haloFillParam != null && TryParseHexColor(haloFillParam.Value, out var haloColor))
                    HaloColor = haloColor;

                var haloOpacityParam = textSymbolizer.Halo.Fill.GetParameter(SldHelper.CssParameter_FillOpacity);
                if (haloOpacityParam?.DoubleValue.HasValue == true)
                    HaloOpacity = haloOpacityParam.DoubleValue.Value;
            }
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


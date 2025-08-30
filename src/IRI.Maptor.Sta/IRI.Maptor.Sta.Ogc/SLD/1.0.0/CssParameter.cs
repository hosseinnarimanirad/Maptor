using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Ogc.SLD;

public class CssParameter
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlText]
    public string Value { get; set; }


    [XmlIgnore]
    public Sld_StrokeLineCap? StrokeLineCap
    {
        get => Name == SldHelper.CssParameter_StrokeLineCap && Enum.TryParse<Sld_StrokeLineCap>(Value, true, out var result) ? result : Sld_StrokeLineCap.Butt;
        set
        {
            if (value.HasValue)
            {
                Name = SldHelper.CssParameter_StrokeLineCap;
                Value = value.Value.ToString().ToLowerInvariant();
            }
        }
    }

    [XmlIgnore]
    public Sld_StrokeLineJoin? StrokeLineJoin
    {
        get => Name == SldHelper.CssParameter_StrokeLineJoin && Enum.TryParse<Sld_StrokeLineJoin>(Value, true, out var result) ? result : Sld_StrokeLineJoin.Mitre;
        set
        {
            if (value.HasValue)
            {
                Name = SldHelper.CssParameter_StrokeLineJoin;
                Value = value.Value.ToString().ToLowerInvariant();
            }
        }
    }

    [XmlIgnore]
    public Sld_FontWeight? FontWeight
    {
        get => Name == SldHelper.CssParameter_FontWeight && Enum.TryParse<Sld_FontWeight>(Value, true, out var result) ? result : Sld_FontWeight.Normal;
        set
        {
            if (value.HasValue)
            {
                Name = SldHelper.CssParameter_FontWeight;
                Value = value.Value.ToString().ToLowerInvariant();
            }
        }
    }

    [XmlIgnore]
    public Sld_FontStyle? FontStyle
    {
        get => Name == SldHelper.CssParameter_FontStyle && Enum.TryParse<Sld_FontStyle>(Value, true, out var result) ? result : Sld_FontStyle.Normal;
        set
        {
            if (value.HasValue)
            {
                Name = SldHelper.CssParameter_FontStyle;
                Value = value.Value.ToString().ToLowerInvariant();
            }
        }
    }

    [XmlIgnore]
    public double? DoubleValue
    {
        get => double.TryParse(Value,
                                System.Globalization.NumberStyles.AllowDecimalPoint |
                                System.Globalization.NumberStyles.AllowExponent,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var result) ? result : null;
        set
        {
            if (value.HasValue)
            {
                Value = value.Value.ToString().ToLowerInvariant();
            }
        }
    }
}
using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Ogc.SLD;

public class Font
{
    [XmlElement("CssParameter")]
    public List<CssParameter> CssParameters { get; set; } = new();

    public CssParameter? GetParameter(string key) => this.CssParameters.FirstOrDefault(c => c.Name.EqualsIgnoreCase(key));


    [XmlIgnore]
    public string FontFamilyValue => GetParameter(SldHelper.CssParameter_FontFamily)?.Value ?? SldConstants.DefaultFontFamily;

    [XmlIgnore]
    public Sld_FontStyle FontStyleValue => GetParameter(SldHelper.CssParameter_FontStyle)?.FontStyle ?? SldConstants.DefaultFontStyle;

    [XmlIgnore]
    public Sld_FontWeight FontWeightValue => GetParameter(SldHelper.CssParameter_FontWeight)?.FontWeight ?? SldConstants.DefaultFontWeight;

    [XmlIgnore]
    public double FontSizeValue => GetParameter(SldHelper.CssParameter_FontSize)?.DoubleValue ?? SldConstants.DefaultFontSize;



}
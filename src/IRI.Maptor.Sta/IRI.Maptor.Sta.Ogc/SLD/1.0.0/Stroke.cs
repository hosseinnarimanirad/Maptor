using System;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;

using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Ogc.SLD;

public class Stroke
{
    [XmlElement("GraphicFill")]
    public GraphicFill GraphicFill { get; set; }

    [XmlElement("GraphicStroke")]
    public GraphicStroke? GraphicStroke { get; set; }

    [XmlElement("CssParameter")]
    public List<CssParameter> CssParameters { get; set; } = new();

    public CssParameter? GetParameter(string key) => this.CssParameters.FirstOrDefault(c => c.Name.EqualsIgnoreCase(key));


    public bool HasGraphicStroke => GraphicStroke is not null;

    public bool HasCssParameters => CssParameters.Any();

    [XmlIgnore]
    public string StrokeValue => GetParameter(SldHelper.CssParameter_Stroke)?.Value ?? SldConstants.DefaultStroke;

    [XmlIgnore]
    public double StrokeThicknessValue => GetParameter(SldHelper.CssParameter_StrokeWidth).DoubleValue ?? SldConstants.DefaultStrokeWidth;

    [XmlIgnore]
    public double StrokeOpacityValue => GetParameter(SldHelper.CssParameter_StrokeOpacity).DoubleValue ?? SldConstants.DefaultStrokeOpacity;

    [XmlIgnore]
    public Sld_StrokeLineJoin StrokeLineJoinValue => GetParameter(SldHelper.CssParameter_StrokeLineJoin)?.StrokeLineJoin ?? SldConstants.DefaultStrokeLineJoin;

    [XmlIgnore]
    public Sld_StrokeLineCap StrokeLineCapValue => GetParameter(SldHelper.CssParameter_StrokeLineCap)?.StrokeLineCap ?? SldConstants.DefaultStrokeLineCap;

    [XmlIgnore]
    public double StrokeDashOffsetValue => GetParameter(SldHelper.CssParameter_StrokeDashOffset)?.DoubleValue ?? SldConstants.DefaultStrokeDashOffset;

    [XmlIgnore]
    public double[]? StrokeDashArrayValue => GetParameter(SldHelper.CssParameter_StrokeDashArray)?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                                                                                        ?.Select(double.Parse)
                                                                                                        ?.ToArray();


}
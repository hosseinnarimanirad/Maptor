using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Ogc.SLD;

public class Fill
{
    [XmlElement("GraphicFill")]
    public GraphicFill GraphicFill { get; set; }

    [XmlElement("CssParameter")]
    public List<CssParameter> CssParameters { get; set; } = new();

    public CssParameter? GetParameter(string key) => this.CssParameters.FirstOrDefault(c => c.Name.EqualsIgnoreCase(key));


    public bool HasCssParameters => CssParameters.Any();



    [XmlIgnore]
    public string FillValue => GetParameter(SldHelper.CssParameter_Fill)?.Value ?? SldConstants.DefaultFill;
     
    [XmlIgnore]
    public double FillOpacityValue => GetParameter(SldHelper.CssParameter_FillOpacity)?.DoubleValue ?? SldConstants.DefaultFillOpacity;

}
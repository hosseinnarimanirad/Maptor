using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace IRI.Maptor.Sta.Ogc.SLD;

public class Rule
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("Title")]
    public string Title { get; set; }

    [XmlElement("Abstract")]
    public string Abstract { get; set; }

    [XmlElement("LegendGraphic")]
    public LegendGraphic LegendGraphic { get; set; }

    [XmlElement("Filter", Namespace = SldNamespaces.OGC)]
    public OgcFilter Filter { get; set; }

    [XmlElement("ElseFilter")]
    public ElseFilter ElseFilter { get; set; }

    [XmlElement("MinScaleDenominator")]
    public double? MinScaleDenominator { get; set; }

    [XmlElement("MaxScaleDenominator")]
    public double? MaxScaleDenominator { get; set; }

    // Polymorphic list of symbolizers (including Raster)
    [XmlElement("LineSymbolizer", Type = typeof(LineSymbolizer))]
    [XmlElement("PolygonSymbolizer", Type = typeof(PolygonSymbolizer))]
    [XmlElement("PointSymbolizer", Type = typeof(PointSymbolizer))]
    [XmlElement("TextSymbolizer", Type = typeof(TextSymbolizer))]
    [XmlElement("RasterSymbolizer", Type = typeof(RasterSymbolizer))]
    public List<Symbolizer> Symbolizers { get; set; } = new();

    public bool ShouldSerializeMinScaleDenominator() => MinScaleDenominator.HasValue;
    public bool ShouldSerializeMaxScaleDenominator() => MaxScaleDenominator.HasValue;
}
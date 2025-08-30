using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc;



public abstract class SpatialOperator : OgcFilterBase { }

[XmlType("Equals", Namespace = SldNamespaces.FES)]
public class OgcEqualsSpatialy : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Disjoint", Namespace = SldNamespaces.FES)]
public class OgcDisjoint : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Touches", Namespace = SldNamespaces.FES)]
public class OgcTouches : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Within", Namespace = SldNamespaces.FES)]
public class OgcWithin : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Overlaps", Namespace = SldNamespaces.FES)]
public class OgcOverlaps : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Crosses", Namespace = SldNamespaces.FES)]
public class OgcCrosses : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Intersects", Namespace = SldNamespaces.FES)]
public class OgcIntersects : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Contains", Namespace = SldNamespaces.FES)]
public class OgcContains : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("DWithin", Namespace = SldNamespaces.FES)]
public class OgcDWithin : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    [XmlElement("Distance")]
    public Measure Distance { get; set; }
}

[XmlType("Beyond", Namespace = SldNamespaces.FES)]
public class OgcBeyond : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    [XmlElement("Distance")]
    public Measure Distance { get; set; }
}

[XmlType("BBOX", Namespace = SldNamespaces.FES)]
public class OgcBBOX : SpatialOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

public class Measure
{
    [XmlText]
    public double Value { get; set; }

    [XmlAttribute("uom")]
    public string Uom { get; set; }
}
 
using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Sta.Ogc;

 

public abstract class TemporalOperator : OgcFilterBase { }

[XmlType("After", Namespace = SldNamespaces.FES)]
public class OgcAfter : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Before", Namespace = SldNamespaces.FES)]
public class OgcBefore : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Begins", Namespace = SldNamespaces.FES)]
public class OgcBegins : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("BegunBy", Namespace = SldNamespaces.FES)]
public class OgcBegunBy : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("TContains", Namespace = SldNamespaces.FES)]
public class OgcTContains : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("During", Namespace = SldNamespaces.FES)]
public class OgcDuring : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("EndedBy", Namespace = SldNamespaces.FES)]
public class OgcEndedBy : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Ends", Namespace = SldNamespaces.FES)]
public class OgcEnds : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("TEquals", Namespace = SldNamespaces.FES)]
public class OgcTEquals : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Meets", Namespace = SldNamespaces.FES)]
public class OgcMeets : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("MetBy", Namespace = SldNamespaces.FES)]
public class OgcMetBy : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("TOverlaps", Namespace = SldNamespaces.FES)]
public class OgcTOverlaps : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("OverlappedBy", Namespace = SldNamespaces.FES)]
public class OgcOverlappedBy : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("AnyInteracts", Namespace = SldNamespaces.FES)]
public class OgcAnyInteracts : TemporalOperator
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}
 

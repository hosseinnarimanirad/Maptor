using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Sta.Ogc;


[XmlInclude(typeof(OgcLiteral))]
[XmlInclude(typeof(OgcPropertyName))]
[XmlInclude(typeof(OgcFunction))]
[XmlInclude(typeof(OgcAdd))]
[XmlInclude(typeof(OgcSub))]
[XmlInclude(typeof(OgcMul))]
[XmlInclude(typeof(OgcDiv))]
public abstract class OgcExpression { }

public class OgcLiteral : OgcExpression
{
    [XmlText]
    public string Value { get; set; }

    public override string ToString() => Value;

    public double? GetDoubleValue()
    {
        return double.TryParse(Value, out double value) ? value : null;
    }
}

public class OgcPropertyName : OgcExpression
{
    [XmlText]
    public string Value { get; set; }

    public override string ToString() => Value;
}

[XmlType("Function", Namespace = SldNamespaces.OGC)]
public class OgcFunction : OgcExpression
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("expression")]
    public List<OgcExpression> Arguments { get; set; }
}

[XmlType("Add", Namespace = SldNamespaces.OGC)]
public class OgcAdd : OgcExpression
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Sub", Namespace = SldNamespaces.OGC)]
public class OgcSub : OgcExpression
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Mul", Namespace = SldNamespaces.OGC)]
public class OgcMul : OgcExpression
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("Div", Namespace = SldNamespaces.OGC)]
public class OgcDiv : OgcExpression
{
    [XmlElement("expression")]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
}

[XmlType("ResourceId", Namespace = SldNamespaces.FES)]
public class ResourceId
{
    [XmlAttribute("rid")]
    public string Rid { get; set; }

    [XmlAttribute("previousRid")]
    public string PreviousRid { get; set; }

    [XmlAttribute("version")]
    public string Version { get; set; }

    [XmlAttribute("startDate")]
    public DateTime StartDate { get; set; }

    [XmlAttribute("endDate")]
    public DateTime EndDate { get; set; }

    [XmlIgnore]
    public bool StartDateSpecified { get; set; }

    [XmlIgnore]
    public bool EndDateSpecified { get; set; }
}
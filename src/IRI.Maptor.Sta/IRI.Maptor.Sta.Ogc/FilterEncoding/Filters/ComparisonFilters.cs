using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IRI.Maptor.Sta.Ogc;


public abstract class OgcComparisonOperator : OgcFilterBase
{ 
    public abstract string? GetPropertyName();
}

[XmlType("PropertyIsEqualTo", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsEqualTo : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    [XmlAttribute("matchCase")]
    public bool MatchCase { get; set; } = true;

    [XmlAttribute("matchAction")]
    public MatchActionType MatchAction { get; set; } = MatchActionType.Any;

    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

    public string? GetLiteral()
    {
        var literal = (Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        return literal;
    }
}

[XmlType("PropertyIsNotEqualTo", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsNotEqualTo : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    [XmlAttribute("matchCase")]
    public bool MatchCase { get; set; } = true;

    [XmlAttribute("matchAction")]
    public MatchActionType MatchAction { get; set; } = MatchActionType.Any;


    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

    public string? GetLiteral()
    {
        var literal = (Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        return literal;
    }
}

[XmlType("PropertyIsLessThan", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsLessThan : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

    public double? GetLiteral()
    {
        var literal = (Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        if (literal is null)
            return null;

        return double.TryParse(literal, out var result) ? result : null;
    }
}

[XmlType("PropertyIsGreaterThan", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsGreaterThan : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
     
    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

    public double? GetLiteral()
    {
        var literal = (Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        if (literal is null)
            return null;

        return double.TryParse(literal, out var result) ? result : null;
    }
}

[XmlType("PropertyIsLessThanOrEqualTo", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsLessThanOrEqualTo : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();
     
    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

    public double? GetLiteral()
    {
        var literal = (Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        if (literal is null)
            return null;

        return double.TryParse(literal, out var result) ? result : null;
    }
}

[XmlType("PropertyIsGreaterThanOrEqualTo", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsGreaterThanOrEqualTo : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

    public double? GetLiteral()
    {
        var literal = (Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        if (literal is null)
            return null;

        return double.TryParse(literal, out var result) ? result : null;
    }
}

[XmlType("PropertyIsLike", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsLike : OgcComparisonOperator
{
    //[XmlElement("expression")]
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public List<OgcExpression> Expressions { get; set; } = new List<OgcExpression>();

    [XmlAttribute("wildCard")]
    public string WildCard { get; set; }

    [XmlAttribute("singleChar")]
    public string SingleChar { get; set; }

    [XmlAttribute("escapeChar")]
    public string EscapeChar { get; set; }

    [XmlAttribute("matchCase")]
    public bool MatchCase { get; set; } = true;

    public override string? GetPropertyName() => (Expressions.FirstOrDefault(e => e is OgcPropertyName) as OgcPropertyName)?.Value;

}

[XmlType("PropertyIsNull", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsNull : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public OgcExpression Expression { get; set; }

    public override string? GetPropertyName() => (Expression as OgcPropertyName)?.Value;

}

[XmlType("PropertyIsNil", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsNil : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public OgcExpression Expression { get; set; }

    [XmlAttribute("nilReason")]
    public string NilReason { get; set; }

    public override string? GetPropertyName() => (Expression as OgcPropertyName)?.Value;
}

[XmlType("PropertyIsBetween", Namespace = SldNamespaces.OGC)]
public class OgcPropertyIsBetween : OgcComparisonOperator
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public OgcExpression Expression { get; set; }

    [XmlElement("LowerBoundary")]
    public LowerBoundary LowerBoundary { get; set; }

    [XmlElement("UpperBoundary")]
    public UpperBoundary UpperBoundary { get; set; }

    public override string? GetPropertyName() => (Expression as OgcPropertyName)?.Value;
}

public class LowerBoundary
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public OgcExpression Expression { get; set; }
}

public class UpperBoundary
{
    [XmlElement("Literal", typeof(OgcLiteral))]
    [XmlElement("PropertyName", typeof(OgcPropertyName))]
    [XmlElement("Function", typeof(OgcFunction))]
    [XmlElement("Add", typeof(OgcAdd))]
    [XmlElement("Sub", typeof(OgcSub))]
    [XmlElement("Mul", typeof(OgcMul))]
    [XmlElement("Div", typeof(OgcDiv))]
    public OgcExpression Expression { get; set; }
}



using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Sta.Ogc;
 

[XmlType("AbstractQueryExpression", Namespace = SldNamespaces.FES)]
public abstract class AbstractQueryExpression
{
    [XmlAttribute("handle")]
    public string Handle { get; set; }
}

[XmlType("AbstractAdhocQueryExpression", Namespace = SldNamespaces.FES)]
public abstract class AbstractAdhocQueryExpression : AbstractQueryExpression
{
    [XmlElement("AbstractProjectionClause")]
    public List<object> ProjectionClauses { get; set; } = new();

    [XmlElement("AbstractSelectionClause")]
    public object SelectionClause { get; set; }

    [XmlElement("AbstractSortingClause")]
    public object SortingClause { get; set; }

    [XmlAttribute("typeNames")]
    public string TypeNames { get; set; }

    [XmlAttribute("aliases")]
    public string Aliases { get; set; }
} 

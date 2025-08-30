using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Sta.Ogc;

 

public abstract class LogicalOperator : OgcFilterBase { }

[XmlType("And", Namespace = SldNamespaces.OGC)]
public class OgcAnd : LogicalOperator
{
    [XmlElement("PropertyIsEqualTo", typeof(OgcPropertyIsEqualTo))]
    [XmlElement("PropertyIsNotEqualTo", typeof(OgcPropertyIsNotEqualTo))]
    [XmlElement("PropertyIsLessThan", typeof(OgcPropertyIsLessThan))]
    [XmlElement("PropertyIsGreaterThan", typeof(OgcPropertyIsGreaterThan))]
    [XmlElement("PropertyIsLessThanOrEqualTo", typeof(OgcPropertyIsLessThanOrEqualTo))]
    [XmlElement("PropertyIsGreaterThanOrEqualTo", typeof(OgcPropertyIsGreaterThanOrEqualTo))]
    [XmlElement("PropertyIsLike", typeof(OgcPropertyIsLike))]
    [XmlElement("PropertyIsNull", typeof(OgcPropertyIsNull))]
    [XmlElement("PropertyIsNil", typeof(OgcPropertyIsNil))]
    [XmlElement("PropertyIsBetween", typeof(OgcPropertyIsBetween))]
    [XmlElement("And", typeof(OgcAnd))]
    [XmlElement("Or", typeof(OgcOr))]
    [XmlElement("Not", typeof(OgcNot))]
    [XmlElement("Equals", typeof(OgcEqualsSpatialy))]
    [XmlElement("Disjoint", typeof(OgcDisjoint))]
    [XmlElement("Touches", typeof(OgcTouches))]
    [XmlElement("Within", typeof(OgcWithin))]
    [XmlElement("Overlaps", typeof(OgcOverlaps))]
    [XmlElement("Crosses", typeof(OgcCrosses))]
    [XmlElement("Intersects", typeof(OgcIntersects))]
    [XmlElement("Contains", typeof(OgcContains))]
    [XmlElement("DWithin", typeof(OgcDWithin))]
    [XmlElement("Beyond", typeof(OgcBeyond))]
    [XmlElement("BBOX", typeof(OgcBBOX))]
    public List<OgcFilterBase> Predicates { get; set; } = new();

}

[XmlType("Or", Namespace = SldNamespaces.OGC)]
public class OgcOr : LogicalOperator
{
    [XmlElement("PropertyIsEqualTo", typeof(OgcPropertyIsEqualTo))]
    [XmlElement("PropertyIsNotEqualTo", typeof(OgcPropertyIsNotEqualTo))]
    [XmlElement("PropertyIsLessThan", typeof(OgcPropertyIsLessThan))]
    [XmlElement("PropertyIsGreaterThan", typeof(OgcPropertyIsGreaterThan))]
    [XmlElement("PropertyIsLessThanOrEqualTo", typeof(OgcPropertyIsLessThanOrEqualTo))]
    [XmlElement("PropertyIsGreaterThanOrEqualTo", typeof(OgcPropertyIsGreaterThanOrEqualTo))]
    [XmlElement("PropertyIsLike", typeof(OgcPropertyIsLike))]
    [XmlElement("PropertyIsNull", typeof(OgcPropertyIsNull))]
    [XmlElement("PropertyIsNil", typeof(OgcPropertyIsNil))]
    [XmlElement("PropertyIsBetween", typeof(OgcPropertyIsBetween))]
    [XmlElement("And", typeof(OgcAnd))]
    [XmlElement("Or", typeof(OgcOr))]
    [XmlElement("Not", typeof(OgcNot))]
    [XmlElement("Equals", typeof(OgcEqualsSpatialy))]
    [XmlElement("Disjoint", typeof(OgcDisjoint))]
    [XmlElement("Touches", typeof(OgcTouches))]
    [XmlElement("Within", typeof(OgcWithin))]
    [XmlElement("Overlaps", typeof(OgcOverlaps))]
    [XmlElement("Crosses", typeof(OgcCrosses))]
    [XmlElement("Intersects", typeof(OgcIntersects))]
    [XmlElement("Contains", typeof(OgcContains))]
    [XmlElement("DWithin", typeof(OgcDWithin))]
    [XmlElement("Beyond", typeof(OgcBeyond))]
    [XmlElement("BBOX", typeof(OgcBBOX))]
    public List<OgcFilterBase> Predicates { get; set; } = new();
}

[XmlType("Not", Namespace = SldNamespaces.OGC)]
public class OgcNot : LogicalOperator
{
    [XmlElement("PropertyIsEqualTo", typeof(OgcPropertyIsEqualTo))]
    [XmlElement("PropertyIsNotEqualTo", typeof(OgcPropertyIsNotEqualTo))]
    [XmlElement("PropertyIsLessThan", typeof(OgcPropertyIsLessThan))]
    [XmlElement("PropertyIsGreaterThan", typeof(OgcPropertyIsGreaterThan))]
    [XmlElement("PropertyIsLessThanOrEqualTo", typeof(OgcPropertyIsLessThanOrEqualTo))]
    [XmlElement("PropertyIsGreaterThanOrEqualTo", typeof(OgcPropertyIsGreaterThanOrEqualTo))]
    [XmlElement("PropertyIsLike", typeof(OgcPropertyIsLike))]
    [XmlElement("PropertyIsNull", typeof(OgcPropertyIsNull))]
    [XmlElement("PropertyIsNil", typeof(OgcPropertyIsNil))]
    [XmlElement("PropertyIsBetween", typeof(OgcPropertyIsBetween))]
    [XmlElement("And", typeof(OgcAnd))]
    [XmlElement("Or", typeof(OgcOr))]
    [XmlElement("Not", typeof(OgcNot))]
    [XmlElement("Equals", typeof(OgcEqualsSpatialy))]
    [XmlElement("Disjoint", typeof(OgcDisjoint))]
    [XmlElement("Touches", typeof(OgcTouches))]
    [XmlElement("Within", typeof(OgcWithin))]
    [XmlElement("Overlaps", typeof(OgcOverlaps))]
    [XmlElement("Crosses", typeof(OgcCrosses))]
    [XmlElement("Intersects", typeof(OgcIntersects))]
    [XmlElement("Contains", typeof(OgcContains))]
    [XmlElement("DWithin", typeof(OgcDWithin))]
    [XmlElement("Beyond", typeof(OgcBeyond))]
    [XmlElement("BBOX", typeof(OgcBBOX))]
    public object Predicate { get; set; }
}
 
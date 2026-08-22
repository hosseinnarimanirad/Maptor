using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Core.Ogc;
 

[XmlRoot("Filter", Namespace = SldNamespaces.OGC)]
public class OgcFilter
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

    [XmlElement("Equals", typeof(OgcEqualsSpatially))]
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

    [XmlElement("After", typeof(OgcAfter))]
    [XmlElement("Before", typeof(OgcBefore))]
    [XmlElement("Begins", typeof(OgcBegins))]
    [XmlElement("BegunBy", typeof(OgcBegunBy))]
    [XmlElement("TContains", typeof(OgcTContains))]
    [XmlElement("During", typeof(OgcDuring))]
    [XmlElement("EndedBy", typeof(OgcEndedBy))]
    [XmlElement("Ends", typeof(OgcEnds))]
    [XmlElement("TEquals", typeof(OgcTEquals))]
    [XmlElement("Meets", typeof(OgcMeets))]
    [XmlElement("MetBy", typeof(OgcMetBy))]
    [XmlElement("TOverlaps", typeof(OgcTOverlaps))]
    [XmlElement("OverlappedBy", typeof(OgcOverlappedBy))]
    [XmlElement("AnyInteracts", typeof(OgcAnyInteracts))]
    //[XmlElement("ResourceId", typeof(ResourceId))]
    //[XmlElement("Function", typeof(Function))]
    public OgcFilterBase Predicate { get; set; }


    //[XmlNamespaceDeclarations]
    //public XmlSerializerNamespaces Xmlns { get; set; }

    public OgcFilter()
    {
        //Xmlns = new XmlSerializerNamespaces();
        //Xmlns.Add("fes", SldNamespaces.FES);
        //Xmlns.Add("ogc", SldNamespaces.OGC);
    }
}

public abstract class OgcFilterBase { }
 
  
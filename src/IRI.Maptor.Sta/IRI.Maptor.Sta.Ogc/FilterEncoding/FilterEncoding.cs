using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Sta.Ogc;
 

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
 
 

//#region Serialization Helper

//public static class FilterSerializer
//{
//    public static string Serialize(Filter filter)
//    {
//        var serializer = new XmlSerializer(typeof(Filter));
//        var namespaces = new XmlSerializerNamespaces();
//        namespaces.Add("fes", SldNamespaces.FES);
//        namespaces.Add("ogc", SldNamespaces.OGC);

//        using (var writer = new System.IO.StringWriter())
//        {
//            serializer.Serialize(writer, filter, namespaces);
//            return writer.ToString();
//        }
//    }

//    public static Filter Deserialize(string xml)
//    {
//        var serializer = new XmlSerializer(typeof(Filter));
//        using (var reader = new System.IO.StringReader(xml))
//        {
//            return (Filter)serializer.Deserialize(reader);
//        }
//    }

//    public static string SerializeCapabilities(FilterCapabilities capabilities)
//    {
//        var serializer = new XmlSerializer(typeof(FilterCapabilities));
//        var namespaces = new XmlSerializerNamespaces();
//        namespaces.Add("ogc", SldNamespaces.OGC);

//        using (var writer = new System.IO.StringWriter())
//        {
//            serializer.Serialize(writer, capabilities, namespaces);
//            return writer.ToString();
//        }
//    }

//    public static FilterCapabilities DeserializeCapabilities(string xml)
//    {
//        var serializer = new XmlSerializer(typeof(FilterCapabilities));
//        using (var reader = new System.IO.StringReader(xml))
//        {
//            return (FilterCapabilities)serializer.Deserialize(reader);
//        }
//    }
//}

//#endregion

//#region Usage Examples

//public static class FilterExamples
//{
//    public static Filter CreateSimplePropertyFilter(string propertyName, string value)
//    {
//        return new Filter
//        {
//            Predicate = new PropertyIsEqualTo
//            {
//                Expressions = new List<Expression>
//                {
//                        new PropertyName { Value = propertyName },
//                        new Literal { Value = value }
//                }
//            }
//        };
//    }



//    public static Filter CreateLogicalAndFilter(params object[] predicates)
//    {
//        return new Filter
//        {
//            Predicate = new And { Predicates = predicates }
//        };
//    }

//    public static Filter CreateLogicalOrFilter(params object[] predicates)
//    {
//        return new Filter
//        {
//            Predicate = new Or { Predicates = predicates }
//        };
//    }
//}

//#endregion

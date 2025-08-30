using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc;

public enum ComparisonOperatorType
{
    [XmlEnum("LessThan")] 
    LessThan,

    [XmlEnum("GreaterThan")] 
    GreaterThan,

    [XmlEnum("LessThanEqualTo")] 
    LessThanEqualTo,

    [XmlEnum("GreaterThanEqualTo")] 
    GreaterThanEqualTo,

    [XmlEnum("EqualTo")] 
    EqualTo,

    [XmlEnum("NotEqualTo")] 
    NotEqualTo,

    [XmlEnum("Like")] 
    Like,

    [XmlEnum("Between")] 
    Between,

    [XmlEnum("NullCheck")] 
    NullCheck

}
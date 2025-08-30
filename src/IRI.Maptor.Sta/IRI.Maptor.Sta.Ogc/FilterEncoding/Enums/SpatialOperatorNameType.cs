using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc;

public enum SpatialOperatorNameType
{
    [XmlEnum("BBOX")]
    BBOX,

    [XmlEnum("Equals")]
    Equals,

    [XmlEnum("Disjoint")]
    Disjoint,

    [XmlEnum("Intersects")]
    Intersects,

    [XmlEnum("Touches")]
    Touches,

    [XmlEnum("Crosses")]
    Crosses,

    [XmlEnum("Within")]
    Within,

    [XmlEnum("Contains")]
    Contains,

    [XmlEnum("Overlaps")]
    Overlaps,

    [XmlEnum("Beyond")]
    Beyond,

    [XmlEnum("DWithin")]
    DWithin
}
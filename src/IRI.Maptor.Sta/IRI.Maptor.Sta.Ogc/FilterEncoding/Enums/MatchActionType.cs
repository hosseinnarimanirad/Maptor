using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc;

public enum MatchActionType
{
    [XmlEnum("All")]
    All,

    [XmlEnum("Any")]
    Any,

    [XmlEnum("One")]
    One
}
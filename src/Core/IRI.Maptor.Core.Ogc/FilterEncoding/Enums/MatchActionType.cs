using System.Xml.Serialization;

namespace IRI.Maptor.Core.Ogc;

public enum MatchActionType
{
    [XmlEnum("All")]
    All,

    [XmlEnum("Any")]
    Any,

    [XmlEnum("One")]
    One
}
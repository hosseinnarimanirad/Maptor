using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace IRI.Maptor.Sta.Ogc.SLD;

public class Mark
{
    // Keep WellKnownName as string for maximal compatibility;
    // an overload helper is provided to set using enum.
    [XmlElement("WellKnownName")]
    public string WellKnownName { get; set; }

    [XmlElement("Fill")]
    public Fill Fill { get; set; }

    [XmlElement("Stroke")]
    public Stroke Stroke { get; set; }

    //public void SetWellKnownName(WellKnownMark mark) => WellKnownName = mark.ToString();


    [XmlIgnore]
    public WellKnownMark? WellKnownNameValue
    {
        get => Enum.TryParse<WellKnownMark>(WellKnownName, true, out var result) ? result : WellKnownMark.square;
        set
        {
            if (value.HasValue)
            {
                WellKnownName = value.Value.ToString().ToLowerInvariant();
            }
        }
    }
}
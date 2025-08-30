using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc.SLD;

/// <summary>
/// The weight of the font. 
/// Options are normal and bold. 
/// Default is normal.
/// </summary>
public enum Sld_FontWeight
{
    [XmlEnum("normal")] Normal,
    [XmlEnum("bold")] Bold
}
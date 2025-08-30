using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc.SLD;

/// <summary>
/// The style of the font. 
/// Options are 
///     normal, 
///     italic, 
///     and oblique. 
/// Default is normal.
/// </summary>
public enum Sld_FontStyle
{
    [XmlEnum("normal")] Normal,
    [XmlEnum("italic")] Italic,
    [XmlEnum("oblique")] Oblique
}

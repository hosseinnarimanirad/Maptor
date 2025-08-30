using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc.SLD;

/// <summary>
/// Determines how lines are rendered at their ends. 
/// Possible values are 
///     butt (sharp square edge), 
///     round (rounded edge), 
///     and square (slightly elongated square edge). 
/// Default is butt.
/// </summary>
public enum Sld_StrokeLineCap
{
    /// <summary>
    /// butt (sharp square edge)
    /// </summary>
    [XmlEnum("butt")] Butt,

    /// <summary>
    /// round (rounded edge)
    /// </summary>
    [XmlEnum("round")] Round,

    /// <summary>
    /// square (slightly elongated square edge)
    /// </summary>
    [XmlEnum("square")] Square
}

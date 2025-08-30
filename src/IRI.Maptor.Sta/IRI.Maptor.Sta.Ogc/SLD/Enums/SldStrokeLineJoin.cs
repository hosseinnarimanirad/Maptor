using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc.SLD;

/// <summary>
/// Determines how lines are rendered at intersections of line segments. 
/// Possible values are 
///     mitre (sharp corner), 
///     round (rounded corner), 
///     and bevel (diagonal corner). 
/// Default is mitre.
/// </summary>
public enum Sld_StrokeLineJoin
{
    /// <summary>
    /// mitre (sharp corner), 
    /// </summary>
    [XmlEnum("mitre")] Mitre,

    /// <summary>
    /// round (rounded corner)
    /// </summary>
    [XmlEnum("round")] Round,

    /// <summary>
    /// diagonal corner
    /// </summary>
    [XmlEnum("bevel")] Bevel
}
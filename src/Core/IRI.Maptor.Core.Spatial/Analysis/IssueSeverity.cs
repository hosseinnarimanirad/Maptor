namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// How much a reported data issue matters. Used by every checker that reports issues about features, such
/// as the points-on-lines topology.
/// </summary>
public enum IssueSeverity
{
    /// <summary>Worth knowing; the data may well be right (e.g. a point shared by two lines).</summary>
    Info = 0,

    /// <summary>Probably a data problem; results built on the data are still usable.</summary>
    Warning = 1,

    /// <summary>The data is wrong; whatever depends on it is unreliable.</summary>
    Error = 2,
}
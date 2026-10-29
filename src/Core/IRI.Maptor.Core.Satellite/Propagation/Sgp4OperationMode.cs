namespace IRI.Maptor.Core.Satellite.Propagation;

/// <summary>
/// Vallado's "operation mode": two readings of the original code that differ for deep-space orbits only
/// (the sidereal angle at epoch, and a quadrant fix in the lunar-solar periodics). Near-Earth results
/// are the same in both.
/// </summary>
public enum Sgp4OperationMode
{
    /// <summary>The improved mode ('i'): a modern sidereal time. Vallado's recommendation and the usual default.</summary>
    Improved,

    /// <summary>The AFSPC mode ('a'): reproduces the original Air Force code, quirks included.</summary>
    Afspc,
}

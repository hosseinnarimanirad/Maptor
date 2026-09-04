namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

/// <summary>
/// How many digits an MGRS reference carries per axis, and therefore the size of the square
/// it names. <c>39S WV 5</c> is a 10 km square; <c>39S WV 53516 39501</c> is a 1 m square.
/// </summary>
public enum MgrsPrecision
{
    /// <summary>No digits — the 100 km square itself, e.g. <c>39S WV</c>.</summary>
    Km100 = 0,

    /// <summary>One digit per axis, e.g. <c>39S WV 5 3</c>.</summary>
    Km10 = 1,

    /// <summary>Two digits per axis, e.g. <c>39S WV 53 39</c>.</summary>
    Km1 = 2,

    /// <summary>Three digits per axis, e.g. <c>39S WV 535 395</c>.</summary>
    M100 = 3,

    /// <summary>Four digits per axis, e.g. <c>39S WV 5351 3950</c>.</summary>
    M10 = 4,

    /// <summary>Five digits per axis, e.g. <c>39S WV 53516 39501</c>.</summary>
    M1 = 5,
}

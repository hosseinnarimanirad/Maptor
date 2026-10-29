namespace IRI.Maptor.Core.Spatial.Primitives;

/// <summary>
/// How <see cref="Geometry{T}.OffsetCurve"/> joins two offset segments on the outer side of a bend.
/// On the inner side both styles use the exact corner (the intersection of the two offset lines).
/// </summary>
public enum OffsetCurveJoin
{
    /// <summary>A circular arc around the vertex, as <see cref="Geometry{T}.Buffer"/> draws it.</summary>
    Round = 0,

    /// <summary>
    /// A sharp corner where the two offset lines meet, like a parallel line drawn on a plan. A
    /// corner farther than 4 × distance from the vertex (a bend sharper than ~29°) is bevelled
    /// instead of drawn as a spike.
    /// </summary>
    Mitre = 1,
}

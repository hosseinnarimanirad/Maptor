namespace IRI.Maptor.Core.Spatial.Analysis;

public class SimplificationParameters
{
    // 1401.02.26
    // used in n-th point simplification methods
    public uint N { get; set; }

    public double? DistanceThreshold { get; set; }

    public double? AreaThreshold { get; set; }

    /// <summary>
    /// Square Cosine of angle should be provided, must be between 0 and 1
    /// </summary>
    public double? AngleThreshold { get; set; }

    public bool Retain3Points { get; set; } = false;


    // used in Lang algorithm
    public int? LookAhead { get; set; }

    public double? AverageLatitude { get; set; }

    /// <summary>
    /// The point list is a closed ring stored without its repeated closing vertex (the convention of polygon rings in
    /// <see cref="Primitives.Geometry{T}"/>): the last point is joined back to the first. Used by Visvalingam-Whyatt
    /// (every vertex but the first may be eliminated) and Ramer-Douglas-Peucker (the ring is divided at the vertex
    /// farthest from the first one, Douglas and Peucker 1973, p. 117). <see cref="Primitives.Geometry{T}.Simplify(SimplificationType, SimplificationParameters)"/>
    /// sets it for the rings of polygons on its own copy of the parameters; callers of <see cref="Simplifications"/>
    /// set it themselves. Added 1405.07.14 (2026-10-06).
    /// </summary>
    public bool IsRing { get; set; } = false;

    /// <summary>A shallow copy, so that a caller's parameters object is never changed.</summary>
    public SimplificationParameters Clone() => (SimplificationParameters)MemberwiseClone();
}

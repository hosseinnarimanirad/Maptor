namespace IRI.Maptor.Core.FileGdb.Enums;

/// <summary>
/// Geometry type declared for a whole table (low byte of the layer flags in the field description header).
/// </summary>
public enum GdbTableGeometryType : byte
{
    None = 0,
    Point = 1,
    Multipoint = 2,
    Polyline = 3,
    Polygon = 4,
    Envelope = 5,
    Path = 6,
    Any = 7,
    Multipatch = 9,
    Ring = 11,
    Line = 13,
    CircularArc = 14,
    BezierCurve = 15,
    EllipticCurve = 16,
    GeometryCollection = 17,
    TriangleStrip = 18,
    TriangleFan = 19,
    Ray = 20,
    Sphere = 21,
    Tin = 22,
}

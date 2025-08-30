using System.Xml.Serialization;

namespace IRI.Maptor.Sta.Ogc;

public enum GeometryOperandType
{
    [XmlEnum("gml:Envelope")] 
    Envelope,

    [XmlEnum("gml:Point")] 
    Point,

    [XmlEnum("gml:LineString")] 
    LineString,

    [XmlEnum("gml:Polygon")] 
    Polygon,

    [XmlEnum("gml:ArcByCenterPoint")] 
    ArcByCenterPoint,
    
    [XmlEnum("gml:CircleByCenterPoint")] 
    CircleByCenterPoint,
    
    [XmlEnum("gml:Arc")] 
    Arc,
    
    [XmlEnum("gml:Circle")] 
    Circle,
    
    [XmlEnum("gml:ArcByBulge")] 
    ArcByBulge,
    
    [XmlEnum("gml:Bezier")] 
    Bezier,
    
    [XmlEnum("gml:Clothoid")] 
    Clothoid,
    
    [XmlEnum("gml:CubicSpline")] 
    CubicSpline,
    
    [XmlEnum("gml:Geodesic")] 
    Geodesic,
    
    [XmlEnum("gml:OffsetCurve")] 
    OffsetCurve,
    
    [XmlEnum("gml:Triangle")] 
    Triangle,
    
    [XmlEnum("gml:PolyhedralSurface")] 
    PolyhedralSurface,
    
    [XmlEnum("gml:TriangulatedSurface")] 
    TriangulatedSurface,
    
    [XmlEnum("gml:Tin")] 
    Tin,
    
    [XmlEnum("gml:Solid")] 
    Solid
}

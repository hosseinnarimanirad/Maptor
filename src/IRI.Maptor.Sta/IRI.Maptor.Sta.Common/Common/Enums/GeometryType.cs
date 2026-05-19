using IRI.Maptor.Sta.Common.Attributes;

namespace IRI.Maptor.Sta.Common.Enums;

public enum GeometryType
{
    [GeometryAttribute(GeometryCategory.Point, isMultiPart: false, isRingBase: false)]
    Point = 1,

    [GeometryAttribute(GeometryCategory.Polyline, isMultiPart: false, isRingBase: false)]
    LineString = 2,

    [GeometryAttribute(GeometryCategory.Polygon, isMultiPart: false, isRingBase: true)]
    Polygon = 3,

    [GeometryAttribute(GeometryCategory.Point, isMultiPart: true, isRingBase: false)]
    MultiPoint = 4,

    [GeometryAttribute(GeometryCategory.Polyline, isMultiPart: true, isRingBase: false)]
    MultiLineString = 5,

    [GeometryAttribute(GeometryCategory.Polygon, isMultiPart: true, isRingBase: true)]
    MultiPolygon = 6,

    [GeometryAttribute(GeometryCategory.None, isMultiPart: false, isRingBase: false)]
    GeometryCollection = 7,

    [GeometryAttribute(GeometryCategory.Polyline, isMultiPart: false, isRingBase: false)]
    CircularString = 8,

    [GeometryAttribute(GeometryCategory.Polyline, isMultiPart: false, isRingBase: false)]
    CompoundCurve = 9,

    [GeometryAttribute(GeometryCategory.Polygon, isMultiPart: false, isRingBase: true)]
    CurvePolygon = 10,

    [GeometryAttribute(GeometryCategory.None, isMultiPart: false, isRingBase: false)]
    None = 100,
}
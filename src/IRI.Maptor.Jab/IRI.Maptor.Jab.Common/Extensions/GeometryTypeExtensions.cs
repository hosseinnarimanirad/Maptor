using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Extensions;

public static class GeometryTypeExtensions
{
    public static LayerType? AsLayerType(this GeometryType? geometryType)
    {
        if (geometryType is null)
            return null;

        switch (geometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return LayerType.Point;

            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return LayerType.Polyline;

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return LayerType.Polygon;

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
                return null;

            default:
                throw new NotImplementedException("GeometryTypeExtensions > AsLayerType");
        }
    }
}

using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Extensions;

public static class GeometryTypeExtensions
{
    public static SpatialModelMode AsLayerType(this GeometryType? geometryType)
    {
        if (geometryType is null)
            return SpatialModelMode.None;

        switch (geometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return SpatialModelMode.Point;

            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return SpatialModelMode.Polyline;

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return SpatialModelMode.Polygon;

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
                return SpatialModelMode.None;

            default:
                throw new NotImplementedException("GeometryTypeExtensions > AsLayerType");
        }
    }
}

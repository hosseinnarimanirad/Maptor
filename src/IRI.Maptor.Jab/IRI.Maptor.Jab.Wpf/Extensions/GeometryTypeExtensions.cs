using System;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.Common.Enums;

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

    public static DrawMode AsDrawMode(this GeometryType geometryType)
    { 
        return geometryType switch
        {
            GeometryType.Point or GeometryType.MultiPoint => DrawMode.Point,
            GeometryType.LineString or GeometryType.MultiLineString => DrawMode.Polyline,

            GeometryType.Polygon or GeometryType.MultiPolygon => DrawMode.Polygon,

            _ => DrawMode.Rectangle
        };
    }
}

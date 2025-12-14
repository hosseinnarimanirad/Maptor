using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using System.Drawing;

namespace IRI.Maptor.Extensions;

public static class GeometryExtensions
{
    #region Geometry > Esri Shape

    public static EsriShapeBase? AsEsriShape<T>(this Geometry<T> geometry, int? srid = null, Func<IPoint, IPoint> mapFunction = null) where T : IPoint, new()
    {
        if (geometry.IsNullOrEmpty())
            return null;

        var targetSrid = srid ?? geometry.Srid;

        var type = geometry.Type;

        switch (type)
        {
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();

            case GeometryType.Point:
                return PointToEsriPoint(geometry, targetSrid, mapFunction);

            case GeometryType.MultiPoint:
                return MultiPointToEsriMultiPoint(geometry, targetSrid, mapFunction);

            case GeometryType.LineString:
                return LineStringToEsriPolyline(geometry, targetSrid, mapFunction);

            case GeometryType.MultiLineString:
                return MultiLineStringToEsriPolyline(geometry, targetSrid, mapFunction);

            case GeometryType.Polygon:
                return PolygonToEsriPolygon(geometry, targetSrid, mapFunction);

            case GeometryType.MultiPolygon:
                return MultiPolygonToEsriPolygon(geometry, targetSrid, mapFunction);
        }
    }

    public static EsriPoint AsEsriPoint<T>(this Geometry<T> point, int srid) where T : IPoint, new()
    {
        if (point.IsNullOrEmpty())
        {
            return new EsriPoint(double.NaN, double.NaN, 0);
        }
        else
        {
            return new EsriPoint(point.Points.First().X, point.Points.First().Y, srid);
        }
    }

    //Not supportig Z and M Values
    private static EsriPoint PointToEsriPoint<T>(Geometry<T> point, int srid, Func<IPoint, IPoint> mapFunction) where T : IPoint, new()
    {
        var esriPoint = point.AsEsriPoint(srid);

        return mapFunction == null ? esriPoint : (EsriPoint)mapFunction(esriPoint);
    }

    //Not supportig Z and M Values
    private static EsriMultiPoint MultiPointToEsriMultiPoint<T>(Geometry<T> multiPoint, int srid, Func<IPoint, IPoint> mapFunction) where T : IPoint, new()
    {
        if (multiPoint.IsNullOrEmpty())
        {
            return new EsriMultiPoint();
        }

        int numberOfGeometries = multiPoint.NumberOfGeometries;

        List<EsriPoint> points = new List<EsriPoint>(multiPoint.NumberOfPoints);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            points.AddRange(GetPoints(multiPoint.Geometries[i], srid, mapFunction, isRing: false));
        }

        return new EsriMultiPoint(points.ToArray());
    }

    //Not supporting Z and M values
    private static EsriPolyline LineStringToEsriPolyline<T>(Geometry<T> lineString, int srid, Func<IPoint, IPoint> mapFunction) where T : IPoint, new()
    {
        if (lineString.IsNullOrEmpty())
        {
            return new EsriPolyline();
        }

        return new EsriPolyline(GetPoints(lineString, srid, mapFunction, isRing: false).ToArray());
    }

    //Not supporting Z and M values
    private static EsriPolyline MultiLineStringToEsriPolyline<T>(Geometry<T> multiLineString, int srid, Func<IPoint, IPoint> mapFunction) where T : IPoint, new()
    {
        if (multiLineString.IsNullOrEmpty())
        {
            return new EsriPolyline();
        }

        int numberOfGeometries = multiLineString.NumberOfGeometries;

        List<EsriPoint> points = new List<EsriPoint>(multiLineString.NumberOfPoints);

        List<int> parts = new List<int>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            parts.Add(points.Count);

            points.AddRange(GetPoints(multiLineString.Geometries[i], srid, mapFunction, isRing: false));
        }

        return new EsriPolyline(points.ToArray(), parts.ToArray());
    }

    //Not supporting Z and M values
    //check for cw and cww criteria
    private static EsriPolygon PolygonToEsriPolygon<T>(Geometry<T> polygon, int srid, Func<IPoint, IPoint> mapFunction) where T : IPoint, new()
    {
        if (polygon.IsNullOrEmpty())
        {
            return new EsriPolygon();
        }

        int numberOfRings = polygon.NumberOfGeometries;

        List<EsriPoint> points = new List<EsriPoint>(polygon.TotalNumberOfPoints);

        List<int> parts = new List<int>(numberOfRings);

        for (int i = 0; i < numberOfRings; i++)
        {
            parts.Add(points.Count);

            points.AddRange(GetPoints(polygon.Geometries[i], srid, mapFunction, isRing: true));
        }

        return new EsriPolygon(points.ToArray(), parts.ToArray());
    }

    //Not supporting Z and M values
    //check for cw and cww criteria
    private static EsriPolygon MultiPolygonToEsriPolygon<T>(Geometry<T> multiPolygon, int srid, Func<IPoint, IPoint> mapFunction) where T : IPoint, new()
    {
        if (multiPolygon.IsNullOrEmpty())
        {
            return new EsriPolygon();
        }

        int numberOfGeometries = multiPolygon.NumberOfGeometries;

        List<EsriPoint> points = new List<EsriPoint>(multiPolygon.NumberOfPoints);

        List<int> parts = new List<int>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var tempPolygon = multiPolygon.Geometries[i];

            var numberOfRings = tempPolygon == null ? 0 : tempPolygon.NumberOfGeometries;

            for (int j = 0; j < numberOfRings; j++)
            {
                parts.Add(points.Count);

                points.AddRange(GetPoints(tempPolygon.Geometries[j], srid, mapFunction, isRing: true));
            }
        }

        return new EsriPolygon(points.ToArray(), parts.ToArray());

    }

    private static IEnumerable<EsriPoint> GetPoints<T>(Geometry<T> geometry, int srid, Func<IPoint, IPoint> mapFunction, bool isRing) where T : IPoint, new()
    {
        if (geometry.IsNullOrEmpty())
            return Enumerable.Empty<EsriPoint>();

        List<EsriPoint> result = new List<EsriPoint>(geometry.Points.Count);

        if (mapFunction is null)
        {
            for (int i = 0; i < geometry.Points.Count; i++)
            {
                result.Add(new EsriPoint(geometry.Points[i].X, geometry.Points[i].Y, srid));
            }
        }
        else
        {
            for (int i = 0; i < geometry.Points.Count; i++)
            {
                var temporaryPoint = mapFunction(geometry.Points[i]);

                result.Add(new EsriPoint(temporaryPoint.X, temporaryPoint.Y, srid));
            }
        }

        // The rings are closed (the first and last vertex of a ring MUST be the same).
        // ESRI Shapefile Technical Description, page 9
        if (isRing)
        {
            result.Add(result[0]);
        }

        return result;
    }

    #endregion

    public static void SaveAsShapefile<T>(this Feature<T> feature, string shpFileName, SrsBase srs = null) where T : IPoint, new()
    {
        if (feature == null)
            return;

        IRI.Maptor.Sta.ShapefileFormat.Shapefile.SaveAsShapefile(shpFileName, new List<Feature<T>>() { feature }, srs);
    }

    public static void SaveAsShapefile<T>(this IEnumerable<Feature<T>> features, string shpFileName, SrsBase srs = null) where T : IPoint, new()
    {
        if (features.IsNullOrEmpty())
            return;

        IRI.Maptor.Sta.ShapefileFormat.Shapefile.SaveAsShapefile(shpFileName, features, srs);
    }

}

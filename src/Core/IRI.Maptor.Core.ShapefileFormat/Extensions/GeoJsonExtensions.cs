using System;
using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.ShapefileFormat.EsriType;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Extensions;

public static class GeoJsonExtensions
{
    #region IGeoJsonGeometry > Esri Shape

    public static EsriShapeBase AsEsriShape(this IGeoJsonGeometry geometry, bool isLongitudeFirst = true, int srid = 0, Func<IPoint, IPoint> mapFunction = null)
    {
        //var type = geometry.GeometryType;
        //if (geometry.IsNullOrEmpty())
        //{
        //    return SqlSpatialHelper.CreateEmptySqlGeometry(type, srid);
        //}

        if (geometry.IsNullOrEmpty())
        {
            return null;
        }

        var type = geometry.GeometryType;

        switch (type)
        {
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();

            case GeometryType.Point:
                return ToEsriPoint((GeoJsonPoint)geometry, isLongitudeFirst, srid, mapFunction);

            case GeometryType.MultiPoint:
                return ToEsriMultiPoint((GeoJsonMultiPoint)geometry, isLongitudeFirst, srid, mapFunction);

            case GeometryType.LineString:
                return ToEsriPolyline((GeoJsonLineString)geometry, isLongitudeFirst, srid, mapFunction);

            case GeometryType.MultiLineString:
                return ToEsriPolyline((GeoJsonMultiLineString)geometry, isLongitudeFirst, srid, mapFunction);

            case GeometryType.Polygon:
                return ToEsriPolygon((GeoJsonPolygon)geometry, isLongitudeFirst, srid, mapFunction);

            case GeometryType.MultiPolygon:
                return ToEsriPolygon((GeoJsonMultiPolygon)geometry, isLongitudeFirst, srid, mapFunction);
        }
    }

    public static EsriPoint AsEsriPoint(this GeoJsonPoint point, bool isLongitudeFirst, int srid)
    {
        if (point.IsNullOrEmpty())
        {
            return new EsriPoint(double.NaN, double.NaN, 0);
        }
        else
        {
            var temporaryPoint = IRI.Maptor.Core.Common.Primitives.Point.Parse(point.Coordinates, isLongitudeFirst);

            return new EsriPoint(temporaryPoint.X, temporaryPoint.Y, srid);
        }
    }

    //Not supporting Z and M Values
    private static EsriPoint ToEsriPoint(GeoJsonPoint geometry, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        var point = geometry.AsEsriPoint(isLongitudeFirst, srid);

        return mapFunction == null ? point : (EsriPoint)mapFunction(point);
    }

    //Not supporting Z and M Values
    private static EsriMultiPoint ToEsriMultiPoint(GeoJsonMultiPoint geometry, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriMultiPoint();
        }

        // 1400.02.03
        // number of points equals to number of geometries in multipoint
        //List<EsriPoint> points = new List<EsriPoint>(geometry.Coordinates.Length);

        //foreach (var item in geometry.Coordinates)
        //{
        //    IPoint temporaryPoint = IRI.Maptor.Core.Common.Primitives.Point.Parse(item, isLongitudeFirst);

        //    if (mapFunction != null)
        //    {
        //        temporaryPoint = mapFunction(temporaryPoint);
        //    }

        //    points.Add(new EsriPoint(temporaryPoint.X, temporaryPoint.Y, srid));
        //}

        //return new EsriMultiPoint(points.ToArray());

        return new EsriMultiPoint(GetPoints(geometry.Coordinates, isLongitudeFirst, srid, mapFunction).ToArray());
    }

    //Not supporting Z and M values
    private static EsriPolyline ToEsriPolyline(GeoJsonLineString geometry, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriPolyline();
        }

        //int numberOfGeometries = geometry.STNumGeometries().Value;

        //List<EsriPoint> points = new List<EsriPoint>(geometry.STNumPoints().Value);

        //List<int> parts = new List<int>(numberOfGeometries);

        //for (int i = 0; i < numberOfGeometries; i++)
        //{
        //    int index = i + 1;

        //    parts.Add(points.Count);

        //    points.AddRange(GetPoints(geometry.STGeometryN(index), mapFunction));
        //}

        //return new EsriPolyline(points.ToArray(), parts.ToArray());

        return new EsriPolyline(GetPoints(geometry.Coordinates, isLongitudeFirst, srid, mapFunction).ToArray());
    }

    //Not supporting Z and M values
    private static EsriPolyline ToEsriPolyline(GeoJsonMultiLineString geometry, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriPolyline();
        }

        int numberOfGeometries = geometry.NumberOfGeometries();

        List<EsriPoint> points = new List<EsriPoint>(geometry.NumberOfPoints());

        List<int> parts = new List<int>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            parts.Add(points.Count);

            points.AddRange(GetPoints(geometry.Coordinates[i], isLongitudeFirst, srid, mapFunction));
        }

        return new EsriPolyline(points.ToArray(), parts.ToArray());
    }

    //Not supporting Z and M values
    //check for cw and cww criteria
    private static EsriPolygon ToEsriPolygon(GeoJsonPolygon geometry, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriPolygon();
        }

        int numberOfRings = geometry.NumberOfGeometries();

        List<EsriPoint> points = new List<EsriPoint>(geometry.NumberOfPoints());

        List<int> parts = new List<int>(numberOfRings);

        for (int i = 0; i < numberOfRings; i++)
        {
            parts.Add(points.Count);

            points.AddRange(GetPoints(geometry.Coordinates[i], isLongitudeFirst, srid, mapFunction));
        }

        return new EsriPolygon(points.ToArray(), parts.ToArray());
    }

    //Not supporting Z and M values
    //check for cw and cww criteria
    private static EsriPolygon ToEsriPolygon(GeoJsonMultiPolygon geometry, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriPolygon();
        }

        int numberOfGeometries = geometry.NumberOfGeometries();

        List<EsriPoint> points = new List<EsriPoint>(geometry.NumberOfPoints());

        List<int> parts = new List<int>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            var tempPolygon = geometry.Coordinates[i];

            var numberOfRings = tempPolygon == null ? 0 : tempPolygon.Length;

            for (int j = 0; j < numberOfRings; j++)
            {
                parts.Add(points.Count);

                points.AddRange(GetPoints(tempPolygon[j], isLongitudeFirst, srid, mapFunction));
            }
        }

        return new EsriPolygon(points.ToArray(), parts.ToArray());

        //for (int i = 0; i < numberOfGeometries; i++)
        //{
        //    int index = i + 1;

        //    SqlGeometry tempPolygon = geometry.STGeometryN(index);

        //    var exterior = tempPolygon.STExteriorRing();

        //    if (tempPolygon.IsNullOrEmpty() || exterior.IsNullOrEmpty())
        //        continue;

        //    parts.Add(points.Count);

        //    points.AddRange(GetPoints(exterior, mapFunction));

        //    for (int j = 0; j < tempPolygon.STNumInteriorRing(); j++)
        //    {
        //        var interior = tempPolygon.STInteriorRingN(j + 1);

        //        if (interior.IsNullOrEmpty())
        //            continue;

        //        parts.Add(points.Count);

        //        points.AddRange(GetPoints(interior, mapFunction));
        //    }
        //}

        //return new EsriPolygon(points.ToArray(), parts.ToArray());
    }

    private static IEnumerable<EsriPoint> GetPoints(double[][] coordinates, bool isLongitudeFirst, int srid, Func<IPoint, IPoint> mapFunction)
    {
        if (coordinates.IsNullOrEmpty())
        {
            return null;
        }

        List<EsriPoint> points = new List<EsriPoint>(coordinates.Length);

        foreach (var item in coordinates)
        {
            IPoint temporaryPoint = IRI.Maptor.Core.Common.Primitives.Point.Parse(item, isLongitudeFirst);

            if (mapFunction != null)
            {
                temporaryPoint = mapFunction(temporaryPoint);
            }

            points.Add(new EsriPoint(temporaryPoint.X, temporaryPoint.Y, srid));
        }

        //int numberOfPoints = geometry.Length;

        //EsriPoint[] points = new EsriPoint[numberOfPoints];

        //for (int i = 0; i < numberOfPoints; i++)
        //{
        //    int index = i + 1;

        //    //EsriPoint point = new EsriPoint(geometry.STPointN(index).STX.Value, geometry.STPointN(index).STY.Value);
        //    var point = geometry.STPointN(index).AsEsriPoint();

        //    if (mapFunction == null)
        //    {
        //        points[i] = point;
        //    }
        //    else
        //    {
        //        points[i] = (EsriPoint)mapFunction(point);
        //    }

        //}

        return points;
    }

    #endregion

    public static void SaveAsShapefile(this GeoJsonFeatureSet featureSet, string shpFileName, bool isLongitudeFirst = true, SrsBase srs = null)
    {
        if (featureSet == null || featureSet.Features.IsNullOrEmpty())
            return;

        IRI.Maptor.Core.ShapefileFormat.Shapefile.SaveAsShapefile(shpFileName, featureSet.Features, isLongitudeFirst, srs);
    }

    public static void SaveAsShapefile(this IEnumerable<GeoJsonFeature> features, string shpFileName, bool isLongitudeFirst = true, SrsBase srs = null)
    {
        if (features.IsNullOrEmpty())
            return;

        IRI.Maptor.Core.ShapefileFormat.Shapefile.SaveAsShapefile(shpFileName, features, isLongitudeFirst, srs);
    }
}

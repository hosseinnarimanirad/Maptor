using System.Diagnostics;

using Microsoft.SqlServer.Types;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers;

namespace IRI.Maptor.Extensions;

public static class GeoJsonExtensions
{
    const int minimumPolygonPoints = 3;
    
    #region SqlGeometry

    public static SqlGeometry AsSqlGeometry(this IGeoJsonGeometry geometry, bool isLongitudeFirst = true, int srid = 0)
    {
        var type = geometry.GeometryType;

        if (geometry.IsNullOrEmpty())
        {
            return SqlSpatialHelper.CreateEmptySqlGeometry(type, srid);
        }

        SqlGeometryBuilder builder = new SqlGeometryBuilder();

        builder.SetSrid(srid);

        switch (type)
        {
            case GeometryType.Point:
                AddPoint(builder, (GeoJsonPoint)geometry, isLongitudeFirst);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(builder, (GeoJsonMultiPoint)geometry, isLongitudeFirst);
                break;

            case GeometryType.LineString:
                AddLineString(builder, (GeoJsonLineString)geometry, isLongitudeFirst);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(builder, (GeoJsonMultiLineString)geometry, isLongitudeFirst);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(builder, (GeoJsonMultiPolygon)geometry, isLongitudeFirst);
                break;

            case GeometryType.Polygon:
                AddPolygon(builder, (GeoJsonPolygon)geometry, isLongitudeFirst);
                break;


            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

        return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddPoint(SqlGeometryBuilder builder, GeoJsonPoint point, bool isLongitudeFirst)
    {
        builder.BeginGeometry(OpenGisGeometryType.Point);

        var temporaryPoint = Point.Parse(point.Coordinates, isLongitudeFirst);

        builder.BeginFigure(temporaryPoint.X, temporaryPoint.Y);

        builder.EndFigure();

        builder.EndGeometry();
    }

    private static void AddMultiPoint(SqlGeometryBuilder builder, GeoJsonMultiPoint geometry, bool isLongitudeFirst)
    {
        builder.BeginGeometry(OpenGisGeometryType.MultiPoint);

        foreach (var item in geometry.Coordinates)
        {
            builder.BeginGeometry(OpenGisGeometryType.Point);

            var temporaryPoint = Point.Parse(item, isLongitudeFirst);

            builder.BeginFigure(temporaryPoint.X, temporaryPoint.Y);

            builder.EndFigure();

            builder.EndGeometry();
        }

        builder.EndGeometry();
    }

    private static void AddLineString(SqlGeometryBuilder builder, GeoJsonLineString geometry, bool isLongitudeFirst)
    {
        builder.BeginGeometry(OpenGisGeometryType.LineString);

        AddLineStringOrRing(builder, geometry.Coordinates, false, isLongitudeFirst);

        builder.EndGeometry();
    }

    private static void AddMultiLineString(SqlGeometryBuilder builder, GeoJsonMultiLineString geometry, bool isLongitudeFirst)
    {
        builder.BeginGeometry(OpenGisGeometryType.MultiLineString);

        foreach (var item in geometry.Coordinates)
        {
            builder.BeginGeometry(OpenGisGeometryType.LineString);

            AddLineStringOrRing(builder, item, false, isLongitudeFirst);

            builder.EndGeometry();
        }

        builder.EndGeometry();
    }

    private static void AddPolygon(SqlGeometryBuilder builder, GeoJsonPolygon geometry, bool isLongitudeFirst)
    {
        builder.BeginGeometry(OpenGisGeometryType.Polygon);

        foreach (var item in geometry.Coordinates)
        {
            if (item.Length < minimumPolygonPoints)
            {
                Trace.WriteLine($"CreatePolygon escape!");
                continue;
            }

            AddLineStringOrRing(builder, item, true, isLongitudeFirst);
        }

        builder.EndGeometry();
    }

    private static void AddMultiPolygon(SqlGeometryBuilder builder, GeoJsonMultiPolygon geometry, bool isLongitudeFirst)
    {
        builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);

        foreach (var item in geometry.Coordinates)
        {
            builder.BeginGeometry(OpenGisGeometryType.Polygon);

            foreach (var lines in item)
            {
                if (lines.Length < minimumPolygonPoints)
                {
                    Trace.WriteLine($"CreateMultiPolygon escape!");
                    continue;
                }

                AddLineStringOrRing(builder, lines, true, isLongitudeFirst);
            }

            builder.EndGeometry();
        }

        builder.EndGeometry();
    }

    //private static void AddGeometryCollection(SqlGeometryBuilder builder, IGeoJsonGeometry geometry, bool isLongitudeFirst)
    //{ 
    //    builder.BeginGeometry(OpenGisGeometryType.GeometryCollection);

    //    foreach (var item in geometry.Geometries)
    //    {
    //        if (geometry.IsNullOrEmpty())
    //        {
    //            SqlSpatialExtensions.AddEmptySqlGeometry(builder, item.Type);
    //        }

    //        switch (item.Type)
    //        {
    //            case GeometryType.Point:
    //                AddPoint(builder, item);
    //                break;
    //            case GeometryType.LineString:
    //                AddLineString(builder, item);
    //                break;
    //            case GeometryType.Polygon:
    //                AddPolygon(builder, item);
    //                break;
    //            case GeometryType.MultiPoint:
    //                AddMultiPoint(builder, item);
    //                break;
    //            case GeometryType.MultiLineString:
    //                AddMultiLineString(builder, item);
    //                break;
    //            case GeometryType.MultiPolygon:
    //                AddMultiPolygon(builder, item);
    //                break;
    //            case GeometryType.GeometryCollection:
    //            case GeometryType.CircularString:
    //            case GeometryType.CompoundCurve:
    //            case GeometryType.CurvePolygon:
    //            default:
    //                throw new NotImplementedException();
    //        }
    //    }

    //    builder.EndGeometry();             
    //}

    private static void AddLineStringOrRing(SqlGeometryBuilder builder, double[][] geometry, bool isRing, bool isLongitudeFirst)
    {
        var firstPoint = Point.Parse(geometry[0], isLongitudeFirst);

        builder.BeginFigure(firstPoint.X, firstPoint.Y);

        for (int i = 1; i < geometry.Length; i++)
        {
            var point = Point.Parse(geometry[i], isLongitudeFirst);

            builder.AddLine(point.X, point.Y);
        }

        //1399.08.19
        //should not manually repeat the last point
        //if (isRing)
        //{
        //    builder.AddLine(firstPoint.X, firstPoint.Y);
        //}

        builder.EndFigure();
    }

    #endregion


    #region SqlGeography

    public static SqlGeography AsSqlGeography(this IGeoJsonGeometry geometry, bool isLongitudeFirst = true, int srid = 0)
    {
        var type = geometry.GeometryType;

        if (geometry.IsNullOrEmpty())
        {
            return SqlSpatialHelper.CreateEmptySqlGeography(type, srid);
        }

        SqlGeographyBuilder builder = new SqlGeographyBuilder();

        builder.SetSrid(srid);

        switch (type)
        {
            case GeometryType.Point:
                AddPoint(builder, (GeoJsonPoint)geometry, isLongitudeFirst);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(builder, (GeoJsonMultiPoint)geometry, isLongitudeFirst);
                break;

            case GeometryType.LineString:
                AddLineString(builder, (GeoJsonLineString)geometry, isLongitudeFirst);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(builder, (GeoJsonMultiLineString)geometry, isLongitudeFirst);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(builder, (GeoJsonMultiPolygon)geometry, isLongitudeFirst);
                break;

            case GeometryType.Polygon:
                AddPolygon(builder, (GeoJsonPolygon)geometry, isLongitudeFirst);
                break;


            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

        return builder.ConstructedGeography.MakeValid();
    }

    private static void AddPoint(SqlGeographyBuilder builder, GeoJsonPoint point, bool isLongitudeFirst)
    {
        builder.BeginGeography(OpenGisGeographyType.Point);

        var temporaryPoint = Point.Parse(point.Coordinates, isLongitudeFirst);

        builder.BeginFigure(temporaryPoint.Y, temporaryPoint.X);

        builder.EndFigure();

        builder.EndGeography();
    }

    private static void AddMultiPoint(SqlGeographyBuilder builder, GeoJsonMultiPoint geometry, bool isLongitudeFirst)
    {
        builder.BeginGeography(OpenGisGeographyType.MultiPoint);

        foreach (var item in geometry.Coordinates)
        {
            builder.BeginGeography(OpenGisGeographyType.Point);

            var temporaryPoint = Point.Parse(item, isLongitudeFirst);

            builder.BeginFigure(temporaryPoint.Y, temporaryPoint.X);

            builder.EndFigure();

            builder.EndGeography();
        }

        builder.EndGeography();
    }

    private static void AddLineString(SqlGeographyBuilder builder, GeoJsonLineString geometry, bool isLongitudeFirst)
    {
        builder.BeginGeography(OpenGisGeographyType.LineString);

        AddLineStringOrRing(builder, geometry.Coordinates, false, isLongitudeFirst);

        builder.EndGeography();
    }

    private static void AddMultiLineString(SqlGeographyBuilder builder, GeoJsonMultiLineString geometry, bool isLongitudeFirst)
    {
        builder.BeginGeography(OpenGisGeographyType.MultiLineString);

        foreach (var item in geometry.Coordinates)
        {
            builder.BeginGeography(OpenGisGeographyType.LineString);

            AddLineStringOrRing(builder, item, false, isLongitudeFirst);

            builder.EndGeography();
        }

        builder.EndGeography();
    }

    private static void AddPolygon(SqlGeographyBuilder builder, GeoJsonPolygon geometry, bool isLongitudeFirst)
    {
        builder.BeginGeography(OpenGisGeographyType.Polygon);

        foreach (var item in geometry.Coordinates)
        {
            if (item.Length < minimumPolygonPoints)
            {
                Trace.WriteLine($"CreatePolygon escape!");
                continue;
            }

            AddLineStringOrRing(builder, item, true, isLongitudeFirst);
        }

        builder.EndGeography();
    }

    private static void AddMultiPolygon(SqlGeographyBuilder builder, GeoJsonMultiPolygon geometry, bool isLongitudeFirst)
    {
        builder.BeginGeography(OpenGisGeographyType.MultiPolygon);

        foreach (var item in geometry.Coordinates)
        {
            builder.BeginGeography(OpenGisGeographyType.Polygon);

            foreach (var lines in item)
            {
                if (lines.Length < minimumPolygonPoints)
                {
                    Trace.WriteLine($"CreateMultiPolygon escape!");
                    continue;
                }

                AddLineStringOrRing(builder, lines, true, isLongitudeFirst);
            }

            builder.EndGeography();
        }

        builder.EndGeography();
    }

    //private static void AddGeometryCollection(SqlGeometryBuilder builder, IGeoJsonGeometry geometry, bool isLongitudeFirst)
    //{ 
    //    builder.BeginGeometry(OpenGisGeometryType.GeometryCollection);

    //    foreach (var item in geometry.Geometries)
    //    {
    //        if (geometry.IsNullOrEmpty())
    //        {
    //            SqlSpatialExtensions.AddEmptySqlGeometry(builder, item.Type);
    //        }

    //        switch (item.Type)
    //        {
    //            case GeometryType.Point:
    //                AddPoint(builder, item);
    //                break;
    //            case GeometryType.LineString:
    //                AddLineString(builder, item);
    //                break;
    //            case GeometryType.Polygon:
    //                AddPolygon(builder, item);
    //                break;
    //            case GeometryType.MultiPoint:
    //                AddMultiPoint(builder, item);
    //                break;
    //            case GeometryType.MultiLineString:
    //                AddMultiLineString(builder, item);
    //                break;
    //            case GeometryType.MultiPolygon:
    //                AddMultiPolygon(builder, item);
    //                break;
    //            case GeometryType.GeometryCollection:
    //            case GeometryType.CircularString:
    //            case GeometryType.CompoundCurve:
    //            case GeometryType.CurvePolygon:
    //            default:
    //                throw new NotImplementedException();
    //        }
    //    }

    //    builder.EndGeometry();             
    //}

    private static void AddLineStringOrRing(SqlGeographyBuilder builder, double[][] geometry, bool isRing, bool isLongitudeFirst)
    {
        var firstPoint = Point.Parse(geometry[0], isLongitudeFirst);

        builder.BeginFigure(firstPoint.Y, firstPoint.X);

        for (int i = 1; i < geometry.Length; i++)
        {
            var point = Point.Parse(geometry[i], isLongitudeFirst);

            builder.AddLine(point.Y, point.X);
        }

        //1399.08.19
        //should not manually repeat the last point
        //if (isRing)
        //{
        //    builder.AddLine(firstPoint.X, firstPoint.Y);
        //}

        builder.EndFigure();
    }

    #endregion
}

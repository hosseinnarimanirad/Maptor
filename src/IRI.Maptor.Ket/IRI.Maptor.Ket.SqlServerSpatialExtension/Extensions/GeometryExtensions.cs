using System.Diagnostics;
using Microsoft.SqlServer.Types;

using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Extensions;

public static class GeometryExtensions
{
    //without counting the last point
    const int minimumPolygonPoints = 3;

    public static double GetTrueArea(this Geometry<Point> geometry, Func<Point, Point> toWgs84Geodetic)
    {
        try
        {
            return geometry.AsSqlGeometry().Project(toWgs84Geodetic, SridHelper.GeodeticWGS84).MakeValid().STArea().Value;
        }
        catch (Exception)
        {
            return double.NaN;
        }
        //return GetArea(geometry.Transform(toWgs84Geodetic, 0));
    }
     
    #region SqlGeometry
     

    //ERROR PRONE: NaN and Infinity points are not supported
    public static SqlGeometry AsSqlGeometry<T>(this T point, int srid = 0) where T : IPoint, new()
    {
        //if (double.IsNaN(point.X + point.Y) || double.IsInfinity(point.X + point.Y))
        //{

        //}
        //return SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(string.Format(CultureInfo.InvariantCulture, "POINT({0:G16} {1:G16})", point.X, point.Y)));
        return SqlGeometry.Point(point.X, point.Y, srid);
    }

    public static SqlGeometry AsSqlGeometry<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        var type = geometry.Type;


        if (geometry.IsNullOrEmpty())
        {
            return Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.CreateEmptySqlGeometry(type, geometry.Srid);
        }

        SqlGeometryBuilder builder = new SqlGeometryBuilder();

        builder.SetSrid(geometry.Srid);

        switch (type)
        {
            case GeometryType.GeometryCollection:
                AddGeometryCollection(builder, geometry);
                break;

            case GeometryType.Point:
                AddPoint(builder, geometry);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(builder, geometry);
                break;

            case GeometryType.LineString:
                AddLineString(builder, geometry);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(builder, geometry);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(builder, geometry);
                break;

            case GeometryType.Polygon:
                AddPolygon(builder, geometry);
                break;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

        return builder.ConstructedGeometry.MakeValid();

    }

    public static OpenGisGeometryType ToOpenGisGeometryType(this GeometryType type)
    {
        return (OpenGisGeometryType)(int)type;
    }

    public static GeometryType ToGeometryType(this OpenGisGeometryType type)
    {
        return (GeometryType)(int)type;
    }

    private static void AddPoint<T>(SqlGeometryBuilder builder, Geometry<T> point) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.Point);

        builder.BeginFigure(point.Points[0].X, point.Points[0].Y);

        builder.EndFigure();

        builder.EndGeometry();

    }

    private static void AddMultiPoint<T>(SqlGeometryBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.MultiPoint);

        foreach (var item in geometry.Geometries)
        {
            builder.BeginGeometry(OpenGisGeometryType.Point);

            builder.BeginFigure(item.Points[0].X, item.Points[0].Y);

            builder.EndFigure();

            builder.EndGeometry();
        }

        builder.EndGeometry();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddLineString<T>(SqlGeometryBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.LineString);

        AddLineStringOrRing(builder, geometry, false);

        builder.EndGeometry();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddMultiLineString<T>(SqlGeometryBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.MultiLineString);

        foreach (var item in geometry.Geometries)
        {
            builder.BeginGeometry(OpenGisGeometryType.LineString);

            AddLineStringOrRing(builder, item, false);

            builder.EndGeometry();
        }

        builder.EndGeometry();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddPolygon<T>(SqlGeometryBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.Polygon);

        foreach (var item in geometry.Geometries)
        {
            if (item.NumberOfPoints < minimumPolygonPoints)
            {
                Trace.WriteLine($"CreatePolygon escape!");
                continue;
            }

            AddLineStringOrRing(builder, item, true);
        }

        builder.EndGeometry();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddMultiPolygon<T>(SqlGeometryBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //return CreatePolygon(points, srid);
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);

        foreach (var item in geometry.Geometries)
        {
            builder.BeginGeometry(OpenGisGeometryType.Polygon);

            foreach (var lines in item.Geometries)
            {
                if (lines.NumberOfPoints < minimumPolygonPoints)
                {
                    Trace.WriteLine($"CreateMultiPolygon escape!");
                    continue;
                }

                AddLineStringOrRing(builder, lines, true);
            }

            builder.EndGeometry();
        }

        builder.EndGeometry();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddGeometryCollection<T>(SqlGeometryBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeometry(OpenGisGeometryType.GeometryCollection);

        foreach (var item in geometry.Geometries)
        {
            if (geometry.IsNullOrEmpty())
            {
                Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.AddEmptySqlGeometry(builder, item.Type);
            }

            switch (item.Type)
            {
                case GeometryType.Point:
                    AddPoint(builder, item);
                    break;
                case GeometryType.LineString:
                    AddLineString(builder, item);
                    break;
                case GeometryType.Polygon:
                    AddPolygon(builder, item);
                    break;
                case GeometryType.MultiPoint:
                    AddMultiPoint(builder, item);
                    break;
                case GeometryType.MultiLineString:
                    AddMultiLineString(builder, item);
                    break;
                case GeometryType.MultiPolygon:
                    AddMultiPolygon(builder, item);
                    break;
                case GeometryType.GeometryCollection:
                case GeometryType.CircularString:
                case GeometryType.CompoundCurve:
                case GeometryType.CurvePolygon:
                default:
                    throw new NotImplementedException();
            }
        }

        builder.EndGeometry();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddLineStringOrRing<T>(SqlGeometryBuilder builder, Geometry<T> geometry, bool isRing) where T : IPoint, new()
    {
        builder.BeginFigure(geometry.Points[0].X, geometry.Points[0].Y);

        for (int i = 1; i < geometry.Points.Count; i++)
        {
            builder.AddLine(geometry.Points[i].X, geometry.Points[i].Y);
        }

        if (isRing)
        {
            builder.AddLine(geometry.Points[0].X, geometry.Points[0].Y);
        }

        builder.EndFigure();
    }

    #endregion


    #region SqlGeography

    public static SqlGeography AsSqlGeography(this IPoint point)
    {
        return SqlGeography.Point(latitude: point.Y, longitude: point.X, srid: SridHelper.GeodeticWGS84);
    }

    public static SqlGeography AsSqlGeography<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        var type = geometry.Type;

        if (geometry.IsNullOrEmpty())
        {
            return Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.CreateEmptySqlGeography(type, geometry.Srid);
        }

        SqlGeographyBuilder builder = new SqlGeographyBuilder();

        builder.SetSrid(geometry.Srid);

        switch (type)
        {
            case GeometryType.GeometryCollection:
                AddGeographyCollection(builder, geometry);
                break;

            case GeometryType.Point:
                AddPoint(builder, geometry);
                break;

            case GeometryType.MultiPoint:
                AddMultiPoint(builder, geometry);
                break;

            case GeometryType.LineString:
                AddLineString(builder, geometry);
                break;

            case GeometryType.MultiLineString:
                AddMultiLineString(builder, geometry);
                break;

            case GeometryType.MultiPolygon:
                AddMultiPolygon(builder, geometry);
                break;

            case GeometryType.Polygon:
                AddPolygon(builder, geometry);
                break;

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

        return builder.ConstructedGeography.MakeValid();

    }

    private static void AddPoint<T>(SqlGeographyBuilder builder, Geometry<T> point) where T : IPoint, new()
    {
        builder.BeginGeography(OpenGisGeographyType.Point);

        builder.BeginFigure(longitude: point.Points[0].X, latitude: point.Points[0].Y);

        builder.EndFigure();

        builder.EndGeography();
    }

    private static void AddMultiPoint<T>(SqlGeographyBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        builder.BeginGeography(OpenGisGeographyType.MultiPoint);

        foreach (var item in geometry.Geometries)
        {
            builder.BeginGeography(OpenGisGeographyType.Point);

            builder.BeginFigure(longitude: item.Points[0].X, latitude: item.Points[0].Y);

            builder.EndFigure();

            builder.EndGeography();
        }

        builder.EndGeography();
    }

    private static void AddLineString<T>(SqlGeographyBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        builder.BeginGeography(OpenGisGeographyType.LineString);

        AddLineStringOrRing(builder, geometry, false);

        builder.EndGeography();
    }

    private static void AddMultiLineString<T>(SqlGeographyBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        builder.BeginGeography(OpenGisGeographyType.MultiLineString);

        foreach (var item in geometry.Geometries)
        {
            builder.BeginGeography(OpenGisGeographyType.LineString);

            AddLineStringOrRing(builder, item, false);

            builder.EndGeography();
        }

        builder.EndGeography();
    }

    private static void AddPolygon<T>(SqlGeographyBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        builder.BeginGeography(OpenGisGeographyType.Polygon);

        foreach (var item in geometry.Geometries)
        {
            if (item.NumberOfPoints < minimumPolygonPoints)
            {
                Trace.WriteLine($"CreatePolygon escape!");
                continue;
            }

            AddLineStringOrRing(builder, item, true);
        }

        builder.EndGeography();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddMultiPolygon<T>(SqlGeographyBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //return CreatePolygon(points, srid);
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeography(OpenGisGeographyType.MultiPolygon);

        foreach (var item in geometry.Geometries)
        {
            builder.BeginGeography(OpenGisGeographyType.Polygon);

            foreach (var lines in item.Geometries)
            {
                if (lines.NumberOfPoints < minimumPolygonPoints)
                {
                    Trace.WriteLine($"CreateMultiPolygon escape!");
                    continue;
                }

                AddLineStringOrRing(builder, lines, true);
            }

            builder.EndGeography();
        }

        builder.EndGeography();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddGeographyCollection<T>(SqlGeographyBuilder builder, Geometry<T> geometry) where T : IPoint, new()
    {
        //SqlGeometryBuilder builder = new SqlGeometryBuilder();

        //builder.SetSrid(geometry.Srid);

        builder.BeginGeography(OpenGisGeographyType.GeometryCollection);

        foreach (var item in geometry.Geometries)
        {
            if (geometry.IsNullOrEmpty())
            {
                Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.AddEmptySqlGeography(builder, item.Type);
            }

            switch (item.Type)
            {
                case GeometryType.Point:
                    AddPoint(builder, item);
                    break;
                case GeometryType.LineString:
                    AddLineString(builder, item);
                    break;
                case GeometryType.Polygon:
                    AddPolygon(builder, item);
                    break;
                case GeometryType.MultiPoint:
                    AddMultiPoint(builder, item);
                    break;
                case GeometryType.MultiLineString:
                    AddMultiLineString(builder, item);
                    break;
                case GeometryType.MultiPolygon:
                    AddMultiPolygon(builder, item);
                    break;
                case GeometryType.GeometryCollection:
                case GeometryType.CircularString:
                case GeometryType.CompoundCurve:
                case GeometryType.CurvePolygon:
                default:
                    throw new NotImplementedException();
            }
        }

        builder.EndGeography();

        //return builder.ConstructedGeometry.MakeValid();
    }

    private static void AddLineStringOrRing<T>(SqlGeographyBuilder builder, Geometry<T> geometry, bool isRing) where T : IPoint, new()
    {
        builder.BeginFigure(longitude: geometry.Points[0].X, latitude: geometry.Points[0].Y);

        for (int i = 1; i < geometry.Points.Count; i++)
        {
            builder.AddLine(longitude: geometry.Points[i].X, latitude: geometry.Points[i].Y);
        }

        if (isRing)
        {
            builder.AddLine(longitude: geometry.Points[0].X, latitude: geometry.Points[0].Y);
        }

        builder.EndFigure();
    }

    #endregion

}

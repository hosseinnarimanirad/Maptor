using Microsoft.SqlServer.Types;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Extensions;

public static class SqlGeographyExtensions
{
    public static bool IsNullOrEmpty(this SqlGeography geography)
    {
        return geography == null || geography.IsNull || geography.STIsEmpty().IsTrue;
    }
     
    public static OpenGisGeographyType GetOpenGisType(this SqlGeography geography)
    {
        if (geography == null)
        {
            return OpenGisGeographyType.GeometryCollection;
        }
        else
        {
            return (OpenGisGeographyType)Enum.Parse(typeof(OpenGisGeographyType), geography.STGeometryType().Value, true);
        }         
    }

    public static Point AsPoint(this SqlGeography point)
    {
        if (point.IsNullOrEmpty() || point.Long.IsNull || point.Lat.IsNull)
        {
            return new Point(double.NaN, double.NaN);
        }
        else
        {
            return new Point(point.Long.Value, point.Lat.Value);
        }
    }


    #region Projection

    public static SqlGeometry Project(this SqlGeography geography, Func<Point, Point> mapFunction, int newSrid = 0)
    { 
        SqlGeometryBuilder builder = new SqlGeometryBuilder();

        builder.SetSrid(newSrid);

        geography = geography.MakeValid();

        OpenGisGeometryType geometryType = (OpenGisGeometryType)Enum.Parse(typeof(OpenGisGeometryType), geography.STGeometryType().Value, true);

        builder.BeginGeometry(geometryType);

        switch (geometryType)
        {
            case OpenGisGeometryType.CircularString:
            case OpenGisGeometryType.CompoundCurve:
            case OpenGisGeometryType.CurvePolygon:
            case OpenGisGeometryType.GeometryCollection:
                throw new NotImplementedException();

            case OpenGisGeometryType.LineString:
                ProjectLineString(builder, geography, mapFunction);
                break;

            case OpenGisGeometryType.MultiLineString:
                ProjectMultiLineSring(builder, geography, mapFunction);
                break;

            case OpenGisGeometryType.MultiPoint:
                ProjectMultiPoint(builder, geography, mapFunction);
                break;

            case OpenGisGeometryType.MultiPolygon:
                ProjectMultiPolygon(builder, geography, mapFunction);
                break;

            case OpenGisGeometryType.Point:
                ProjectPoint(builder, geography, mapFunction);
                break;

            case OpenGisGeometryType.Polygon:
                ProjectPolygon(builder, geography, mapFunction);
                break;

            default:
                throw new NotImplementedException();
        }

        builder.EndGeometry();

        return builder.ConstructedGeometry.STIsValid().IsTrue ? builder.ConstructedGeometry : builder.ConstructedGeometry.MakeValid();
    }

    //Not supporting Z and M values
    private static void ProjectMultiPolygon(SqlGeometryBuilder builder, SqlGeography multiPolygon, Func<Point, Point> mapFunction)
    {
        int numberOfGeometries = multiPolygon.STNumGeometries().Value;

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            builder.BeginGeometry(OpenGisGeometryType.Polygon);

            ProjectPolygon(builder, multiPolygon.STGeometryN(i), mapFunction);

            builder.EndGeometry();
        }
    }

    //Not supporting Z and M values
    private static void ProjectPolygon(SqlGeometryBuilder builder, SqlGeography geometry, Func<Point, Point> mapFunction)
    {
        //ProjectRing(builder, geometry.STExteriorRing(), mapFunction);

        //int numberOfInteriorRings = geometry.STNumInteriorRing().Value;
        int numberOfRings = geometry.NumRings().Value;

        for (int i = 1; i <= numberOfRings; i++)
        {
            //ProjectRing(builder, geometry.STInteriorRingN(i), mapFunction);
            ProjectLineString(builder, geometry.RingN(i), mapFunction);
        }
    }

    //Not supporting Z and M values
    private static void ProjectMultiLineSring(SqlGeometryBuilder builder, SqlGeography multiLineString, Func<Point, Point> mapFunction)
    {
        int numberOfGeometries = multiLineString.STNumGeometries().Value;

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            builder.BeginGeometry(OpenGisGeometryType.LineString);

            ProjectLineString(builder, multiLineString.STGeometryN(i), mapFunction);

            builder.EndGeometry();
        }
    }

    //Not supporting Z and M values
    private static void ProjectLineString(SqlGeometryBuilder builder, SqlGeography lineString, Func<Point, Point> mapFunction)
    {
        int numberOfPoints = lineString.STNumPoints().Value;

        //Point startPoint = mapFunction(new Point(lineString.STStartPoint().Long.Value, lineString.STStartPoint().Lat.Value));
        var startPoint = mapFunction(lineString.STStartPoint().AsPoint());

        builder.BeginFigure(startPoint.X, startPoint.Y);

        for (int i = 2; i <= numberOfPoints; i++)
        {
            var point = mapFunction(GetPoint(lineString, i));

            builder.AddLine(point.X, point.Y);
        }

        builder.EndFigure();
    }

    //Not supporting Z and M values
    //?? possible bug: start value for i
    private static void ProjectMultiPoint(SqlGeometryBuilder builder, SqlGeography multiPoint, Func<Point, Point> mapFunction)
    {
        int numberOfGeometries = multiPoint.STNumGeometries().Value;

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            builder.BeginGeometry(OpenGisGeometryType.Point);

            ProjectPoint(builder, multiPoint.STGeometryN(i), mapFunction);

            builder.EndGeometry();
        }
    }

    //Not supporting Z and M values
    public static void ProjectPoint(SqlGeometryBuilder builder, SqlGeography point, Func<Point, Point> mapFunction)
    {
        //Point thePoint = mapFunction(new Point(point.Long.Value, point.Lat.Value));
        var thePoint = mapFunction(point.AsPoint());

        builder.BeginFigure(thePoint.X, thePoint.Y);

        builder.EndFigure();
    }

    private static Point GetPoint(SqlGeography geography, int index)
    {
        return geography.STPointN(index).AsPoint(); 
    }


    public static SqlGeometry GeodeticToMercator(this SqlGeography geometry)
    {
        return geometry.Project(point => MapProjects.GeodeticToMercator(point, Ellipsoids.WGS84));
    }

    public static SqlGeometry GeodeticWgs84ToWebMercator(this SqlGeography geometry)
    {
        return geometry.Project(point => MapProjects.GeodeticWgs84ToWebMercator(point), SridHelper.WebMercator);
    }

    public static SqlGeometry GeodeticToCylindricalEqualArea(this SqlGeography geometry)
    {
        return geometry.Project(point => MapProjects.GeodeticToCylindricalEqualArea(point, Ellipsoids.WGS84));
    }

    #endregion



    #region To GeoJson

    public static IGeoJsonGeometry AsGeoJson(this SqlGeography geography, bool isLongitudeFirst = true)
    {
        OpenGisGeographyType geometryType = geography.GetOpenGisType();

        switch (geometryType)
        {
            case OpenGisGeographyType.Point:
                return geography.SqlPointToGeoJsonPoint(isLongitudeFirst);

            case OpenGisGeographyType.MultiPoint:
                return geography.SqlMultiPointToGeoJsonMultiPoint(isLongitudeFirst);

            case OpenGisGeographyType.LineString:
                return geography.SqlLineStringToGeoJsonPolyline(isLongitudeFirst);

            case OpenGisGeographyType.MultiLineString:
                return geography.SqlMultiLineStringToGeoJsonPolyline(isLongitudeFirst);

            case OpenGisGeographyType.Polygon:
                return geography.SqlPolygonToGeoJsonPolygon(isLongitudeFirst);

            case OpenGisGeographyType.MultiPolygon:
                return geography.SqlMultiPolygonToGeoJsonMultiPolygon(isLongitudeFirst);

            case OpenGisGeographyType.GeometryCollection:
            case OpenGisGeographyType.CircularString:
            case OpenGisGeographyType.CompoundCurve:
            case OpenGisGeographyType.CurvePolygon:
            case OpenGisGeographyType.FullGlobe:
            default:
                //return null;
                throw new NotImplementedException();
        }
    }

    private static double[] GetGeoJsonObjectPoint(SqlGeography point, bool isLongitudeFirst)
    {
        if (point.IsNullOrEmpty())
            return new double[0];

        return isLongitudeFirst ?
                new double[] { point.Long.Value, point.Lat.Value } :
                new double[] { point.Lat.Value, point.Long.Value };

        //return new double[] { point.Long.Value, point.Lat.Value };
    }

    private static double[][] GetGeoJsonLineStringOrRing(SqlGeography lineStringOrRing, bool isLongitudeFirst)
    {
        if (lineStringOrRing.IsNullOrEmpty())
            return new double[0][];

        int numberOfPoints = lineStringOrRing.STNumPoints().Value;

        double[][] result = new double[numberOfPoints][];

        for (int i = 1; i <= numberOfPoints; i++)
        {
            result[i - 1] = GetGeoJsonObjectPoint(lineStringOrRing.STPointN(i), isLongitudeFirst);
        }

        return result;
    }

    private static GeoJsonPoint SqlPointToGeoJsonPoint(this SqlGeography geometry, bool isLongitudeFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonPoint()
            {
                Type = GeoJson.Point,
            };

        var coordinates = isLongitudeFirst ?
                new double[] { geometry.Long.Value, geometry.Lat.Value } :
                new double[] { geometry.Lat.Value, geometry.Long.Value };

        return new GeoJsonPoint()
        {
            Type = GeoJson.Point,
            Coordinates = coordinates /*new double[] { geometry.Long.Value, geometry.Lat.Value }*/
        };
    }

    private static GeoJsonMultiPoint SqlMultiPointToGeoJsonMultiPoint(this SqlGeography geometry, bool isLongitudeFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return GeoJsonMultiPoint.Empty;
        //return new GeoJsonMultiPoint()
        //{
        //    //Type = GeoJson.MultiPoint,
        //    //Coordinates = new double[0][],
        //    Coordinates = [],
        //};

        var numberOfGeometries = geometry.STNumGeometries().Value;

        double[][] points = new double[numberOfGeometries][];

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            points[i - 1] = GetGeoJsonObjectPoint(geometry.STGeometryN(i), isLongitudeFirst);
        }

        return new GeoJsonMultiPoint()
        {
            Coordinates = points,
            //Type = GeoJson.MultiPoint,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonLineString SqlLineStringToGeoJsonPolyline(this SqlGeography geometry, bool isLongitudeFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonLineString()
            {
                Type = GeoJson.LineString,
                Coordinates = new double[0][],
            };

        double[][] paths = GetGeoJsonLineStringOrRing(geometry, isLongitudeFirst);

        return new GeoJsonLineString()
        {
            Coordinates = paths,
            Type = GeoJson.LineString,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonMultiLineString SqlMultiLineStringToGeoJsonPolyline(this SqlGeography geometry, bool isLongitudeFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonMultiLineString()
            {
                Type = GeoJson.MultiLineString,
                Coordinates = new double[0][][],
            };

        int numberOfParts = geometry.STNumGeometries().Value;

        double[][][] result = new double[numberOfParts][][];

        for (int i = 1; i <= numberOfParts; i++)
        {
            result[i - 1] = GetGeoJsonLineStringOrRing(geometry.STGeometryN(i), isLongitudeFirst);
        }

        return new GeoJsonMultiLineString()
        {
            Coordinates = result,
            Type = GeoJson.MultiLineString,
        };
    }

    //Not supportig Z and M Values
    //todo: 1399.08.19; this method must be checked
    private static GeoJsonPolygon SqlPolygonToGeoJsonPolygon(this SqlGeography geometry, bool isLongitudeFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonPolygon()
            {
                Type = GeoJson.Polygon,
                Coordinates = new double[0][][],
            };

        //double[][][] rings = new double[1][][] { GetGeoJsonLineStringOrRing(geometry) };
        var numberOfRings = geometry.NumRings().Value;

        // 1400.01.30
        // در ژئوگرافی بر خلاف ژئومتری تعداد رینگ‌ها کل موارد رو شامل
        // می‌شه. در واقع حلقه داخلی و خارجی از هم جدا نشده پس این‌جا
        // نیازی نیست که به علاوه یک کنیم.
        //double[][][] result = new double[numberOfRings + 1][][];

        double[][][] result = new double[numberOfRings][][];

        for (int i = 1; i <= numberOfRings; i++)
        {
            result[i - 1] = GetGeoJsonLineStringOrRing(geometry.RingN(i), isLongitudeFirst);
        }

        return new GeoJsonPolygon()
        {
            Coordinates = result,
            Type = GeoJson.Polygon,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonMultiPolygon SqlMultiPolygonToGeoJsonMultiPolygon(this SqlGeography geometry, bool isLongitudeFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonMultiPolygon()
            {
                Type = GeoJson.MultiPolygon,
                Coordinates = new double[0][][][],
            };

        int numberOfParts = geometry.STNumGeometries().Value;

        double[][][][] rings = new double[numberOfParts][][][];

        for (int i = 1; i <= numberOfParts; i++)
        {
            rings[i - 1] = geometry.STGeometryN(i).SqlPolygonToGeoJsonPolygon(isLongitudeFirst).Coordinates;
        }

        return new GeoJsonMultiPolygon()
        {
            Coordinates = rings,
            Type = GeoJson.MultiPolygon,
        };
    }

    #endregion


}
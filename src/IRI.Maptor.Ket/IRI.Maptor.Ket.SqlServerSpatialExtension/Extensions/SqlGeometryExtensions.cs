using System.Text;
using Microsoft.SqlServer.Types;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions; 

namespace IRI.Maptor.Extensions;

public static class SqlGeometryExtensions
{
    public static bool IsNullOrEmpty(this SqlGeometry geometry)
    {
        return geometry is null || geometry.IsNull || geometry.STIsEmpty().IsTrue;
    }
     
    public static int GetSrid(this SqlGeometry geometry)
    {
        if (geometry is null)
            return 0;

        var srid = geometry.STSrid;

        return srid.IsNull ? 0 : srid.Value;
    }

    public static double GetAreaInSquareKilometers(this SqlGeometry geometry, Func<IPoint, Point> toWgs84)
    {
        if (geometry is null)
            return 0;

        try
        {
            return geometry.Transform(i => MapProjects.GeodeticToCylindricalEqualArea(toWgs84(i)))
                            .STArea()
                            .Value / 1000000.0;
        }
        catch (Exception)
        {
            return 0;
        }

    }


    public static BoundingBox GetBoundingBox(this List<SqlGeometry> spatialFeatures)
    {
        if (spatialFeatures.IsNullOrEmpty())
            return new BoundingBox(double.NaN, double.NaN, double.NaN, double.NaN);

        var envelopes = spatialFeatures.AsParallel().Select(i => i?.STEnvelope()).Where(i => !i.IsNullOrEmpty()).ToList();

        return SqlSpatialHelper.GetBoundingBoxFromEnvelopes(envelopes);
    }


    public static string AsWkt(this SqlGeometry geometry)
    {
        return new string(geometry.STAsText().Value);
    }

    public static string AsWktZM(this SqlGeometry geometry)
    {
        return new string(geometry.AsTextZM().Value);
    }

    public static byte[] AsWkb(this SqlGeometry geometry)
    {
        return geometry.STAsBinary().Value;
    }

    public static byte[] AsWkbZm(this SqlGeometry geometry)
    {
        return geometry.AsBinaryZM().Value;
    }

    public static string AsWkbBase64(this SqlGeometry geometry)
    {
        return Convert.ToBase64String(geometry.AsWkb());
    }

    public static string AsWkbString(this SqlGeometry geometry)
    {
        return Sta.Common.Helpers.HexStringHelper.ByteToHexBitFiddle(geometry?.AsWkb(), true);
    }


    public static List<SqlGeometry> GetGeometries(this SqlGeometry geometry)
    {
        if (geometry.IsNullOrEmpty() || geometry.STNumGeometries().IsNull)
        {
            return new List<SqlGeometry>();
        }

        int numberOfGeometries = geometry.STNumGeometries().Value;

        var result = new List<SqlGeometry>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            result.Add(geometry.STGeometryN(i + 1));
        }

        return result;
    }

    public static BoundingBox GetBoundingBox(this SqlGeometry geometry)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new BoundingBox(double.NaN, double.NaN, double.NaN, double.NaN);
        }
        else if (geometry.STIsValid().IsFalse)
        {
            return new BoundingBox(double.NaN, double.NaN, double.NaN, double.NaN);
        }

        var envelope = geometry.STEnvelope();

        var point1 = envelope.STPointN(1);
        var point2 = envelope.STPointN(2);
        var point3 = envelope.STPointN(3);
        var point4 = envelope.STPointN(4);

        var xArray = new double[] { point1.STX.Value, point2.STX.Value, point3.STX.Value, point4.STX.Value };

        var yArray = new double[] { point1.STY.Value, point2.STY.Value, point3.STY.Value, point4.STY.Value };

        return new BoundingBox(xArray.Min(), yArray.Min(), xArray.Max(), yArray.Max());
    }

    public static OpenGisGeometryType GetOpenGisType(this SqlGeometry geometry)
    {
        if (geometry == null)
        {
            return OpenGisGeometryType.GeometryCollection;
        }
        else
        {
            return (OpenGisGeometryType)Enum.Parse(typeof(OpenGisGeometryType), geometry.STGeometryType().Value, true);
        } 
    }

    public static bool IsPointOrMultiPoint(this SqlGeometry geometry)
    {
        if (geometry != null)
        {
            var type = geometry.GetOpenGisType();

            return type == OpenGisGeometryType.Point || type == OpenGisGeometryType.MultiPoint;
        }

        return false;
    }

    public static bool IsLineStringOrMultiLineString(this SqlGeometry geometry)
    {
        if (geometry != null)
        {
            var type = geometry.GetOpenGisType();

            return type == OpenGisGeometryType.LineString || type == OpenGisGeometryType.MultiLineString;
        }

        return false;
    }

    public static bool IsPolygonOrMultiPolygon(this SqlGeometry geometry)
    {
        if (geometry != null)
        {
            var type = geometry.GetOpenGisType();

            return type == OpenGisGeometryType.Polygon || type == OpenGisGeometryType.MultiPolygon;
        }

        return false;
    }

    public static Point AsPoint(this SqlGeometry point)
    {
        if (point.IsNullOrEmpty() || point.STX.IsNull || point.STY.IsNull)
        {
            return new Point(double.NaN, double.NaN);
        }
        else
        {
            return new Point(point.STX.Value, point.STY.Value);
        }
    }

    public static SqlGeometry GetCentroidOrOnSurface(this SqlGeometry geometry)
    { 
        return geometry.STContains(geometry.STCentroid()).Value ? geometry.STCentroid() : geometry.STPointOnSurface();
    }


    public static List<Point> GetAllPoints(this SqlGeometry geometry)
    {
        return geometry.AsGeometry().GetAllPoints();
    }


    #region Projection

    public static SqlGeometry Transform(this SqlGeometry geometry, Func<Point, Point> mapFunction, int newSrid = 0)
    {
        return geometry.AsGeometry().Transform(mapFunction, newSrid).AsSqlGeometry();
    }

    public static SqlGeography Project(this SqlGeometry geometry, Func<Point, Point> mapFunction, int srid)
    {
        var builder = new SqlGeographyBuilder();

        builder.SetSrid(srid);

        geometry = geometry.MakeValid();

        OpenGisGeographyType geographyType = (OpenGisGeographyType)Enum.Parse(typeof(OpenGisGeographyType), geometry.STGeometryType().Value, true);

        builder.BeginGeography(geographyType);

        switch (geographyType)
        {
            case OpenGisGeographyType.Point:
                ProjectPoint(builder, geometry, mapFunction);
                break;

            case OpenGisGeographyType.LineString:
                ProjectLineString(builder, geometry, mapFunction);
                break;

            case OpenGisGeographyType.Polygon:
                ProjectPolygon(builder, geometry, mapFunction);
                break;

            case OpenGisGeographyType.MultiPoint:
                ProjectMultiPoint(builder, geometry, mapFunction);
                break;

            case OpenGisGeographyType.MultiLineString:
                ProjectMultiLineSring(builder, geometry, mapFunction);
                break;

            case OpenGisGeographyType.MultiPolygon:
                ProjectMultiPolygon(builder, geometry, mapFunction);
                break;

            case OpenGisGeographyType.GeometryCollection:
            case OpenGisGeographyType.CircularString:
            case OpenGisGeographyType.CompoundCurve:
            case OpenGisGeographyType.CurvePolygon:
            case OpenGisGeographyType.FullGlobe:
            default:
                throw new NotImplementedException();
        }

        builder.EndGeography();

        var result = builder.ConstructedGeography.STIsValid().IsTrue ? builder.ConstructedGeography : builder.ConstructedGeography.MakeValid();

        return result.EnvelopeAngle().Value == 180 ? result.ReorientObject() : result;


    }

    //Not supporting Z and M values
    private static void ProjectMultiPolygon(SqlGeographyBuilder builder, SqlGeometry multiPolygon, Func<Point, Point> mapFunction)
    {
        int numberOfGeometries = multiPolygon.STNumGeometries().Value;

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            builder.BeginGeography(OpenGisGeographyType.Polygon);

            ProjectPolygon(builder, multiPolygon.STGeometryN(i), mapFunction);

            builder.EndGeography();
        }
    }

    //Not supporting Z and M values
    private static void ProjectPolygon(SqlGeographyBuilder builder, SqlGeometry polygon, Func<Point, Point> mapFunction)
    {
        ProjectLineString(builder, polygon.STExteriorRing(), mapFunction);

        int numberOfInteriorRings = polygon.STNumInteriorRing().Value;
        //int numberOfRings = polygon.STNumCurves().Value;

        for (int i = 1; i <= numberOfInteriorRings; i++)
        {
            //ProjectRing(builder, geometry.STInteriorRingN(i), mapFunction);
            ProjectLineString(builder, polygon.STInteriorRingN(i), mapFunction);
        }
    }

    //Not supporting Z and M values
    private static void ProjectMultiLineSring(SqlGeographyBuilder builder, SqlGeometry multiLineString, Func<Point, Point> mapFunction)
    {
        int numberOfGeometries = multiLineString.STNumGeometries().Value;

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            builder.BeginGeography(OpenGisGeographyType.LineString);

            ProjectLineString(builder, multiLineString.STGeometryN(i), mapFunction);

            builder.EndGeography();
        }
    }

    //Not supporting Z and M values
    private static void ProjectLineString(SqlGeographyBuilder builder, SqlGeometry lineString, Func<Point, Point> mapFunction)
    {
        if (lineString.IsNull)
            return;

        int numberOfPoints = lineString.STNumPoints().Value;

        //Point startPoint = mapFunction(new Point(lineString.STStartPoint().Long.Value, lineString.STStartPoint().Lat.Value));
        var startPoint = mapFunction(lineString.STStartPoint().AsPoint());

        builder.BeginFigure(startPoint.Y, startPoint.X);

        for (int i = 2; i <= numberOfPoints; i++)
        {
            var point = mapFunction(GetPoint(lineString, i));

            builder.AddLine(point.Y, point.X);
        }

        builder.EndFigure();
    }

    //Not supporting Z and M values
    //?? possible bug: start value for i
    private static void ProjectMultiPoint(SqlGeographyBuilder builder, SqlGeometry multiPoint, Func<Point, Point> mapFunction)
    {
        int numberOfGeometries = multiPoint.STNumGeometries().Value;

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            builder.BeginGeography(OpenGisGeographyType.Point);

            ProjectPoint(builder, multiPoint.STGeometryN(i), mapFunction);

            builder.EndGeography();
        }
    }

    //Not supporting Z and M values
    private static void ProjectPoint(SqlGeographyBuilder builder, SqlGeometry point, Func<Point, Point> mapFunction)
    {
        //Point thePoint = mapFunction(new Point(point.Long.Value, point.Lat.Value));
        var thePoint = mapFunction(point.AsPoint());

        builder.BeginFigure(thePoint.Y, thePoint.X);

        builder.EndFigure();
    }

    private static Point GetPoint(SqlGeometry geometry, int index)
    {
        return geometry.STPointN(index).AsPoint();

        //return new IRI.Maptor.Sta.Common.Primitives.Point(temp.Long.Value, temp.Lat.Value);
    }


    public static SqlGeography WebMercatorToGeodeticWgs84(this SqlGeometry geometry)
    {
        return geometry.Project(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
    }

    public static SqlGeography MercatorToGeographic(this SqlGeometry geometry)
    {
        return geometry.Project(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);
    }

    public static SqlGeography UTMToGeographic(this SqlGeometry geometry, int utmZone)
    {
        return geometry.Project(i => MapProjects.UTMToGeodetic(i, utmZone), SridHelper.GeodeticWGS84);
    }

    #endregion


    #region To Geometry

    public static Geometry<Point> AsGeometry(this SqlGeometry geometry)
    {
        // old
        //This check is not required bacause it is already checked at evey ExtractXXXX function
        //this is specially for MultiXXX types. What is multipolygon was not Empty but first GeometryN of it was empty

        // 1399.12.27
        // This check is required!
        if (geometry.IsNull)
        {
            return Geometry<Point>.Empty;
        }
        if (geometry.STIsEmpty().IsTrue)
        {
            return Geometry<Point>.CreateEmpty(geometry.GetOpenGisType().ToGeometryType(), geometry.GetSrid());
        }


        Geometry<Point> result;

        switch (geometry.GetOpenGisType())
        {
            case OpenGisGeometryType.Point:
                result = SqlPointToGeometry(geometry);
                break;

            case OpenGisGeometryType.MultiPoint:
                result = SqlMultiPointToGeometry(geometry);
                break;

            case OpenGisGeometryType.LineString:
                result = SqlLineStringToGeometry(geometry, false);
                break;

            case OpenGisGeometryType.MultiLineString:
                result = SqlMultiLineStringToGeometry(geometry);
                break;

            case OpenGisGeometryType.Polygon:
                result = SqlPolygonToGeometry(geometry);
                break;

            case OpenGisGeometryType.MultiPolygon:
                result = SqlMultiPolygonToGeometry(geometry);
                break;

            case OpenGisGeometryType.GeometryCollection:
                result = SqlGeometryCollectionToGeometry(geometry);
                break;

            case OpenGisGeometryType.CircularString:
            case OpenGisGeometryType.CompoundCurve:
            case OpenGisGeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }

        result.Srid = geometry.GetSrid();

        return result;
    }

    private static Geometry<Point> SqlPointToGeometry(SqlGeometry point)
    {
        var srid = point.GetSrid();

        //This check is required
        if (point.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.Point, srid);

        return new Geometry<Point>(new List<Point>() { point.AsPoint() }, GeometryType.Point, srid);
    }

    private static Geometry<Point> SqlMultiPointToGeometry(SqlGeometry multiPoint)
    {
        var srid = multiPoint.GetSrid();

        //This check is required
        if (multiPoint.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, srid);

        var numberOfGeometries = multiPoint.STNumGeometries().Value;

        List<Geometry<Point>> result = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            //1399.07.11
            //result[i - 1] = SqlPointToGeometry(multiPoint.STGeometryN(i));
            result.Add(SqlPointToGeometry(multiPoint.STGeometryN(i)));
        }

        return new Geometry<Point>(result, GeometryType.MultiPoint, srid);
    }

    private static Geometry<Point> SqlLineStringToGeometry(SqlGeometry lineString, bool isRing)
    {
        var srid = lineString.GetSrid();

        //This check is required
        if (lineString.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.LineString, srid);

        int numberOfPoints = isRing ? lineString.STNumPoints().Value - 1 : lineString.STNumPoints().Value;

        List<Point> result = new List<Point>(numberOfPoints);

        for (int i = 1; i <= numberOfPoints; i++)
        {
            //1399.07.11
            //result[i - 1] = new Point(lineString.STPointN(i).STX.Value, lineString.STPointN(i).STY.Value);
            result.Add(new Point(lineString.STPointN(i).STX.Value, lineString.STPointN(i).STY.Value));
        }

        return new Geometry<Point>(result, GeometryType.LineString, srid);
    }

    private static Geometry<Point> SqlMultiLineStringToGeometry(SqlGeometry multiLineString)
    {
        var srid = multiLineString.GetSrid();

        //This check is required
        if (multiLineString.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiLineString, srid);

        int numberOfParts = multiLineString.STNumGeometries().Value;

        List<Geometry<Point>> result = new List<Geometry<Point>>(numberOfParts);

        for (int i = 1; i <= numberOfParts; i++)
        {
            //1399.07.11
            //result[i - 1] = SqlLineStringToGeometry(multiLineString.STGeometryN(i), false);
            result.Add(SqlLineStringToGeometry(multiLineString.STGeometryN(i), false));
        }

        return new Geometry<Point>(result, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point> SqlPolygonToGeometry(SqlGeometry polygon)
    {
        var srid = polygon.GetSrid();

        //This check is required
        if (polygon.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.Polygon, srid);

        var numberOfInteriorRings = polygon.STNumInteriorRing().Value;

        Geometry<Point> exteriorRing = SqlLineStringToGeometry(polygon.STExteriorRing(), true);

        List<Geometry<Point>> result = new List<Geometry<Point>>(numberOfInteriorRings + 1);

        //result[0] = exteriorRing;
        result.Add(exteriorRing);

        for (int i = 1; i <= numberOfInteriorRings; i++)
        {
            //The first result contains the exterior ring so start at i 
            //result[i] = SqlLineStringToGeometry(polygon.STInteriorRingN(i), true);
            result.Add(SqlLineStringToGeometry(polygon.STInteriorRingN(i), true));
        }

        return new Geometry<Point>(result, GeometryType.Polygon, srid);
    }

    private static Geometry<Point> SqlMultiPolygonToGeometry(SqlGeometry multiPolygon)
    {
        var srid = multiPolygon.GetSrid();

        //This check is required
        if (multiPolygon.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPolygon, srid);

        var numberOfGeometries = multiPolygon.STNumGeometries().Value;

        List<Geometry<Point>> result = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            //result[i] = SqlPolygonToGeometry(multiPolygon.STGeometryN(i + 1));
            result.Add(SqlPolygonToGeometry(multiPolygon.STGeometryN(i + 1)));
        }

        return new Geometry<Point>(result, GeometryType.MultiPolygon, srid);
    }

    private static Geometry<Point> SqlGeometryCollectionToGeometry(SqlGeometry geometryCollection)
    {
        var srid = geometryCollection.GetSrid();

        //This check is required
        if (geometryCollection.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.GeometryCollection, srid);

        var numberOfGeometries = geometryCollection.STNumGeometries().Value;

        //Geometry[] result = new Geometry[numberOfGeometries];
        List<Geometry<Point>> result = new List<Geometry<Point>>(numberOfGeometries);

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            //result[i - 1] = geometryCollection.STGeometryN(i).AsGeometry();
            result.Add(geometryCollection.STGeometryN(i).AsGeometry());
        }

        return new Geometry<Point>(result, GeometryType.GeometryCollection, srid);
    }

    #endregion


    #region To Esri Shape

    public static EsriPoint AsEsriPoint(this SqlGeometry point)
    {
        if (point.IsNullOrEmpty() || point.STX.IsNull || point.STY.IsNull)
        {
            return new EsriPoint(double.NaN, double.NaN, 0);
        }
        else
        {
            return new EsriPoint(point.STX.Value, point.STY.Value, point.STSrid.Value);
        }
    }

    public static IEsriShape AsEsriShape(this SqlGeometry geometry, Func<IPoint, IPoint> mapFunction = null)
    {
        //if (geometry.IsNotValidOrEmpty())
        //{
        //    return null;
        //}

        OpenGisGeometryType geometryType = geometry.GetOpenGisType();

        switch (geometryType)
        {
            case OpenGisGeometryType.CircularString:
            case OpenGisGeometryType.CompoundCurve:
            case OpenGisGeometryType.CurvePolygon:
            case OpenGisGeometryType.GeometryCollection:
            default:
                throw new NotImplementedException();

            case OpenGisGeometryType.LineString:
                return SqlLineStringOrMultiLineStringToEsriPolyline(geometry, mapFunction);

            case OpenGisGeometryType.MultiLineString:
                return SqlLineStringOrMultiLineStringToEsriPolyline(geometry, mapFunction);

            case OpenGisGeometryType.MultiPoint:
                return SqlMultiPointToEsriMultiPoint(geometry, mapFunction);

            case OpenGisGeometryType.MultiPolygon:
                return SqlPolygonToEsriPolygon(geometry, mapFunction);


            case OpenGisGeometryType.Point:
                return SqlPointToEsriPoint(geometry, mapFunction);

            case OpenGisGeometryType.Polygon:
                return SqlPolygonToEsriPolygon(geometry, mapFunction);
        }
    }

    //Not supportig Z and M Values
    private static EsriPoint SqlPointToEsriPoint(SqlGeometry geometry, Func<IPoint, IPoint> mapFunction)
    {
        var point = geometry.AsEsriPoint();

        return mapFunction == null ? point : (EsriPoint)mapFunction(point);
    }

    //Not supportig Z and M Values
    private static EsriMultiPoint SqlMultiPointToEsriMultiPoint(SqlGeometry geometry, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty() || geometry.STNumGeometries().IsNull)
        {
            return new EsriMultiPoint();
        }

        int numberOfGeometries = geometry.STNumGeometries().Value;

        List<EsriPoint> points = new List<EsriPoint>(geometry.STNumPoints().Value);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            int index = i + 1;

            //EsriPoint point = new EsriPoint(geometry.STGeometryN(index).STX.Value, geometry.STGeometryN(index).STY.Value);
            var point = geometry.STGeometryN(index).AsEsriPoint();

            if (mapFunction != null)
            {
                point = (EsriPoint)mapFunction(point);
            }

            points.Add(point);
        }

        return new EsriMultiPoint(points.ToArray());
    }

    //Not supporting Z and M values
    private static EsriPolyline SqlLineStringOrMultiLineStringToEsriPolyline(SqlGeometry geometry, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriPolyline();
        }

        int numberOfGeometries = geometry.STNumGeometries().Value;

        List<EsriPoint> points = new List<EsriPoint>(geometry.STNumPoints().Value);

        List<int> parts = new List<int>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            int index = i + 1;

            parts.Add(points.Count);

            points.AddRange(GetPoints(geometry.STGeometryN(index), mapFunction));
        }

        return new EsriPolyline(points.ToArray(), parts.ToArray());
    }


    //Not supporting Z and M values
    //check for cw and cww criteria
    private static EsriPolygon SqlPolygonToEsriPolygon(SqlGeometry geometry, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return new EsriPolygon();
        }

        int numberOfGeometries = geometry.STNumGeometries().Value;

        List<EsriPoint> points = new List<EsriPoint>(geometry.STNumPoints().Value);

        List<int> parts = new List<int>(numberOfGeometries);

        for (int i = 0; i < numberOfGeometries; i++)
        {
            int index = i + 1;

            SqlGeometry tempPolygon = geometry.STGeometryN(index);

            var exterior = tempPolygon.STExteriorRing();

            if (tempPolygon.IsNullOrEmpty() || exterior.IsNullOrEmpty())
                continue;

            parts.Add(points.Count);

            points.AddRange(GetPoints(exterior, mapFunction));

            for (int j = 0; j < tempPolygon.STNumInteriorRing(); j++)
            {
                var interior = tempPolygon.STInteriorRingN(j + 1);

                if (interior.IsNullOrEmpty())
                    continue;

                parts.Add(points.Count);

                points.AddRange(GetPoints(interior, mapFunction));
            }
        }

        return new EsriPolygon(points.ToArray(), parts.ToArray());
    }

    private static EsriPoint[] GetPoints(SqlGeometry geometry, Func<IPoint, IPoint> mapFunction)
    {
        if (geometry.IsNullOrEmpty())
        {
            return null;
        }

        int numberOfPoints = geometry.STNumPoints().Value;

        EsriPoint[] points = new EsriPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            int index = i + 1;

            //EsriPoint point = new EsriPoint(geometry.STPointN(index).STX.Value, geometry.STPointN(index).STY.Value);
            var point = geometry.STPointN(index).AsEsriPoint();

            if (mapFunction == null)
            {
                points[i] = point;
            }
            else
            {
                points[i] = (EsriPoint)mapFunction(point);
            }

        }

        return points;
    }

    #endregion


    #region To Esri Json Geometry

    public static EsriJsonGeometry AsEsriJsonGeometry(this SqlGeometry geometry)
    {
        //if (geometry.IsNotValidOrEmpty())
        //{
        //    throw new NotImplementedException();
        //}

        OpenGisGeometryType geometryType = geometry.GetOpenGisType();

        switch (geometryType)
        {
            case OpenGisGeometryType.CircularString:
            case OpenGisGeometryType.CompoundCurve:
            case OpenGisGeometryType.CurvePolygon:
            case OpenGisGeometryType.GeometryCollection:
            default:
                throw new NotImplementedException();

            case OpenGisGeometryType.Point:
                return geometry.SqlPointToEsriJsonPoint();

            case OpenGisGeometryType.MultiPoint:
                return geometry.SqlMultiPointToEsriJsonMultiPoint();

            case OpenGisGeometryType.LineString:
                return geometry.SqlLineStringToEsriJsonPolyline();

            case OpenGisGeometryType.MultiLineString:
                return geometry.SqlMultiLineStringToEsriJsonPolyline();

            case OpenGisGeometryType.Polygon:
                return geometry.SqlPolygonToEsriJsonPolygon();

            case OpenGisGeometryType.MultiPolygon:
                return geometry.SqlMultiPolygonToEsriJsonMultiPolygon();
        }
    }

    private static double?[] GetEsriJsonObjectPoint(SqlGeometry point)
    {
        if (point.IsNullOrEmpty())
            return new double?[0];

        return new double?[] { point.STX.Value, point.STY.Value };
    }

    private static double?[][] GetLineStringOrRing(SqlGeometry lineStringOrRing)
    {
        if (lineStringOrRing.IsNullOrEmpty())
            return new double?[0][];

        int numberOfPoints = lineStringOrRing.STNumPoints().Value;

        double?[][] result = new double?[numberOfPoints][];

        for (int i = 1; i <= numberOfPoints; i++)
        {
            result[i - 1] = GetEsriJsonObjectPoint(lineStringOrRing.STPointN(i));
        }

        return result;
    }

    //Not supportig Z and M Values
    private static EsriJsonGeometry SqlPointToEsriJsonPoint(this SqlGeometry geometry)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new EsriJsonGeometry()
            {
                Type = EsriJsonGeometryType.point,
            };

        return new EsriJsonGeometry()
        {
            X = geometry.STX.Value,
            Y = geometry.STY.Value,
            Type = EsriJsonGeometryType.point,
            SpatialReference = new EsriJsonSpatialreference() { Wkid = geometry.GetSrid() }
        };
    }

    //Not supportig Z and M Values
    private static EsriJsonGeometry SqlMultiPointToEsriJsonMultiPoint(this SqlGeometry geometry)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new EsriJsonGeometry()
            {
                Type = EsriJsonGeometryType.multipoint,
                Points = new double?[0][],
            };

        var numberOfGeometries = geometry.STNumGeometries().Value;

        double?[][] points = new double?[numberOfGeometries][];

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            points[i - 1] = GetEsriJsonObjectPoint(geometry.STGeometryN(i));
        }

        return new EsriJsonGeometry()
        {
            Points = points,
            Type = EsriJsonGeometryType.multipoint,
            SpatialReference = new EsriJsonSpatialreference() { Wkid = geometry.GetSrid() }
        };
    }

    //Not supportig Z and M Values
    private static EsriJsonGeometry SqlLineStringToEsriJsonPolyline(this SqlGeometry geometry)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new EsriJsonGeometry()
            {
                Type = EsriJsonGeometryType.polyline,
                Paths = new double?[0][][],
            };

        double?[][][] paths = new double?[1][][] { GetLineStringOrRing(geometry) };

        return new EsriJsonGeometry()
        {
            Paths = paths,
            Type = EsriJsonGeometryType.polyline,
            SpatialReference = new EsriJsonSpatialreference() { Wkid = geometry.GetSrid() }
        };
    }

    //Not supportig Z and M Values
    private static EsriJsonGeometry SqlMultiLineStringToEsriJsonPolyline(this SqlGeometry geometry)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new EsriJsonGeometry()
            {
                Type = EsriJsonGeometryType.polyline,
                Paths = new double?[0][][],
            };

        int numberOfParts = geometry.STNumGeometries().Value;

        double?[][][] result = new double?[numberOfParts][][];

        for (int i = 1; i <= numberOfParts; i++)
        {
            result[i - 1] = GetLineStringOrRing(geometry.STGeometryN(i));
        }

        return new EsriJsonGeometry()
        {
            Paths = result,
            Type = EsriJsonGeometryType.polyline,
            SpatialReference = new EsriJsonSpatialreference() { Wkid = geometry.GetSrid() }
        };
    }

    //Not supportig Z and M Values
    //todo: 1399.08.19; this method is not OK, look at SqlGeometry To Geometry
    private static EsriJsonGeometry SqlPolygonToEsriJsonPolygon(this SqlGeometry geometry)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new EsriJsonGeometry()
            {
                Type = EsriJsonGeometryType.polygon,
                Rings = new double?[0][][],
            };

        double?[][][] rings = new double?[1][][] { GetLineStringOrRing(geometry) };

        return new EsriJsonGeometry()
        {
            Paths = rings,
            Type = EsriJsonGeometryType.polygon,
            SpatialReference = new EsriJsonSpatialreference() { Wkid = geometry.GetSrid() }
        };
    }

    //Not supportig Z and M Values
    private static EsriJsonGeometry SqlMultiPolygonToEsriJsonMultiPolygon(this SqlGeometry geometry)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new EsriJsonGeometry()
            {
                Type = EsriJsonGeometryType.polygon,
                Rings = new double?[0][][],
            };

        int numberOfParts = geometry.STNumGeometries().Value;

        double?[][][] rings = new double?[numberOfParts][][];

        for (int i = 1; i <= numberOfParts; i++)
        {
            rings[i - 1] = GetLineStringOrRing(geometry.STGeometryN(i));
        }

        return new EsriJsonGeometry()
        {
            Paths = rings,
            Type = EsriJsonGeometryType.polygon,
            SpatialReference = new EsriJsonSpatialreference() { Wkid = geometry.GetSrid() }
        };
    }

    #endregion


    #region To Path Markup

    public static string AsPathMarkup(this SqlGeometry geometry, double decimals = double.NaN)
    {
        //if (geometry.IsNotValidOrEmpty())
        //{
        //    throw new NotImplementedException();
        //}



        OpenGisGeometryType geometryType = geometry.GetOpenGisType();

        switch (geometryType)
        {
            case OpenGisGeometryType.CircularString:
            case OpenGisGeometryType.CompoundCurve:
            case OpenGisGeometryType.CurvePolygon:
            case OpenGisGeometryType.GeometryCollection:
            default:
                throw new NotImplementedException();

            case OpenGisGeometryType.Point:
            case OpenGisGeometryType.MultiPoint:
                throw new NotImplementedException();

            case OpenGisGeometryType.LineString:
                return geometry.SqlLineStringToPathMarkup(decimals);

            case OpenGisGeometryType.MultiLineString:
                return geometry.SqlMultiLineStringToPathMarkup(decimals);

            case OpenGisGeometryType.Polygon:
                return geometry.SqlPolygonToPathMarkup(decimals);

            case OpenGisGeometryType.MultiPolygon:
                return geometry.SqlMultiPolygonToPathMarkup(decimals);
        }
    }

    private static string LineStringOrRingToPathMarkup(SqlGeometry lineStringOrRing, double decimals)
    {
        if (lineStringOrRing.IsNullOrEmpty())
            return string.Empty;

        int numberOfPoints = lineStringOrRing.STNumPoints().Value;

        StringBuilder result = new StringBuilder(" M ");

        if (double.IsNaN(decimals))
        {
            for (int i = 1; i <= numberOfPoints; i++)
            {
                var p = lineStringOrRing.STPointN(i);

                result.Append($"{p.STX.Value},{-p.STY.Value}L ");
            }
        }
        else
        {
            int digits = (int)decimals;

            for (int i = 1; i <= numberOfPoints; i++)
            {
                var p = lineStringOrRing.STPointN(i);

                result.Append($"{Math.Round(p.STX.Value, digits)},{Math.Round(-p.STY.Value, digits)}L ");
            }
        }


        result.Remove(result.Length - 2, 1);

        result.Append(" Z ");

        return result.ToString();
    }

    //Not supportig Z and M Values
    private static string SqlLineStringToPathMarkup(this SqlGeometry geometry, double decimals)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return string.Empty;

        return LineStringOrRingToPathMarkup(geometry, decimals);
    }

    //Not supportig Z and M Values
    private static string SqlMultiLineStringToPathMarkup(this SqlGeometry geometry, double decimals)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return string.Empty;

        int numberOfParts = geometry.STNumGeometries().Value;

        StringBuilder result = new StringBuilder();

        for (int i = 1; i <= numberOfParts; i++)
        {
            result.Append(LineStringOrRingToPathMarkup(geometry.STGeometryN(i), decimals));
        }

        return result.ToString();
    }

    //Not supportig Z and M Values
    //todo: 1399.08.19; this method is not OK, look at SqlGeometry To Geometry
    private static string SqlPolygonToPathMarkup(this SqlGeometry geometry, double decimals)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return string.Empty;

        var numberOfInteriorRings = geometry.STNumInteriorRing().Value;

        StringBuilder result = new StringBuilder(LineStringOrRingToPathMarkup(geometry.STExteriorRing(), decimals));

        for (int i = 1; i <= numberOfInteriorRings; i++)
        {
            //The first result contains the exterior ring so start at i 
            result.Append(LineStringOrRingToPathMarkup(geometry.STInteriorRingN(i), decimals));
        }

        return result.ToString();
    }

    //Not supportig Z and M Values
    private static string SqlMultiPolygonToPathMarkup(this SqlGeometry geometry, double decimals)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return string.Empty;

        int numberOfParts = geometry.STNumGeometries().Value;

        StringBuilder result = new StringBuilder();

        for (int i = 1; i <= numberOfParts; i++)
        {
            result.Append(geometry.STGeometryN(i).SqlPolygonToPathMarkup(decimals));
        }

        return result.ToString();
    }

    #endregion


    #region SqlGeometry To GeoJson

    public static IGeoJsonGeometry AsGeoJson(this SqlGeometry geometry, bool isXFirst = true)
    { 
        OpenGisGeometryType geometryType = geometry.GetOpenGisType();

        switch (geometryType)
        {
            case OpenGisGeometryType.CircularString:
            case OpenGisGeometryType.CompoundCurve:
            case OpenGisGeometryType.CurvePolygon:
            case OpenGisGeometryType.GeometryCollection:
            default:
                throw new NotImplementedException();

            case OpenGisGeometryType.Point:
                return geometry.SqlPointToGeoJsonPoint(isXFirst);

            case OpenGisGeometryType.MultiPoint:
                return geometry.SqlMultiPointToGeoJsonMultiPoint(isXFirst);

            case OpenGisGeometryType.LineString:
                return geometry.SqlLineStringToGeoJsonPolyline(isXFirst);

            case OpenGisGeometryType.MultiLineString:
                return geometry.SqlMultiLineStringToGeoJsonPolyline(isXFirst);

            case OpenGisGeometryType.Polygon:
                return geometry.SqlPolygonToGeoJsonPolygon(isXFirst);

            case OpenGisGeometryType.MultiPolygon:
                return geometry.SqlMultiPolygonToGeoJsonMultiPolygon(isXFirst);
        }
    }

    private static double[] GetGeoJsonObjectPoint(SqlGeometry point, bool isXFirst)
    {
        if (point.IsNullOrEmpty())
            return new double[0];

        if (isXFirst)
        {
            return new double[] { point.STX.Value, point.STY.Value };
        }
        else
        {
            return new double[] { point.STY.Value, point.STX.Value };
        }
    }

    private static double[][] GetGeoJsonLineStringOrRing(SqlGeometry lineStringOrRing, bool isXFirst)
    {
        if (lineStringOrRing.IsNullOrEmpty())
            return new double[0][];

        int numberOfPoints = lineStringOrRing.STNumPoints().Value;

        double[][] result = new double[numberOfPoints][];

        for (int i = 1; i <= numberOfPoints; i++)
        {
            result[i - 1] = GetGeoJsonObjectPoint(lineStringOrRing.STPointN(i), isXFirst);
        }

        return result;
    }

    private static GeoJsonPoint SqlPointToGeoJsonPoint(this SqlGeometry geometry, bool isXFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonPoint()
            {
                Type = GeoJson.Point,
            };

        double[] coordinates = isXFirst ?
                                new double[] { geometry.STX.Value, geometry.STY.Value } :
                                new double[] { geometry.STY.Value, geometry.STX.Value };

        return new GeoJsonPoint()
        {
            Type = GeoJson.Point,
            Coordinates = coordinates /*new double[] { geometry.STX.Value, geometry.STY.Value }*/
        };
    }

    private static GeoJsonMultiPoint SqlMultiPointToGeoJsonMultiPoint(this SqlGeometry geometry, bool isXFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return GeoJsonMultiPoint.Empty;
        //return new GeoJsonMultiPoint()
        //{
        //    //Type = GeoJson.MultiPoint,
        //    Coordinates = [],
        //};

        var numberOfGeometries = geometry.STNumGeometries().Value;

        double[][] points = new double[numberOfGeometries][];

        for (int i = 1; i <= numberOfGeometries; i++)
        {
            points[i - 1] = GetGeoJsonObjectPoint(geometry.STGeometryN(i), isXFirst);
        }

        return new GeoJsonMultiPoint()
        {
            Coordinates = points,
            //Type = GeoJson.MultiPoint,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonLineString SqlLineStringToGeoJsonPolyline(this SqlGeometry geometry, bool isXFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonLineString()
            {
                Type = GeoJson.LineString,
                Coordinates = new double[0][],
            };

        double[][] paths = GetGeoJsonLineStringOrRing(geometry, isXFirst);

        return new GeoJsonLineString()
        {
            Coordinates = paths,
            Type = GeoJson.LineString,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonMultiLineString SqlMultiLineStringToGeoJsonPolyline(this SqlGeometry geometry, bool isXFirst)
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
            result[i - 1] = GetGeoJsonLineStringOrRing(geometry.STGeometryN(i), isXFirst);
        }

        return new GeoJsonMultiLineString()
        {
            Coordinates = result,
            Type = GeoJson.MultiLineString,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonPolygon SqlPolygonToGeoJsonPolygon(this SqlGeometry geometry, bool isXFirst)
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return new GeoJsonPolygon()
            {
                Type = GeoJson.Polygon,
                Coordinates = new double[0][][],
            };

        //********************************************************************************
        var numberOfInteriorRings = geometry.STNumInteriorRing().Value;

        double[][] exteriorRing = GetGeoJsonLineStringOrRing(geometry.STExteriorRing(), isXFirst);

        double[][][] result = new double[numberOfInteriorRings + 1][][];

        result[0] = exteriorRing;

        for (int i = 1; i <= numberOfInteriorRings; i++)
        {
            //The first result contains the exterior ring so start at i 
            result[i] = GetGeoJsonLineStringOrRing(geometry.STInteriorRingN(i), isXFirst);
        }

        //***********************************************************************************

        //int numberOfParts = geometry.STNumGeometries().Value;
        //double[][][] result = new double[numberOfParts][][];
        //for (int i = 1; i <= numberOfParts; i++)
        //{
        //    result[i - 1] = GetGeoJsonLineStringOrRing(geometry.STGeometryN(i));
        //}

        return new GeoJsonPolygon()
        {
            Coordinates = result,
            Type = GeoJson.Polygon,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonMultiPolygon SqlMultiPolygonToGeoJsonMultiPolygon(this SqlGeometry geometry, bool isXFirst)
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
            rings[i - 1] = geometry.STGeometryN(i).SqlPolygonToGeoJsonPolygon(isXFirst).Coordinates;
        }

        return new GeoJsonMultiPolygon()
        {
            Coordinates = rings,
            Type = GeoJson.MultiPolygon,
        };
    }

    #endregion


    #region Model

    public static SqlGeometry AsSqlGeometry(this Sta.Spatial.Model.TileInfo tile)
    {
        return tile.WebMercatorExtent.AsSqlGeometry();
    }

    public static Geometry<Point> AsGeometry(this Sta.Spatial.Model.TileInfo tile)
    {
        return tile.WebMercatorExtent.AsGeometry<Point>(0);
    }

    #endregion

     
}

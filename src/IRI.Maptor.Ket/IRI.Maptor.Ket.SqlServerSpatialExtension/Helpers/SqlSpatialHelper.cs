using Microsoft.SqlServer.Types;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers;

public static class SqlSpatialHelper
{
    #region SqlGeometry

    public static SqlGeometry Parse(string wkt)
    {
        return SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(wkt));
    }

    public static SqlGeometry ParseFromEsriJson(string esriGeometryJson, string type)
    {
        return Parse(EsriJsonGeometry.Parse(esriGeometryJson, EnumHelper.Parse<EsriJsonGeometryType>(type)).AsWkt());
    }

    public static void AddEmptySqlGeometry(SqlGeometryBuilder builder, GeometryType type)
    {
        switch (type)
        {
            case GeometryType.LineString:
                builder.BeginGeometry(OpenGisGeometryType.LineString);
                break;

            case GeometryType.MultiLineString:
                builder.BeginGeometry(OpenGisGeometryType.MultiLineString);
                break;

            case GeometryType.MultiPoint:
                builder.BeginGeometry(OpenGisGeometryType.MultiPoint);
                break;

            case GeometryType.MultiPolygon:
                builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);
                break;

            case GeometryType.Point:
                builder.BeginGeometry(OpenGisGeometryType.Point);
                break;

            case GeometryType.Polygon:
                builder.BeginGeometry(OpenGisGeometryType.Polygon);
                break;

            case GeometryType.GeometryCollection:
                builder.BeginGeometry(OpenGisGeometryType.GeometryCollection);
                break;

            case GeometryType.CircularString:
                builder.BeginGeometry(OpenGisGeometryType.CircularString);
                break;

            case GeometryType.CompoundCurve:
                builder.BeginGeometry(OpenGisGeometryType.CompoundCurve);
                break;

            case GeometryType.CurvePolygon:
                builder.BeginGeometry(OpenGisGeometryType.CurvePolygon);
                break;

            default:
                throw new NotImplementedException();
        }

        builder.EndGeometry();
    }

    public static SqlGeometry CreateEmptySqlGeometry(GeometryType type, int srid)
    {
        switch (type)
        {
            case GeometryType.LineString:
                return CreateEmptyLineString(srid);

            case GeometryType.MultiLineString:
                return CreateEmptyMultiLineString(srid);

            case GeometryType.MultiPoint:
                return CreateEmptyMultipoint(srid);

            case GeometryType.MultiPolygon:
                return CreateEmptyMultiPolygon(srid);

            case GeometryType.Point:
                return CreateEmptyPoint(srid);

            case GeometryType.Polygon:
                return CreateEmptyPolygon(srid);

            case GeometryType.GeometryCollection:
                return CreateEmptyGeometryCollection(srid);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:

            default:
                return null;
        }

    }

    public static SqlGeometry CreateEmptyPoint(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("POINT EMPTY"), srid);
    }

    public static SqlGeometry CreateEmptyLineString(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("LINESTRING EMPTY"), srid);
    }

    public static SqlGeometry CreateEmptyPolygon(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("POLYGON EMPTY"), srid);
    }

    public static SqlGeometry CreateEmptyMultipoint(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("MULTIPOINT EMPTY"), srid);
    }

    public static SqlGeometry CreateEmptyMultiLineString(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("MULTILINESTRING EMPTY"), srid);
    }

    public static SqlGeometry CreateEmptyMultiPolygon(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("MULTIPOLYGON EMPTY"), srid);
    }

    public static SqlGeometry CreateEmptyGeometryCollection(int srid)
    {
        return SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars("GEOMETRYCOLLECTION EMPTY"), srid);
    }

    public static BoundingBox GetBoundingBoxFromEnvelopes(List<SqlGeometry> envelopes)
    {
        if (envelopes == null)
        {
            return BoundingBox.NaN;
        }

        var validEnvelopes = envelopes/*.Where(i => !i.IsNotValidOrEmpty())*/.ToList();

        if (validEnvelopes.Count == 0)
        {
            return BoundingBox.NaN;
        }

        var xValues = validEnvelopes.SelectMany(i => new double[] { i.STPointN(1).STX.Value, i.STPointN(2).STX.Value, i.STPointN(3).STX.Value, i.STPointN(4).STX.Value });

        var yValues = validEnvelopes.SelectMany(i => new double[] { i.STPointN(1).STY.Value, i.STPointN(2).STY.Value, i.STPointN(3).STY.Value, i.STPointN(4).STY.Value });

        return new BoundingBox(xValues.Min(), yValues.Min(), xValues.Max(), yValues.Max());
    }

    public static SqlGeometry? CreateLineStringFromPoints(List<SqlGeometry> geometries)
    {
        if (geometries?.Any() != true)
        {
            return null;
        }

        var points = geometries.Select(g => g.AsPoint()).ToList();
         
        return Geometry<Point>.CreatePointOrLineString(points, geometries.SkipWhile(g => g.STSrid.IsNull)?.FirstOrDefault()?.STSrid.Value ?? 0).AsSqlGeometry();
    }

    #endregion


    #region SqlGeography

    public static void AddEmptySqlGeography(SqlGeographyBuilder builder, GeometryType type)
    {
        switch (type)
        {
            case GeometryType.LineString:
                builder.BeginGeography(OpenGisGeographyType.LineString);
                break;

            case GeometryType.MultiLineString:
                builder.BeginGeography(OpenGisGeographyType.MultiLineString);
                break;

            case GeometryType.MultiPoint:
                builder.BeginGeography(OpenGisGeographyType.MultiPoint);
                break;

            case GeometryType.MultiPolygon:
                builder.BeginGeography(OpenGisGeographyType.MultiPolygon);
                break;

            case GeometryType.Point:
                builder.BeginGeography(OpenGisGeographyType.Point);
                break;

            case GeometryType.Polygon:
                builder.BeginGeography(OpenGisGeographyType.Polygon);
                break;

            case GeometryType.GeometryCollection:
                builder.BeginGeography(OpenGisGeographyType.GeometryCollection);
                break;

            case GeometryType.CircularString:
                builder.BeginGeography(OpenGisGeographyType.CircularString);
                break;

            case GeometryType.CompoundCurve:
                builder.BeginGeography(OpenGisGeographyType.CompoundCurve);
                break;

            case GeometryType.CurvePolygon:
                builder.BeginGeography(OpenGisGeographyType.CurvePolygon);
                break;

            default:
                throw new NotImplementedException();
        }

        builder.EndGeography();
    }

    public static SqlGeography CreateEmptySqlGeography(GeometryType type, int srid)
    {
        switch (type)
        {
            case GeometryType.LineString:
                return CreateEmptyGeoLineString(srid);

            case GeometryType.MultiLineString:
                return CreateEmptyGeoMultiLineString(srid);

            case GeometryType.MultiPoint:
                return CreateEmptyGeoMultipoint(srid);

            case GeometryType.MultiPolygon:
                return CreateEmptyGeoMultiPolygon(srid);

            case GeometryType.Point:
                return CreateEmptyGeoPoint(srid);

            case GeometryType.Polygon:
                return CreateEmptyGeoPolygon(srid);

            case GeometryType.GeometryCollection:
                return CreateEmptyGeographyCollection(srid);

            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:

            default:
                return null;
        }

    }

    public static SqlGeography CreateEmptyGeoPoint(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("POINT EMPTY"), srid);
    }

    public static SqlGeography CreateEmptyGeoLineString(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("LINESTRING EMPTY"), srid);
    }

    public static SqlGeography CreateEmptyGeoPolygon(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("POLYGON EMPTY"), srid);
    }

    public static SqlGeography CreateEmptyGeoMultipoint(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("MULTIPOINT EMPTY"), srid);
    }

    public static SqlGeography CreateEmptyGeoMultiLineString(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("MULTILINESTRING EMPTY"), srid);
    }

    public static SqlGeography CreateEmptyGeoMultiPolygon(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("MULTIPOLYGON EMPTY"), srid);
    }

    public static SqlGeography CreateEmptyGeographyCollection(int srid)
    {
        return SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars("GEOGRAPHY EMPTY"), srid);
    }

    public static BoundingBox GetBoundingBoxFromEnvelopes(List<SqlGeography> envelopes)
    {
        if (envelopes == null)
        {
            return BoundingBox.NaN;
        }

        var validEnvelopes = envelopes/*.Where(i => !i.IsNotValidOrEmpty())*/.ToList();

        if (validEnvelopes.Count == 0)
        {
            return BoundingBox.NaN;
        }

        var xValues = validEnvelopes.SelectMany(i => new double[] { i.STPointN(1).Long.Value, i.STPointN(2).Long.Value, i.STPointN(3).Long.Value, i.STPointN(4).Long.Value });

        var yValues = validEnvelopes.SelectMany(i => new double[] { i.STPointN(1).Lat.Value, i.STPointN(2).Lat.Value, i.STPointN(3).Lat.Value, i.STPointN(4).Lat.Value });

        return new BoundingBox(xValues.Min(), yValues.Min(), xValues.Max(), yValues.Max());
    }
     
    #endregion 
}

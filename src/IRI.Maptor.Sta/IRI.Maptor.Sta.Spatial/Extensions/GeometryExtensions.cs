using System.Diagnostics;

using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;


namespace IRI.Maptor.Extensions;

public static class Sta_GeometryExtensions
{
    public static BoundingBox GetBoundingBox<T>(this IEnumerable<Geometry<T>> spatialFeatures) where T : IPoint, new()
    {
        if (spatialFeatures.IsNullOrEmpty() /*== null || spatialFeatures.Count < 1*/)
            return new BoundingBox(double.NaN, double.NaN, double.NaN, double.NaN);

        var envelopes = spatialFeatures.Select(i => i?.GetBoundingBox()).Where(i => i != null).Select(i => i.Value).ToList();

        return BoundingBox.GetMergedBoundingBox(envelopes, true);
    }

    public static T Project<T>(this T point, SrsBase sourceSrs, SrsBase targetSrs) where T : IPoint, new()
    {
        if (sourceSrs.Ellipsoid.AreTheSame(targetSrs.Ellipsoid))
        {
            var c1 = sourceSrs.ToGeodetic(point);

            return targetSrs.FromGeodetic(c1);
        }
        else
        {
            var c1 = sourceSrs.ToGeodetic(point);

            return targetSrs.FromGeodetic(c1, sourceSrs.Ellipsoid);
        }
    }

    public static List<Geometry<T>> Project<T>(this List<Geometry<T>> values, SrsBase sourceSrs, SrsBase targetSrs) where T : IPoint, new()
    {
        List<Geometry<T>> result = new List<Geometry<T>>(values.Count);

        if (sourceSrs.Ellipsoid.AreTheSame(targetSrs.Ellipsoid))
        {
            for (int i = 0; i < values.Count; i++)
            {
                var c1 = values[i].Transform(p => sourceSrs.ToGeodetic(p), SridHelper.GeodeticWGS84);

                result.Add(c1.Transform(p => targetSrs.FromGeodetic(p), targetSrs.Srid));
            }
        }
        else
        {
            for (int i = 0; i < values.Count; i++)
            {
                var c1 = values[i].Transform(p => sourceSrs.ToGeodetic(p), SridHelper.GeodeticWGS84);

                result.Add(c1.Transform(p => targetSrs.FromGeodetic(p, sourceSrs.Ellipsoid), targetSrs.Srid));
            }
        }

        return result;
    }

    public static bool IsNullOrEmpty<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        return geometry is null ||
                (geometry.Points.IsNullOrEmpty() &&
                    geometry.Geometries.IsNullOrEmpty()) ||
                    geometry.TotalNumberOfPoints == 0;
    }

    public static bool IsNotValidOrEmpty<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        return geometry.IsNullOrEmpty() || !geometry.IsValid();
    }


    #region Geometry To GeoJson

    // public methods
    /// <summary>
    /// Converts an IGeometry instance to a GeoJSON geometry.
    /// </summary>
    /// <param name="geometry">The geometry to convert.</param>
    /// <param name="isLongitudeFirst">If true, coordinates are output as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>An IGeoJsonGeometry instance representing the converted geometry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when geometry is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the geometry type is not supported.</exception>
    public static IGeoJsonGeometry AsGeoJson(this IGeometry geometry, bool isLongitudeFirst = true)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        return geometry switch
        {
            Geometry<Point> g => g.AsGeoJson(isLongitudeFirst),
            Geometry<PointZ> gz => gz.AsGeoJson(isLongitudeFirst),
            Geometry<PointZM> gzm => gzm.AsGeoJson(isLongitudeFirst),
            _ => throw new NotSupportedException($"Geometry type {geometry.GetType()} is not supported for GeoJSON conversion.")
        };
    }

    public static IGeoJsonGeometry AsGeoJson<T>(this T point) where T : IPoint, new()
    {
        if (point == null)
            return new GeoJsonPoint()
            {
                Type = GeoJson.Point,
            };

        return new GeoJsonPoint()
        {
            Type = GeoJson.Point,
            Coordinates = [point.X, point.Y]
        };
    }

    public static IGeoJsonGeometry AsGeoJson<T>(this Geometry<T> geometry, bool isLongitudeFirst = true) where T : IPoint, new()
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
                return geometry.GeometryPointToGeoJsonPoint(isLongitudeFirst);

            case GeometryType.LineString:
                return GeometryLineStringToGeoJsonPolyline(geometry, isLongitudeFirst);

            case GeometryType.Polygon:
                return GeometryPolygonToGeoJsonPolygon(geometry, isLongitudeFirst);

            case GeometryType.MultiPoint:
                return GeometryMultiPointToGeoJsonMultiPoint(geometry, isLongitudeFirst);

            case GeometryType.MultiLineString:
                return GeometryMultiLineStringToGeoJsonPolyline(geometry, isLongitudeFirst);

            case GeometryType.MultiPolygon:
                return GeometryMultiPolygonToGeoJsonMultiPolygon(geometry, isLongitudeFirst);

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    // private methods
    private static double[] GetGeoJsonObjectPoint<T>(T point, bool isLongitudeFirst) where T : IPoint, new()
    {
        if (point == null)
            return [];

        return point switch
        {
            PointZM pzm => isLongitudeFirst
                ? [pzm.X, pzm.Y, pzm.Z, pzm.M]
                : [pzm.Y, pzm.X, pzm.Z, pzm.M],
            PointZ pz => isLongitudeFirst
                ? [pz.X, pz.Y, pz.Z]
                : [pz.Y, pz.X, pz.Z],
            _ => isLongitudeFirst
                ? [point.X, point.Y]
                : [point.Y, point.X]
        };
    }

    private static double[][] GetGeoJsonLineStringOrRing<T>(Geometry<T> lineStringOrRing, bool isLongitudeFirst, bool isRing) where T : IPoint, new()
    {
        if (lineStringOrRing.IsNullOrEmpty())
            return [];

        int numberOfPoints = lineStringOrRing.NumberOfPoints;

        double[][] result;

        if (isRing)
        {
            // 1400.02.04
            // In GeoJson polygons, the last point must be repeated
            result = new double[numberOfPoints + 1][];

            result[numberOfPoints] = GetGeoJsonObjectPoint<T>(lineStringOrRing.Points[0], isLongitudeFirst);
        }
        else
        {
            result = new double[numberOfPoints][];
        }

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = GetGeoJsonObjectPoint<T>(lineStringOrRing.Points[i], isLongitudeFirst);
        }

        return result;
    }

    private static GeoJsonPoint GeometryPointToGeoJsonPoint<T>(this Geometry<T> point, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (point.IsNullOrEmpty())
            return GeoJsonPoint.Empty;

        var coordinates = GetGeoJsonObjectPoint(point.Points[0], isLongitudeFirst);

        return new GeoJsonPoint()
        {
            Type = GeoJson.Point,
            Coordinates = coordinates
        };
    }

    private static GeoJsonMultiPoint GeometryMultiPointToGeoJsonMultiPoint<T>(this Geometry<T> multiPoint, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (multiPoint.IsNullOrEmpty())
            return GeoJsonMultiPoint.Empty;

        var numberOfGeometries = multiPoint.NumberOfGeometries;

        double[][] points = new double[numberOfGeometries][];

        for (int i = 0; i < numberOfGeometries; i++)
        {
            points[i] = GetGeoJsonObjectPoint(multiPoint.Geometries[i].Points[0], isLongitudeFirst);
        }

        return new GeoJsonMultiPoint()
        {
            Coordinates = points,
        };
    }

    private static GeoJsonLineString GeometryLineStringToGeoJsonPolyline<T>(this Geometry<T> lineString, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (lineString.IsNullOrEmpty())
            return GeoJsonLineString.Empty;

        double[][] paths = GetGeoJsonLineStringOrRing(lineString, isLongitudeFirst, false);

        return new GeoJsonLineString()
        {
            Coordinates = paths,
            Type = GeoJson.LineString,
        };
    }

    private static GeoJsonMultiLineString GeometryMultiLineStringToGeoJsonPolyline<T>(this Geometry<T> multiLineString, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (multiLineString.IsNullOrEmpty())
            return GeoJsonMultiLineString.Empty;

        int numberOfParts = multiLineString.NumberOfGeometries;

        double[][][] result = new double[numberOfParts][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            result[i] = GetGeoJsonLineStringOrRing(multiLineString.Geometries[i], isLongitudeFirst, false);
        }

        return new GeoJsonMultiLineString()
        {
            Coordinates = result,
            Type = GeoJson.MultiLineString,
        };
    }

    private static GeoJsonPolygon GeometryPolygonToGeoJsonPolygon<T>(this Geometry<T> polygon, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (polygon.IsNullOrEmpty())
            return GeoJsonPolygon.Empty;


        int numberOfParts = polygon.NumberOfGeometries;

        double[][][] result = new double[numberOfParts][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            result[i] = GetGeoJsonLineStringOrRing(polygon.Geometries[i], isLongitudeFirst, true);
        }

        return new GeoJsonPolygon()
        {
            Coordinates = result,
            Type = GeoJson.Polygon,
        };
    }

    private static GeoJsonMultiPolygon GeometryMultiPolygonToGeoJsonMultiPolygon<T>(this Geometry<T> multiPolygon, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (multiPolygon.IsNullOrEmpty())
            return GeoJsonMultiPolygon.Empty;

        int numberOfParts = multiPolygon.NumberOfGeometries;

        double[][][][] rings = new double[numberOfParts][][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            rings[i] = multiPolygon.Geometries[i].GeometryPolygonToGeoJsonPolygon(isLongitudeFirst).Coordinates;
        }

        return new GeoJsonMultiPolygon()
        {
            Coordinates = rings,
            Type = GeoJson.MultiPolygon,
        };
    }

    #endregion


    #region To EsriJsonGeometry


    public static EsriJsonGeometry AsEsriJsonGeometry<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
                return geometry.PointToEsriJsonPoint();

            case GeometryType.LineString:
                return geometry.LineStringToEsriJsonPolyline();

            case GeometryType.Polygon:
                return geometry.PolygonToEsriJsonPolygon();

            case GeometryType.MultiPoint:
                return geometry.MultiPointToEsriJsonMultiPoint();

            case GeometryType.MultiLineString:
                return geometry.MultiLineStringToEsriJsonPolyline();

            case GeometryType.MultiPolygon:
                return geometry.MultiPolygonToEsriJsonMultiPolygon();

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.None:
            default:
                throw new NotImplementedException("Sta_GeometryExtensions > AsEsriJsonGeometry");
        }

    }

    private static double?[] GetEsriJsonObjectPoint<T>(T point) where T : IPoint, new()
    {
        if (point.IsNaN())
            return [];

        return point switch
        {
            PointZM pzm => [pzm.X, pzm.Y, pzm.Z, pzm.M],
            PointZ pz => [pz.X, pz.Y, pz.Z],
            _ => [point.X, point.Y]
        };

    }

    private static double?[][] GetLineStringOrRing<T>(Geometry<T> lineStringOrRing, bool isRing) where T : IPoint, new()
    {
        if (lineStringOrRing.IsNullOrEmpty())
            return [];

        int numberOfPoints = lineStringOrRing.NumberOfPoints;

        double?[][] result = new double?[numberOfPoints][];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = GetEsriJsonObjectPoint(lineStringOrRing.Points[i]);
        }

        if (isRing)
        {
            Array.Reverse(result);
        }

        // ring orientation is different for Geometry<T> and esri json geometries
        //return isRing ? result.Reverse().ToArray() : result;
        return result;
    }

    private static EsriJsonGeometry PointToEsriJsonPoint<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return EsriJsonGeometry.CreateEmpty(EsriJsonGeometryType.esriGeometryPoint);

        var point = geometry.AsPoint();

        var result = new EsriJsonGeometry()
        {
            X = point.X,
            Y = point.Y,
            Type = EsriJsonGeometryType.esriGeometryPoint,
            SpatialReference = new EsriJsonSpatialReference() { Wkid = geometry.Srid }
        };

        if (point is PointZM pointZm)
        {
            result.Z = pointZm.Z;
            result.M = pointZm.M;
        }
        else if (point is PointZ pointZ)
        {
            result.Z = pointZ.Z;
        }

        return result;
    }

    private static EsriJsonGeometry MultiPointToEsriJsonMultiPoint<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return EsriJsonGeometry.CreateEmpty(EsriJsonGeometryType.esriGeometryMultipoint);
        //{
        //    Type = EsriJsonGeometryType.multipoint,
        //    Points = new double?[0][],
        //};

        var numberOfGeometries = geometry.NumberOfGeometries;

        double?[][] points = new double?[numberOfGeometries][];

        for (int i = 0; i < numberOfGeometries; i++)
        {
            points[i] = GetEsriJsonObjectPoint(geometry.Geometries[i].AsPoint());
        }

        return new EsriJsonGeometry()
        {
            Points = points,
            Type = EsriJsonGeometryType.esriGeometryMultipoint,
            SpatialReference = new EsriJsonSpatialReference() { Wkid = geometry.Srid }
        };
    }

    private static EsriJsonGeometry LineStringToEsriJsonPolyline<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return EsriJsonGeometry.CreateEmpty(EsriJsonGeometryType.esriGeometryPolyline);
        //return new EsriJsonGeometry()
        //{
        //    Type = EsriJsonGeometryType.polyline,
        //    Paths = [],
        //};

        double?[][][] paths = [GetLineStringOrRing(geometry, isRing: false)];

        return new EsriJsonGeometry()
        {
            Paths = paths,
            Type = EsriJsonGeometryType.esriGeometryPolyline,
            SpatialReference = new EsriJsonSpatialReference() { Wkid = geometry.Srid }
        };
    }

    private static EsriJsonGeometry MultiLineStringToEsriJsonPolyline<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return EsriJsonGeometry.CreateEmpty(EsriJsonGeometryType.esriGeometryPolyline);
        //return new EsriJsonGeometry()
        //{
        //    Type = EsriJsonGeometryType.polyline,
        //    Paths = new double?[0][][],
        //};

        int numberOfParts = geometry.NumberOfGeometries;

        double?[][][] result = new double?[numberOfParts][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            result[i] = GetLineStringOrRing(geometry.Geometries[i], isRing: false);
        }

        return new EsriJsonGeometry()
        {
            Paths = result,
            Type = EsriJsonGeometryType.esriGeometryPolyline,
            SpatialReference = new EsriJsonSpatialReference() { Wkid = geometry.Srid }
        };
    }

    //todo: 1399.08.19; this method is not OK, look at SqlGeometry To Geometry
    private static EsriJsonGeometry PolygonToEsriJsonPolygon<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return EsriJsonGeometry.CreateEmpty(EsriJsonGeometryType.esriGeometryPolygon);
        //return new EsriJsonGeometry()
        //{
        //    Type = EsriJsonGeometryType.polygon,
        //    Rings = [],
        //};

        int numberOfParts = geometry.NumberOfGeometries;

        //double?[][][] rings = new double?[1][][] { GetLineStringOrRing(geometry) };
        double?[][][] rings = new double?[numberOfParts][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            rings[i] = GetLineStringOrRing(geometry.Geometries[i], isRing: true);
        }

        return new EsriJsonGeometry()
        {
            Rings = rings,
            Type = EsriJsonGeometryType.esriGeometryPolygon,
            SpatialReference = new EsriJsonSpatialReference() { Wkid = geometry.Srid }
        };
    }

    private static EsriJsonGeometry MultiPolygonToEsriJsonMultiPolygon<T>(this Geometry<T> geometry) where T : IPoint, new()
    {
        //This check is required
        if (geometry.IsNullOrEmpty())
            return EsriJsonGeometry.CreateEmpty(EsriJsonGeometryType.esriGeometryPolygon);
        //return new EsriJsonGeometry()
        //{
        //    Type = EsriJsonGeometryType.polygon,
        //    Rings = new double?[0][][],
        //};

        int numberOfParts = geometry.NumberOfGeometries;

        double?[][][] rings = new double?[numberOfParts][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            rings[i] = GetLineStringOrRing(geometry.Geometries[i], isRing: true);
        }

        return new EsriJsonGeometry()
        {
            Rings = rings,
            Type = EsriJsonGeometryType.esriGeometryPolygon,
            SpatialReference = new EsriJsonSpatialReference() { Wkid = geometry.Srid }
        };
    }


    #endregion


    #region Geometry To Dxf

    /// <summary>
    /// Converts the geometry to a DXF string
    /// </summary>
    /// <param name="geometry">The geometry to convert</param>
    /// <returns>DXF file content as string</returns>
    public static string ToDxf(this Geometry<Point> geometry)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        DxfWriter.ResetHandleCounter();

        return DxfWriter.Write(geometry, null);
    }

    /// <summary>
    /// Saves the geometry to a DXF file
    /// </summary>
    /// <param name="geometry">The geometry to save</param>
    /// <param name="filePath">The path to save the DXF file</param>
    /// <returns>The path to the saved file</returns>
    public static async Task SaveAsDxfAsync(this Geometry<Point> geometry, string filePath)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        DxfWriter.ResetHandleCounter();

        await DxfWriter.WriteToFileAsync(geometry, filePath);
    }

    #endregion


    #region Simplification

    public static List<Geometry<Point>> Simplify(
      this IEnumerable<Geometry<Point>> geometries,
      SimplificationType type,
      SimplificationParamters paramters,
      bool reduceToPoint = true)
    {
        try
        {
            if (geometries.IsNullOrEmpty())
                return new List<Geometry<Point>>();

            var result = new List<Geometry<Point>>();

            foreach (var geometry in geometries)
            {
                var simplified = geometry.Simplify(type, paramters);

                if (!simplified.IsNullOrEmpty())
                {
                    result.Add(geometry.Simplify(type, paramters));
                }
            }

            if (reduceToPoint)
            {
                for (int g = 0; g < result.Count; g++)
                {
                    //try
                    //{
                    var length = result[g].GetEuclideanLength();

                    if (length < paramters.DistanceThreshold)
                    {
                        //result[g] = result[g].STPointOnSurface();
                        result[g] = result[g].GetLastPoint().AsGeometry(result[g].Srid);
                    }
                    //}
                    //catch (Exception)
                    //{
                    //    throw;
                    //}
                }

                result = result.RemoveOverlappingPoints(paramters.DistanceThreshold!.Value);
            }

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static List<Geometry<Point>> RemoveOverlappingPoints(this List<Geometry<Point>> source, double minDistance)
    {
        try
        {
            List<Geometry<Point>> result = new List<Geometry<Point>>();

            if (source == null || source.Count < 1)
                return result;

            var points = source.Where(i => i.Type == GeometryType.Point).Select(i => new Point(i.Points[0].X, i.Points[0].Y)).ToList();

            if (points.IsNullOrEmpty())
                return new List<Geometry<Point>>();

            Stopwatch watch = Stopwatch.StartNew();

            //var cFast = KdTreePointClusters<Point>.GetClusterCenters(points, Point.NaN, minDistance).Count;


            ////************************************************************************************************
            //watch.Stop();
            //var tFast = watch.ElapsedMilliseconds / 1000;
            //watch.Restart();
            ////************************************************************************************************


            //var clusters = new PointClusters<Point>(points);
            //var cSlow = clusters.GetClusters((p1, p2) => Point.EuclideanDistance(p1, p2) < minDistance).Count;


            ////************************************************************************************************
            //watch.Stop();
            //var tSlow = watch.ElapsedMilliseconds / 1000;
            //watch.Restart();
            ////************************************************************************************************


            var kdtreeCluster = new KdTreePointClusters<Point>(points, new Group<Point>(Point.NaN));
            //kdtreeCluster.GetClusters((p1, p2) => Point.EuclideanDistance(p1, p2) < minDistance);
            kdtreeCluster.GetClusters((p1, p2) => SpatialUtility.GetEuclideanLength(p1, p2) < minDistance);


            //************************************************************************************************
            watch.Stop();
            var tNormal = watch.ElapsedMilliseconds / 1000;
            watch.Restart();
            //************************************************************************************************


            var centers = kdtreeCluster.GetGroupCenters();


            //************************************************************************************************
            watch.Stop();
            var tGetGroupCenters = watch.ElapsedMilliseconds / 1000;
            watch.Restart();
            //************************************************************************************************


            for (int i = 0; i < source.Count; i++)
            {
                try
                {
                    if (source[i].IsNullOrEmpty())
                        continue;

                    if (source[i].Type == GeometryType.Point)
                    {
                        result.Add(source[i]);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }



            //************************************************************************************************
            watch.Stop();
            var tAddNonPoints = watch.ElapsedMilliseconds / 1000;//System.Diagnostics.Debug.WriteLine($"\t\tADDNONPOINTS {watch.ElapsedMilliseconds / 1000} s", "PYRAMID");
            watch.Restart();
            //************************************************************************************************



            var srid = source.FirstOrDefault(i => !i.IsNullOrEmpty()).Srid;

            result.AddRange(centers.Select(i => i.AsGeometry(srid)));



            //************************************************************************************************
            watch.Stop();
            var tAddPoints = watch.ElapsedMilliseconds / 1000;//System.Diagnostics.Debug.WriteLine($"\t\tADDPOINTS {centers.Count} - {watch.ElapsedMilliseconds / 1000} s", "PYRAMID");
            watch.Restart();
            //************************************************************************************************


            //Debug.WriteLine($"\t\t [Points :{points.Count}] , [Slow-(c: {cSlow} , t: {tSlow})], [Normal-(c:{centers.Count} , t: {tNormal})]", "PYRAMID");

            Debug.WriteLine($"\t\t GetGroupCenters: {tGetGroupCenters}, AddNonPoints: {tAddNonPoints}, AddPoints: {tAddPoints}", "PYRAMID");

            return result;
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    #endregion
}

using System.Diagnostics;

using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;


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

        if (isLongitudeFirst)
        {
            return [point.X, point.Y];
        }
        else
        {
            return [point.Y, point.X];
        }

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
            // In GeoJson polygons the last point must be repeated
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
            return new GeoJsonPoint()
            {
                Type = GeoJson.Point,
            };

        if (isLongitudeFirst)
        {
            return new GeoJsonPoint()
            {
                Type = GeoJson.Point,
                Coordinates = [point.Points[0].X, point.Points[0].Y]
            };
        }
        else
        {
            return new GeoJsonPoint()
            {
                Type = GeoJson.Point,
                Coordinates = [point.Points[0].Y, point.Points[0].X]
            };
        }

    }

    private static GeoJsonMultiPoint GeometryMultiPointToGeoJsonMultiPoint<T>(this Geometry<T> multiPoint, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (multiPoint.IsNullOrEmpty())
            return GeoJsonMultiPoint.Empty;
        //return new GeoJsonMultiPoint()
        //{                
        //    Coordinates = [],
        //};

        var numberOfGeometries = multiPoint.NumberOfGeometries;

        double[][] points = new double[numberOfGeometries][];

        for (int i = 0; i < numberOfGeometries; i++)
        {
            points[i] = GetGeoJsonObjectPoint(multiPoint.Geometries[i].Points[0], isLongitudeFirst);
        }

        return new GeoJsonMultiPoint()
        {
            Coordinates = points,
            //Type = GeoJson.MultiPoint,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonLineString GeometryLineStringToGeoJsonPolyline<T>(this Geometry<T> lineString, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (lineString.IsNullOrEmpty())
            return new GeoJsonLineString()
            {
                Type = GeoJson.LineString,
                Coordinates = [],
            };

        double[][] paths = GetGeoJsonLineStringOrRing(lineString, isLongitudeFirst, false);

        return new GeoJsonLineString()
        {
            Coordinates = paths,
            Type = GeoJson.LineString,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonMultiLineString GeometryMultiLineStringToGeoJsonPolyline<T>(this Geometry<T> multiLineString, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (multiLineString.IsNullOrEmpty())
            return new GeoJsonMultiLineString()
            {
                Type = GeoJson.MultiLineString,
                Coordinates = [],
            };

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

    //Not supportig Z and M Values
    private static GeoJsonPolygon GeometryPolygonToGeoJsonPolygon<T>(this Geometry<T> polygon, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (polygon.IsNullOrEmpty())
            return new GeoJsonPolygon()
            {
                Type = GeoJson.Polygon,
                Coordinates = [],
            };


        int numberOfParts = polygon.NumberOfGeometries;

        double[][][] result = new double[numberOfParts][][];

        for (int i = 0; i < numberOfParts; i++)
        {
            result[i] = GetGeoJsonLineStringOrRing(polygon.Geometries[i], isLongitudeFirst, true);
        }

        //double[][][] rings = new double[1][][] { GetGeoJsonLineStringOrRing(geometry) };

        return new GeoJsonPolygon()
        {
            Coordinates = result,
            Type = GeoJson.Polygon,
        };
    }

    //Not supportig Z and M Values
    private static GeoJsonMultiPolygon GeometryMultiPolygonToGeoJsonMultiPolygon<T>(this Geometry<T> multiPolygon, bool isLongitudeFirst) where T : IPoint, new()
    {
        //This check is required
        if (multiPolygon.IsNullOrEmpty())
            return new GeoJsonMultiPolygon()
            {
                Type = GeoJson.MultiPolygon,
                Coordinates = [],
            };

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
                    var length = result[g].CalculateEuclideanLength();

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
            kdtreeCluster.GetClusters((p1, p2) => SpatialUtility.GetEuclideanDistance(p1, p2) < minDistance);


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

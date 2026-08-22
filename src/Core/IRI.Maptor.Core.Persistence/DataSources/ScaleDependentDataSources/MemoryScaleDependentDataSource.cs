//using System.Data;
//using System.Linq;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//using IRI.Maptor.Extensions;
//using IRI.Maptor.Core.Spatial.Helpers;
//using IRI.Maptor.Core.Spatial.Analysis;
//using IRI.Maptor.Core.Common.Primitives;
//using IRI.Maptor.Core.Spatial.Primitives;

//using static System.Math; 

//namespace IRI.Maptor.Core.Persistence.DataSources;

//public class MemoryScaleDependentDataSource<TGeometryAware> : MemoryDataSource<TGeometryAware>, IScaleDependentDataSource where TGeometryAware : class, IGeometryAware<Point>
//{
//    Dictionary<double, List<Geometry<Point>>> source;

//    double averageLatitude = 30;

//    //average latitude is assumed to be 30
//    public MemoryScaleDependentDataSource(List<Geometry<Point>> geometries)
//    {
//        source = new Dictionary<double, List<Geometry<Point>>>();

//        var boundingBox = geometries.GetBoundingBox();

//        var fitLevel = WebMercatorUtility.EstimateZoomLevel(Max(boundingBox.Width, boundingBox.Height), /*averageLatitude,*/ 900);

//        GeometryType = geometries.First().Type;

//        var simplifiedByAngleGeometries = geometries
//                                            .Select(g => g.Simplify(SimplificationType.CumulativeAngle, new SimplificationParameters()
//                                            {
//                                                AngleThreshold = .98,
//                                                Retain3Points = true
//                                            }))
//                                            .Where(g => !g.IsNullOrEmpty())
//                                            .ToList();

//        for (int i = fitLevel; i < 18; i += 4)
//        {
//            var threshold = WebMercatorUtility.CalculateGroundResolution(i, 0);

//            Debug.Print($"threshold: {threshold}, level:{i}");

//            var inverseScale = WebMercatorUtility.ZoomLevels.Single(z => z.ZoomLevel == i).InverseScale;

//            source.Add(inverseScale, simplifiedByAngleGeometries.Select(g => g.Simplify(SimplificationType.CumulativeTriangleRoutine, new SimplificationParameters()
//                                                                {
//                                                                    AreaThreshold = threshold * threshold,
//                                                                    Retain3Points = true
//                                                                }))
//                                                                .Where(g => !g.IsNotValidOrEmpty())
//                                                                .ToList());
//        }
//    }

//    public List<Geometry<Point>> GetGeometries(double scale)
//    {
//        var availableScales = source.Select(k => k.Key).ToList();

//        var selectedScale = WebMercatorUtility.GetUpperLevel(scale, availableScales);

//        return source[selectedScale];
//    }

//    public List<Geometry<Point>> GetGeometries(double scale, BoundingBox boundingBox)
//    {
//        Geometry<Point> boundary = boundingBox.AsGeometry<Point>(GetSrid());

//        return GetGeometries(scale, boundary);

//    }

//    public List<Geometry<Point>> GetGeometries(double scale, Geometry<Point> geometry)
//    {
//        return GetGeometries(scale).AsParallel().Where(i => i.Intersects(geometry)).ToList();
//    }



//    public Task<List<Geometry<Point>>> GetGeometriesAsync(double scale)
//    {
//        return Task.Run(() => { return GetGeometries(scale); });
//    }

//    public Task<List<Geometry<Point>>> GetGeometriesAsync(double scale, BoundingBox boundingBox)
//    {
//        return Task.Run(() => { return GetGeometries(scale, boundingBox); });
//    }

//}

//public class MemoryScaleDependentDataSource : MemoryDataSource, IScaleDependentDataSource
//{
//    Dictionary<double, List<Geometry<Point>>> source;

//    double averageLatitude = 30;

//    //average latitude is assumed to be 30
//    public MemoryScaleDependentDataSource(List<Geometry<Point>> geometries)
//    {
//        source = new Dictionary<double, List<Geometry<Point>>>();

//        GeometryType = geometries.First().Type;

//        var boundingBox = geometries.GetBoundingBox();

//        var fitLevel = WebMercatorUtility.EstimateZoomLevel(Max(boundingBox.Width, boundingBox.Height), /*averageLatitude, */900);

//        var simplifiedByAngleGeometries = geometries.Select(g => g.Simplify(SimplificationType.CumulativeAngle, new SimplificationParameters() { AngleThreshold = .98, Retain3Points = true })).Where(g => !g.IsNullOrEmpty()).ToList();

//        for (int i = fitLevel; i < 18; i += 4)
//        {
//            var threshold = WebMercatorUtility.CalculateGroundResolution(i, 0);

//            Debug.Print($"threshold: {threshold}, level:{i}");

//            var inverseScale = WebMercatorUtility.ZoomLevels.Single(z => z.ZoomLevel == i).InverseScale;

//            source.Add(inverseScale, simplifiedByAngleGeometries.Select(g => g.Simplify(SimplificationType.CumulativeTriangleRoutine, new SimplificationParameters() { AreaThreshold = threshold * threshold, Retain3Points = true })).Where(g => !g.IsNotValidOrEmpty()).ToList());
//        }
//    }

//    public List<Geometry<Point>> GetGeometries(double scale)
//    {
//        var availableScales = source.Select(k => k.Key).ToList();

//        var selectedScale = WebMercatorUtility.GetUpperLevel(scale, availableScales);

//        return source[selectedScale];
//    }

//    public List<Geometry<Point>> GetGeometries(double scale, BoundingBox boundingBox)
//    {
//        Geometry<Point> boundary = boundingBox.AsGeometry<Point>(GetSrid());

//        return GetGeometries(scale, boundary);
//    }

//    public List<Geometry<Point>> GetGeometries(double scale, Geometry<Point> geometry)
//    {
//        return GetGeometries(scale).AsParallel().Where(i => i.Intersects(geometry)).ToList();
//    }



//    //public Task<List<Geometry<Point>>> GetGeometriesAsync(double scale)
//    //{
//    //    return Task.Run(() => { return GetGeometries(scale); });
//    //}

//    //public Task<List<Geometry<Point>>> GetGeometriesAsync(double scale, BoundingBox boundingBox)
//    //{
//    //    return Task.Run(() => { return GetGeometries(scale, boundingBox); });
//    //}

//}

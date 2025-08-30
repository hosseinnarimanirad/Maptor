using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;


namespace IRI.Maptor.Res.TrajectoryCompression;

public static class LRHelper
{
    private static bool retain3Points = true;

    private static async Task CompareLayers(string shpFile, StreamWriter writer, string outputDirectory)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //var groundResolutionCoefs = new List<double>()
        //    {
        //        1.0 / 6,
        //        1.0 / 5,
        //        1.0 / 4,
        //        1.0 / 3,
        //        1.0 / 2,
        //        1,
        //        2,
        //        3,
        //        4,
        //        5,
        //        6
        //    };

        //var groundResolutionCoefs_Range = groundResolutionCoefs.Max() - groundResolutionCoefs.Min();
        //var groundResolutionCoefs_Min = groundResolutionCoefs.Min();

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                //.Where(g => g.TotalNumberOfPoints > 15 /*&& g.TotalNumberOfPoints < 500*/)
                                .Where(g => g.TotalNumberOfPoints < 2000)
                                .ToList();

        var temp = features.OrderByDescending(f => f.TotalNumberOfPoints).ToList();

        foreach (var feature in features)
            feature.RemoveConsecutiveDuplicatePoints();

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                //SimplificationType.EuclideanDistance,

                SimplificationType.RamerDouglasPeucker,
                //SimplificationType.ReumannWitkam,

                //SimplificationType.PerpendicularDistance,

                SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                //SimplificationType.TriangleRoutine,
                SimplificationType.APSC
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        //int featureIndex = 0;

        var lrModels = GetLRAModels();

        //foreach (var feature in features)
        //{
        //featureIndex++;

        var boundingBox = features.GetBoundingBox();

        var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 256, 256) - 4;

        var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

        var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

        var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

        var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

        originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

        //var toScreenMap2 = this.Presenter.CreateToScreenMapFunc();
        var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

        var threshold = webMercatorResolution /** coef*/;

        var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

        foreach (var lrModel in lrModels)
        {
            stopwatch.Restart();

            var simplified = features.Select(f => f.Simplify(lrModel, toScreenMap3, true)).Where(f => f.Type != GeometryType.Point).ToList(); ;

            stopwatch.Stop();

            var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-Logistic-{lrModel.Title}", simplified);

            var diff = await GeneralHelper.CreateImages(boundingBox, estimatedZoomLevel, originalBitmap, vectorLayer, outputDirectory, fileName, lrModel.Title);

            //var parameters = new SimplificationParamters() { AreaThreshold = webMercatorResolution * webMercatorResolution, DistanceThreshold = webMercatorResolution, Retain3Points = retain3Points };

            writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, estimatedZoomLevel, lrModel.Title, stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());
        }

        //foreach (var coef in groundResolutionCoefs)
        //{
        // ***************************************

        foreach (var method in methods)
        {
            stopwatch.Restart();

            var simplified = features.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

            stopwatch.Stop();

            var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-{method}", simplified);

            //this.Presenter.AddLayer(vectorLayer);

            var diff = await GeneralHelper.CreateImages(boundingBox, estimatedZoomLevel, originalBitmap, vectorLayer, outputDirectory, fileName, $"{method}"/*, coef*/);

            writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, estimatedZoomLevel, methodNames[method], stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());

            //var shapes = simplified.Select(s => s.AsSqlGeometry().AsEsriShape());
            //Shapefile.Save($"{outputDirectory}\\{fileName}-{level}.shp", shapes, true, true, DefaultMapProjections.WebMercator);
        }
        //}
        //}
    }

    // Goodv1
    private static async Task CompareLayersMultiScale(string shpFile, StreamWriter writer, string outputDirectory, List<(string, LogisticSimplification<Point>)> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                //.Where(g => g.TotalNumberOfPoints > 15 /*&& g.TotalNumberOfPoints < 500*/)
                                .Where(g => g.TotalNumberOfPoints > 15 && g.TotalNumberOfPoints < 2000)
                                .ToList();

        //var temp = features.OrderByDescending(f => f.TotalNumberOfPoints).ToList();

        foreach (var feature in features)
            feature.RemoveConsecutiveDuplicatePoints();

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.EuclideanDistance,

                SimplificationType.RamerDouglasPeucker,
                //SimplificationType.ReumannWitkam,

                //SimplificationType.PerpendicularDistance,

                SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                //SimplificationType.TriangleRoutine,
                SimplificationType.APSC
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        //var lrModels = GetLRAModels();

        var boundingBox = features.GetBoundingBox();

        var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 2048, 2048) + 1;

        var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

        var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

        var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

        var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

        originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

        //var toScreenMap2 = this.Presenter.CreateToScreenMapFunc();
        var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

        var threshold = webMercatorResolution /** coef*/;

        var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

        foreach (var lrModel in lrModels)
        {
            stopwatch.Restart();

            var simplified = features.Select(f => f.Simplify(lrModel.Item2, toScreenMap3, true)).Where(f => f.Type != GeometryType.Point).ToList(); ;

            stopwatch.Stop();

            var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-Logistic-{lrModel.Item1}", simplified);

            var diff = await GeneralHelper.CreateImages(
                boundingBox,
                estimatedZoomLevel,
                originalBitmap,
                vectorLayer,
                outputDirectory,
                fileName,
                $"{lrModel.Item1}",
                saveImages: false);

            writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, estimatedZoomLevel, lrModel.Item1.ToString(), stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());
        }

        foreach (var method in methods)
        {
            stopwatch.Restart();

            var simplified = features.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

            stopwatch.Stop();

            var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-{method}", simplified);

            //this.Presenter.AddLayer(vectorLayer);

            var diff = await GeneralHelper.CreateImages(
                boundingBox,
                estimatedZoomLevel,
                originalBitmap,
                vectorLayer,
                outputDirectory,
                fileName,
                $"{method}",
                saveImages: false);

            writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, estimatedZoomLevel, methodNames[method], stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());

            //var shapes = simplified.Select(s => s.AsSqlGeometry().AsEsriShape());
            //Shapefile.Save($"{outputDirectory}\\{fileName}-{level}.shp", shapes, true, true, DefaultMapProjections.WebMercator);
        }
    }

    // Goodv2
    private static async Task CompareLayersPyramidByFixScreen(string shpFile, StreamWriter writer, string outputDirectory, List<(string, LogisticSimplification<Point>)> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                //.Where(g => g.TotalNumberOfPoints > 15 /*&& g.TotalNumberOfPoints < 500*/)
                                .Where(g => /*g.TotalNumberOfPoints > 15 &&*/ g.TotalNumberOfPoints < 4000)
                                .ToList();

        //var temp = features.OrderByDescending(f => f.TotalNumberOfPoints).ToList();

        foreach (var feature in features)
            feature.RemoveConsecutiveDuplicatePoints();

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.EuclideanDistance,

                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                SimplificationType.PerpendicularDistance,

                SimplificationType.VisvalingamWhyatt,
                SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                SimplificationType.TriangleRoutine,
                SimplificationType.APSC
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        //var lrModels = GetLRAModels();

        var boundingBox = features.GetBoundingBox();

        var pyramid = new int[] { 256, 1024, 2048, 4096 };

        foreach (var level in pyramid)
        {
            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, level, level);

            var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

            var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

            //originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

            //var toScreenMap2 = this.Presenter.CreateToScreenMapFunc();
            var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var threshold = webMercatorResolution /** coef*/;

            var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

            foreach (var lrModel in lrModels)
            {
                stopwatch.Restart();

                var simplified = features.Select(f => f.Simplify(lrModel.Item2, toScreenMap3, true)).Where(f => f.Type != GeometryType.Point).ToList(); ;

                stopwatch.Stop();

                var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-{lrModel.Item1}", simplified);

                var diff = await GeneralHelper.CreateImages(
                    boundingBox,
                    estimatedZoomLevel,
                    originalBitmap,
                    vectorLayer,
                    outputDirectory,
                    fileName,
                    $"{lrModel.Item1}",
                    saveImages: true);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, estimatedZoomLevel, lrModel.Item1, stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());
            }

            foreach (var method in methods)
            {
                stopwatch.Restart();

                var simplified = features.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

                stopwatch.Stop();

                var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-{method}", simplified);

                //this.Presenter.AddLayer(vectorLayer);

                var diff = await GeneralHelper.CreateImages(
                    boundingBox,
                    estimatedZoomLevel,
                    originalBitmap,
                    vectorLayer,
                    outputDirectory,
                    fileName,
                    $"{method}",
                    saveImages: false);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, estimatedZoomLevel, methodNames[method], stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());

                //var shapes = simplified.Select(s => s.AsSqlGeometry().AsEsriShape());
                //Shapefile.Save($"{outputDirectory}\\{fileName}-{level}.shp", shapes, true, true, DefaultMapProjections.WebMercator);
            }
        }
    }

    private static async Task CompareLayersPyramidByZoomLevel(string shpFile, StreamWriter writer, string outputDirectory, List<(string, LogisticSimplification<Point>)> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                //.SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                //.Where(g => g.TotalNumberOfPoints > 15 /*&& g.TotalNumberOfPoints < 500*/)
                                .Where(g => /*g.TotalNumberOfPoints > 15 &&*/ g.TotalNumberOfPoints < 4000)
                                .ToList();

        //var temp = features.OrderByDescending(f => f.TotalNumberOfPoints).ToList();

        foreach (var feature in features)
            feature.RemoveConsecutiveDuplicatePoints();

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                //SimplificationType.EuclideanDistance,

                SimplificationType.RamerDouglasPeucker,
                //SimplificationType.ReumannWitkam,

                //SimplificationType.PerpendicularDistance,

                SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                //SimplificationType.TriangleRoutine,
                SimplificationType.APSC
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        //var lrModels = GetLRAModels();

        var boundingBox = features.GetBoundingBox();

        var zoomLevels = new int[] { 10, 11, 12, 13, 14, 15 };

        foreach (var zoomLevel in zoomLevels)
        {
            //var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, level, level);

            var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);
            //var currentScreenSize = new System.Drawing.Size(4096, 4096);

            var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

            var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

            originalBitmap.Save($"{outputDirectory}\\{fileName}-{zoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

            //var toScreenMap2 = this.Presenter.CreateToScreenMapFunc();
            var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var threshold = webMercatorResolution /** coef*/;

            var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

            foreach (var lrModel in lrModels)
            {
                stopwatch.Restart();

                var simplified = features.Select(f => f.Simplify(lrModel.Item2, toScreenMap3, true)).Where(f => f.Type != GeometryType.Point).ToList(); ;

                stopwatch.Stop();

                var vectorLayer = GeneralHelper.GetAsLayer($"S-{zoomLevel}-{lrModel.Item1}", simplified);

                var diff = await GeneralHelper.CreateImages(
                    boundingBox,
                    zoomLevel,
                    originalBitmap,
                    vectorLayer,
                    outputDirectory,
                    fileName,
                    $"{lrModel.Item1}",
                    saveImages: true);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, zoomLevel, lrModel.Item1.ToString(), stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());
            }

            foreach (var method in methods)
            {
                stopwatch.Restart();

                var simplified = features.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

                stopwatch.Stop();

                var vectorLayer = GeneralHelper.GetAsLayer($"S-{zoomLevel}-{method}", simplified);

                //this.Presenter.AddLayer(vectorLayer);

                var diff = await GeneralHelper.CreateImages(
                    boundingBox,
                    zoomLevel,
                    originalBitmap,
                    vectorLayer,
                    outputDirectory,
                    fileName,
                    $"{method}",
                    saveImages: true);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, zoomLevel, methodNames[method], stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());

                //var shapes = simplified.Select(s => s.AsSqlGeometry().AsEsriShape());
                //Shapefile.Save($"{outputDirectory}\\{fileName}-{level}.shp", shapes, true, true, DefaultMapProjections.WebMercator);
            }
        }
    }


    private static async Task CompareSingleFeature(string shpFile, StreamWriter writer, string outputDirectory, List<(string, LogisticSimplification<Point>)> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.TotalNumberOfPoints > 20 && g.TotalNumberOfPoints < 800)
                                .OrderBy(g => Guid.NewGuid())
                                .Take(100)
                                .ToList();

        var temp = features.OrderByDescending(f => f.TotalNumberOfPoints).ToList();

        foreach (var feature in features)
            feature.RemoveConsecutiveDuplicatePoints();

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                //SimplificationType.EuclideanDistance,

                SimplificationType.RamerDouglasPeucker,
                //SimplificationType.ReumannWitkam,

                //SimplificationType.PerpendicularDistance,

                SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                //SimplificationType.TriangleRoutine,
                SimplificationType.APSC
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 256, 256);

            var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", new() { feature });

            var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

            //originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

            var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var threshold = webMercatorResolution   /** coef*/;

            var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

            foreach (var lrModel in lrModels)
            {
                stopwatch.Restart();

                var simplified = feature.Simplify(lrModel.Item2, toScreenMap3, true);

                if (simplified.Type == GeometryType.Point)
                    continue;

                stopwatch.Stop();

                var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-{lrModel.Item1}-{featureIndex}", new() { simplified });

                var diff = await GeneralHelper.CreateImages(boundingBox, estimatedZoomLevel, originalBitmap, vectorLayer, outputDirectory, fileName, lrModel.Item1, saveImages: false);

                //var parameters = new SimplificationParamters() { AreaThreshold = webMercatorResolution * webMercatorResolution, DistanceThreshold = webMercatorResolution, Retain3Points = retain3Points };

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, simplified, estimatedZoomLevel, lrModel.Item1, stopwatch.ElapsedMilliseconds, 0, parameters).ToTsv());
            }

            //foreach (var coef in groundResolutionCoefs)
            //{
            // ***************************************

            foreach (var method in methods)
            {
                stopwatch.Restart();

                var simplified = feature.Simplify(method, parameters);

                if (simplified.Type == GeometryType.Point)
                    continue;

                stopwatch.Stop();

                var vectorLayer = GeneralHelper.GetAsLayer($"S-{estimatedZoomLevel}-{method}", new() { simplified });

                //this.Presenter.AddLayer(vectorLayer);

                var diff = await GeneralHelper.CreateImages(boundingBox, estimatedZoomLevel, originalBitmap, vectorLayer, outputDirectory, fileName, $"{method}"/*, coef*/, saveImages: false);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, simplified, estimatedZoomLevel, methodNames[method], stopwatch.ElapsedMilliseconds, 0, parameters).ToTsv());

                //var shapes = simplified.Select(s => s.AsSqlGeometry().AsEsriShape());
                //Shapefile.Save($"{outputDirectory}\\{fileName}-{level}.shp", shapes, true, true, DefaultMapProjections.WebMercator);
            }
        }
        //}
    }

    private static async Task CompareSingleFeatureMultiScale(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.TotalNumberOfPoints > 15 && g.TotalNumberOfPoints < 500)
                                .OrderBy(g => Guid.NewGuid())
                                .Take(100)
                                .ToList();

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                //SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                //SimplificationType.APSC
            };

        var coefs = new List<int>() { 0, 2/*, 4, 6, 8, 10 */};

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var baseZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 512, 512);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", new() { feature });

            GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

            originalFeatureSet.Save($"{outputDirectory}\\{fileName}-F{featureIndex}.json", false, true);

            foreach (var coef in coefs)
            {
                if (baseZoomLevel - coef < 4)
                    continue;

                var estimatedZoomLevel = baseZoomLevel - coef;

                var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

                var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

                var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

                if (currentScreenSize.Width <= 0 || currentScreenSize.Height <= 0)
                    continue;

                if (currentScreenSize.Width > 10000 || currentScreenSize.Height > 10000)
                    continue;

                var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

                originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-F{featureIndex}-original.png", System.Drawing.Imaging.ImageFormat.Tiff);

                var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

                var threshold = webMercatorResolution   /** coef*/;

                var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

                var accuracies = new List<SimplificationAccuracy>();

                foreach (var lrModel in lrModels)
                {
                    if (coef != 0)
                        continue;

                    var result = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.CompareBySingleFeature(
                                    feature,
                                    g => g.Simplify(lrModel, toScreenMap3, true),
                                    boundingBox,
                                    currentScreenSize,
                                    originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{estimatedZoomLevel}-C{coef}",
                                    lrModel.Title,
                                    estimatedZoomLevel);

                    accuracies.Add(result.Item1);

                    writer.WriteLine(new VisualSimplificationLog(
                                            fileName,
                                            boundingBox,
                                            featureIndex,
                                            feature,
                                            result.simplified,
                                            estimatedZoomLevel,
                                            lrModel.Title,
                                            stopwatch.ElapsedMilliseconds,
                                            coef,
                                            parameters,
                                            result.Item1.TlvdPerLength,
                                            result.Item1.ConfusionMatrix).ToTsv());
                }

                foreach (var method in methods)
                {
                    var result = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.CompareBySingleFeature(
                                   feature,
                                   g => g.Simplify(method, parameters),
                                   boundingBox,
                                   currentScreenSize,
                                   originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{estimatedZoomLevel}-C{coef}",
                                   methodNames[method],
                                   estimatedZoomLevel);

                    accuracies.Add(result.Item1);

                    writer.WriteLine(new VisualSimplificationLog(
                                            fileName,
                                            boundingBox,
                                            featureIndex,
                                            feature,
                                            result.simplified,
                                            estimatedZoomLevel,
                                            methodNames[method],
                                            stopwatch.ElapsedMilliseconds,
                                            coef,
                                            parameters,
                                            result.Item1.TlvdPerLength,
                                            result.Item1.ConfusionMatrix).ToTsv());

                }


                StringBuilder builder = new StringBuilder();

                builder.AppendLine(SimplificationAccuracy.GetTsvHeader());

                foreach (var accuracy in accuracies)
                {
                    builder.AppendLine(accuracy.ToTsv());
                }

                File.WriteAllText($"{outputDirectory}\\{fileName}-F{featureIndex}-Z{estimatedZoomLevel}-C{coef}-log.txt", builder.ToString());

            }
        }
    }

    private static async Task CompareSingleFeatureVaryingThreshold(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.TotalNumberOfPoints > 30 && g.TotalNumberOfPoints < 2000)
                                .OrderBy(g => Guid.NewGuid())
                                .Take(50)
                                .ToList();


        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        var temp = features.Where(f => f.Type == GeometryType.MultiLineString ||
                            f.Type == GeometryType.MultiPolygon ||
                            f.Type == GeometryType.GeometryCollection)
            .ToList();

        if (temp.Any() == true)
        {

        }

        if (features.Any(f => f.Type != GeometryType.LineString && f.Type != GeometryType.Polygon || f.Geometries?.Count > 1))
        {

        }

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                //SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,

                //SimplificationType.APSC
            };


        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        var coefs = new double[] { 2, 1, 1 / 2.0 };

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 128, 128);

            var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

            var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

            originalBitmap.Save($"{outputDirectory}\\{fileName}-original-F{featureIndex}-Z{zoomLevel}.png", System.Drawing.Imaging.ImageFormat.Tiff);

            var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var threshold = webMercatorResolution /** coef*/;

            var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

            GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

            originalFeatureSet.Save($"{outputDirectory}\\{fileName}-F{featureIndex}.json", false, true);

            foreach (var coef in coefs)
            {
                var accuracies = new List<SimplificationAccuracy>();

                foreach (var lrModel in lrModels)
                {
                    var result = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.CompareBySingleFeature(
                                    feature,
                                    g => g.Simplify(lrModel, toScreenMap3, true),
                                    boundingBox,
                                    currentScreenSize,
                                    originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{1}",
                                    lrModel.Title,
                                    zoomLevel);

                    accuracies.Add(result.Item1);

                    writer.WriteLine(new VisualSimplificationLog(
                                            fileName,
                                            boundingBox,
                                            featureIndex,
                                            feature,
                                            result.simplified,
                                            zoomLevel,
                                            lrModel.Title,
                                            stopwatch.ElapsedMilliseconds,
                                            1,
                                            parameters,
                                            result.Item1.TlvdPerLength,
                                            result.Item1.ConfusionMatrix).ToTsv());
                }


                foreach (var method in methods)
                {
                    threshold = webMercatorResolution * coef;

                    parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

                    var result = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.CompareBySingleFeature(
                                   feature,
                                   g => g.Simplify(method, parameters),
                                   boundingBox,
                                   currentScreenSize,
                                   originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{coef}",
                                   methodNames[method],
                                   zoomLevel);

                    accuracies.Add(result.Item1);

                    writer.WriteLine(new VisualSimplificationLog(
                                            fileName,
                                            boundingBox,
                                            featureIndex,
                                            feature,
                                            result.simplified,
                                            zoomLevel,
                                            methodNames[method],
                                            stopwatch.ElapsedMilliseconds,
                                            coef,
                                            parameters,
                                            result.Item1.TlvdPerLength,
                                            result.Item1.ConfusionMatrix).ToTsv());

                }

                StringBuilder builder = new StringBuilder();

                builder.AppendLine(SimplificationAccuracy.GetTsvHeader());

                foreach (var accuracy in accuracies)
                {
                    builder.AppendLine(accuracy.ToTsv());
                }

                File.WriteAllText($"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{coef}-log.txt", builder.ToString());
            }
        }
    }


    // for comparing with other algorithms
    // layer based comparison that simplify at the scale of the layer
    // instead of every single feature, it uses a range of zoom levels
    private static async Task CompareLayersVaryingThreshold(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.Type != GeometryType.GeometryCollection)
                                .Where(g => g.TotalNumberOfPoints > 15 && g.TotalNumberOfPoints < 3000)
                                .ToList();

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                //SimplificationType.VisvalingamWhyatt,
                //SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,
                //SimplificationType.APSC
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        var boundingBox = features.GetBoundingBox();

        var coefs = new double[] { 2, 1, 1.0 / 2.0 };

        var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 1024, 1024);

        var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

        var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

        var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

        var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

        originalBitmap.Save($"{outputDirectory}\\{fileName}-{zoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

        var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

        var threshold = webMercatorResolution /** coef*/;

        var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

        foreach (var lrModel in lrModels)
        {
            var result = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.CompareByLayer(
                     features,
                     g => g.Simplify(lrModel, toScreenMap3, true),
                     boundingBox,
                     currentScreenSize,
                     originalBitmap,
                     $"{outputDirectory}\\{fileName}-{zoomLevel}",
                     lrModel.Title,
                     zoomLevel);

            writer.WriteLine(new VisualSimplificationLog(
                                    fileName,
                                    boundingBox,
                                    features,
                                    result.simplifieds,
                                    zoomLevel,
                                    lrModel.Title,
                                    stopwatch.ElapsedMilliseconds,
                                    1,
                                    parameters,
                                    result.Item1.TlvdPerLength,
                                    result.Item1.ConfusionMatrix).ToTsv());
        }

        foreach (var method in methods)
        {
            foreach (var coef in coefs)
            {
                threshold = webMercatorResolution * coef;

                parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

                var result = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.CompareByLayer(
                         features,
                         g => g.Simplify(method, parameters),
                         boundingBox,
                         currentScreenSize,
                         originalBitmap,
                         $"{outputDirectory}\\{fileName}-{zoomLevel}-{coef}",
                         methodNames[method],
                         zoomLevel);

                writer.WriteLine(new VisualSimplificationLog(
                                        fileName,
                                        boundingBox,
                                        features,
                                        result.simplifieds,
                                        zoomLevel,
                                        methodNames[method],
                                        stopwatch.ElapsedMilliseconds,
                                        coef,
                                        parameters,
                                        result.Item1.TlvdPerLength,
                                        result.Item1.ConfusionMatrix).ToTsv());
            }
        }
    }


    #region FindSpecial Cases

    // for finding special cases
    private static async Task FindSpecialCase(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************

        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.TotalNumberOfPoints > 15 && g.TotalNumberOfPoints < 3000)
                                //.OrderBy(g => Guid.NewGuid())
                                //.Take(100)
                                .ToList();

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        int featureIndex = 0;

        var diffList = new List<SpecialFeatureInfo>();

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 256, 256);

            var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", new() { feature });

            var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

            var toScreenMap3 = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var threshold = webMercatorResolution   /** coef*/;

            var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

            var length_Original = feature.CalculateEuclideanLength();

            var ranks = new List<(string type, double value)>();

            if (length_Original == 0)
                continue;

            foreach (var lrModel in lrModels)
            {
                if (lrModel.Title.Contains("LF"))
                    continue;

                var simplified = feature.Simplify(lrModel, toScreenMap3, true);

                if (simplified.Type == GeometryType.Point)
                    continue;

                var tlvdPerLength = feature.CalculateTotalVectorDisplacement(simplified);

                ranks.Add((lrModel.Title, tlvdPerLength));
            }

            foreach (var method in methods)
            {
                var simplified = feature.Simplify(method, parameters);

                if (simplified.Type == GeometryType.Point)
                    continue;

                var tlvdPerLength = feature.CalculateTotalVectorDisplacement(simplified);

                ranks.Add((method.ToString(), tlvdPerLength));
            }

            var lrMinValue = ranks.Where(r => r.type.StartsWith("LR")).Min(r => r.value);
            var otherMaxValue = ranks.Where(r => !r.type.StartsWith("LR")).Max(r => r.value);

            var orderedRanks = ranks.OrderBy(r => r.value).ToList();
            int theRank = 0;
            if (orderedRanks.First().type.StartsWith("LR"))
            {
                theRank++;

                diffList.Add(new SpecialFeatureInfo()
                {
                    Rank = theRank,
                    diff = otherMaxValue - lrMinValue,
                    FeatureIndex = featureIndex - 1,
                    OriginalGeometry = feature,
                    Zoomlevel = estimatedZoomLevel
                });
            }
        }

        await ProcessSpecialCase(shpFile, writer, outputDirectory, lrModels, diffList.Where(d => d.diff > 1).OrderByDescending(l => l.diff).Take(15).ToList());
    }

    private static async Task ProcessSpecialCase(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, List<SpecialFeatureInfo> diffList)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        int featureIndex = 0;

        foreach (var item in diffList)
        {
            var feature = item.OriginalGeometry;

            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 256, 256);

            var comparison = await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.Compare(item.OriginalGeometry, featureIndex, outputDirectory, estimatedZoomLevel, fileName, methods, lrModels);

            foreach (var accuracy in comparison)
            {
                writer.WriteLine(accuracy.ToTsv());
            }
        }

    }

    #endregion

    private static List<LogisticSimplification<Point>> GetLRAModels()
    {
        Dictionary<string, string> names = new Dictionary<string, string>
        {
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR4.json", "LR4" },
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR7.json", "LR7" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR7v3.json", "LR7v3" },
                                                    
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR4v11.json", "LR4v11" },
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR7v11.json", "LR7v11" },
                                                       
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v12.json", "LR4v12" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR7v12.json", "LR7v12" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v12-LF.json", "LR4v12-LF" },
              
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR4v15.json", "LR4v15" },
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR7v15.json", "LR7v15" },
                                                        
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR4v18.json", "LR4v18" },
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR7v18.json", "LR7v18" },
                     
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR4v20.json", "LR4v20" },
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR7v20.json", "LR7v20" },
            ////{ @"E:\University.Ph.D\4. Paper2\Model\LR4v20-LF.json", "LR4v20-LF" },

            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v21.json", "LR4v21" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR7v21.json", "LR7v21" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v21-LF.json", "LR4v21-LF" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR5v21.json", "LR5v21" },
             
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v22.json", "LR4v22" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR7v22.json", "LR7v22" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v22-LF.json", "LR4v22-LF" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR5v22.json", "LR5v22" },

            { @"E:\University.Ph.D\4. Paper2\Model\LR4v23.json", "LR4v23" },
            { @"E:\University.Ph.D\4. Paper2\Model\LR7v23.json", "LR7v23" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR4v23-LF.json", "LR4v23-LF" },
            //{ @"E:\University.Ph.D\4. Paper2\Model\LR5v23.json", "LR5v23" },
        };

        var result = new List<LogisticSimplification<Point>>();

        foreach (var item in names)
        {
            var trainingData = LRSimplificationTrainingData<Point>.LoadFromJson(item.Key);

            if (trainingData is null)
            {
                throw new NotImplementedException();
            }

            var model = LogisticSimplification<Point>.Create(trainingData, item.Key);

            model.Title = item.Value;

            result.Add(model);
        }

        return result;
    }


    public async static Task GeneralTest()
    {
        // OSM dataset directory should be addressed here
        var dataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";
        //var dataFolder = "E:\\University.Ph.D\\Sample Data\\OSM-1403-06-25-WebMercatorClipped";
        //var dataFolder = @"E:\University.Ph.D\4. Paper2\DataWm";
        var files = Directory.EnumerateFiles(dataFolder, "*.shp", SearchOption.AllDirectories);

        // Output directory should be addressed here
        var writeFolder = @"E:\University.Ph.D\4. Paper2\AlgorithmRun\1403-07-17";

        if (!Directory.Exists(writeFolder))
            _ = Directory.CreateDirectory(writeFolder);

        StreamWriter writer = new StreamWriter($"{writeFolder}\\All-summary.txt", false);

        writer.WriteLine(VisualSimplificationLog.GetHeader());
        //writer.WriteLine(SimplificationAccuracy.GetTsvHeader());

        List<string> fileNames = new List<string>();

        var oldfiles = Directory.EnumerateFiles(writeFolder, "*.txt", SearchOption.AllDirectories)
                                .Select(s => Path.GetFileNameWithoutExtension(s).Replace("-summary", string.Empty));

        string logFile = $"{writeFolder}\\Log-{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.txt";

        Stopwatch watch = Stopwatch.StartNew();

        var includeHeader = true;

        var lrModels = GetLRAModels();

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (fileNames.Contains(fileName))
                continue;

            if (oldfiles.Contains(fileName))
                continue;

            fileNames.Add(fileName);

            watch.Restart();

            //await CompareLayersPyramidByFixScreen(file, writer, writeFolder, lrModels);
            //await CompareLayersPyramidByFixScreen(file, writer, writeFolder, lrModels);
            //await CompareLayersVaryingThreshold(file, writer, writeFolder, lrModels);
            await CompareSingleFeatureVaryingThreshold(file, writer, writeFolder, lrModels);

            //await FindSpecialCase(file, writer, writeFolder, lrModels);


            File.AppendAllLines(logFile, new List<string>() { $"Finished At: {DateTime.Now.ToLongTimeString()}; Ellapsed: {watch.ElapsedMilliseconds / 1000.0:N0000} (s) - ({fileName})" });
        }

        writer.Close();
        writer.Dispose();
    }


    #region GeoJson

    public async static Task ProcessGeoJson()
    {
        var dataFolder = @"E:\University.Ph.D\4. Paper2\DataWm\samples";

        var files = Directory.EnumerateFiles(dataFolder, "*.json", SearchOption.AllDirectories);

        var lrModels = GetLRAModels();

        foreach (var file in files)
        {
            await ProcessGeoJson(file, dataFolder, lrModels);
        }
    }

    private async static Task ProcessGeoJson(string jsonFile, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(jsonFile)}";

        //******************************************************
        //***************** read features **********************
        var json = GeoJson.ReadFeatures(jsonFile);

        var features = json.Select(f => f.Geometry.TransformToWeMercator()).ToList();

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,

                SimplificationType.SleeveFitting,

                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ 256, 256);

            await IRI.Maptor.Jab.Common.Helpers.SimplificationHelper.Compare(feature, featureIndex, outputDirectory, estimatedZoomLevel, fileName, methods, lrModels);
        }
    }

    #endregion


}

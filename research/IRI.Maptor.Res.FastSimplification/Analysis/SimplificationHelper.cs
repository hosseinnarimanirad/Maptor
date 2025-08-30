using System.IO;
using System.Diagnostics;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;


namespace IRI.Maptor.Res.FastSimplification;

public static class SimplificationHelper
{
    private static bool retain3Points = false;

    private static List<SimplificationType> methods = new List<SimplificationType>()
        {
            SimplificationType.RamerDouglasPeucker,

            SimplificationType.VisvalingamWhyatt,

            SimplificationType.NormalOpeningWindow,
            //SimplificationType.BeforeOpeningWindow,

            SimplificationType.CumulativeTriangleRoutine,
            //SimplificationType.TriangleRoutine
        };

    public async static Task GeneralTest()
    {
        // OSM dataset directory should be addressed here
        var dataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";
        //var dataFolder = @"E:\University.Ph.D\Sample Data\OSM-1404-02-29-WebMercator-Lake";

        var files = Directory.EnumerateFiles(dataFolder, "*.shp", SearchOption.AllDirectories);

        // Output directory should be addressed here
        var writeFolder = $@"E:\University.Ph.D\5. ISPRS Conf Paper\AlgorithmRun\{DateTime.Now.Date:yyyy-MM-dd}";

        if (!Directory.Exists(writeFolder))
            _ = Directory.CreateDirectory(writeFolder);

        StreamWriter writer = new StreamWriter($"{writeFolder}\\summary.txt", false);
        writer.WriteLine(SpeedLog.GetHeader());

        List<string> fileNames = ["AdminArea_WM", "Building_WM", "Landuse_WM", "Railway_WM"];

        var oldfiles = Directory.EnumerateFiles(writeFolder, "*.txt", SearchOption.AllDirectories)
                                .Select(s => System.IO.Path.GetFileNameWithoutExtension(s).Replace("-summary", string.Empty));

        string logFile = $"{writeFolder}\\Log-Visual-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt";

        Stopwatch watch = Stopwatch.StartNew();

        foreach (var file in files)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(file);

            if (fileNames.Contains(fileName) || oldfiles.Contains(fileName))
                continue;

            fileNames.Add(fileName);

            ProcessVisualQuality(file, writer, writeFolder);

            File.AppendAllLines(logFile, new List<string>() { $"Finished At: {DateTime.Now.ToLongTimeString()}; Ellapsed: {watch.ElapsedMilliseconds / 1000.0:N0000} (s) - ({fileName})" });
        }

        writer.Close();
        writer.Dispose();
    }

    private static void ProcessSpeed(string shpFile, StreamWriter writer, string outputDirectory)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        //******************************************************
        //***************** read features **********************
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => g.Split(false))
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.TotalNumberOfPoints > 10)
                                .ToList();

        foreach (var feature in features)
            feature.RemoveConsecutiveDuplicatePoints();

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        var groundBoundingBox = features.GetBoundingBox();

        // screen size: 4096x4096
        var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(groundBoundingBox, /*34,*/ 8000, 8000);

        // screen size      scale
        // 8k           =>  1:1,155,581
        // 4096         =>  1:2,300,000
        var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, groundBoundingBox);

        // threshold values is set to 0.5 pixel
        var threshold = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 0.5);

        var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

        //var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);
        //var originalBitmap = await originalVectorLayer.ParseToBitmapImage(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);
        //originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Tiff);


        Stopwatch stopwatch = Stopwatch.StartNew();

        List<int> featureCount = [1000, 10_000, 50_000, 100_000, 200_000, 300_000];

        foreach (var method in methods)
        {
            foreach (var count in featureCount)
            {
                if (count > features.Count)
                    break;

                var theFeatures = features.Take(count).ToList();

                stopwatch.Restart();

                var simplifiedFeatures = theFeatures.Simplify(method, parameters, reduceToPoint: false);

                stopwatch.Stop();

                if (simplifiedFeatures.IsNullOrEmpty())
                    continue;

                writer.WriteLine(
                  new SpeedLog(fileName,
                                methodNames[method],
                                theFeatures,
                                simplifiedFeatures,
                                stopwatch.ElapsedMilliseconds,
                                parameters).ToTsv());

                //var vectorLayer = GeneralHelper.GetAsLayer("original", [theFeatures]);
                //var simplifiedBitmap = await vectorLayer.ParseToBitmapImage(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);
                //simplifiedBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-{method}-[{count}].png", System.Drawing.Imaging.ImageFormat.Tiff);
            }

            writer.Flush();
        }

    }

    //private static async Task ProcessVisualQuality(string shpFile, StreamWriter writer, string outputDirectory)
    //{
    //    var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

    //    //******************************************************
    //    //***************** read features **********************
    //    var features = Shapefile.ReadShapes(shpFile)
    //                            .Select(g => g.AsGeometry())
    //                            .SelectMany(g => g.Split(false))
    //                            .Where(g => !g.IsNullOrEmpty())
    //                            .Where(g => g.TotalNumberOfPoints > 30)
    //                            .ToList();

    //    foreach (var feature in features)
    //        feature.RemoveConsecutiveDuplicatePoints();

    //    features = features.Where(f => !f.HasDuplicatePoints()).ToList();

    //    List<SimplificationType> methods = new List<SimplificationType>()
    //    {
    //        SimplificationType.RamerDouglasPeucker,

    //        SimplificationType.VisvalingamWhyatt,

    //        SimplificationType.NormalOpeningWindow,
    //        SimplificationType.BeforeOpeningWindow,

    //        SimplificationType.CumulativeTriangleRoutine,
    //    };

    //    Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

    //    var groundBoundingBox = features.GetBoundingBox();

    //    var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(groundBoundingBox, /*34,*/ 4096, 4096);

    //    var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

    //    var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, groundBoundingBox);

    //    // threshold values is set to 0.5 pixel
    //    var threshold = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 0.5);

    //    var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

    //    var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

    //    var originalBitmap = await originalVectorLayer.ParseToBitmapImage(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

    //    originalBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Tiff);


    //    foreach (var feature in features)
    //    {
    //        foreach (var method in methods)
    //        {
    //            var simplifiedFeatures = feature.Simplify(method, parameters);

    //            // writer.WriteLine(
    //            //new SpeedLog(fileName,
    //            //              methodNames[method],
    //            //              theFeatures,
    //            //              simplifiedFeatures,
    //            //              stopwatch.ElapsedMilliseconds,
    //            //              parameters).ToTsv());

    //            //var vectorLayer = GeneralHelper.GetAsLayer("original", [theFeatures]);

    //            //var simplifiedBitmap = await vectorLayer.ParseToBitmapImage(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

    //            //simplifiedBitmap.Save($"{outputDirectory}\\{fileName}-{estimatedZoomLevel}-{method}-[{count}].png", System.Drawing.Imaging.ImageFormat.Tiff);
    //        }

    //        writer.Flush();
    //    }


    //}


    #region Test with visual output

    public static void ProcessVisualQuality(string shpFile, StreamWriter writer, string outputDirectory)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(shpFile)}";

        List<int> renderSizes = [64, 128, 256];

        //******************************************************
        //***************** read features **********************
        var shapes = Shapefile.ReadShapes(shpFile);

        var features = shapes
                        .Select(g => g.AsGeometry())
                        .SelectMany(g => g.NumberOfGeometries > 1 ? g.Split(false) : [g])
                        .Where(g => !g.IsNullOrEmpty())
                        .Where(g => g.TotalNumberOfPoints > 40 && g.TotalNumberOfPoints < 500)
                        .ToList();

        //var isRingbase = shapes[0].IsRingBase();

        if (features.IsNullOrEmpty())
        {
            return;
        }

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        var colors = new Dictionary<SimplificationType, System.Windows.Media.Color>()
        {
            {SimplificationType.RamerDouglasPeucker, IRI.Maptor.Jab.Common.Helpers.ColorHelper.ToWpfColor("#6B007B")}, //light blue
            {SimplificationType.CumulativeTriangleRoutine, IRI.Maptor.Jab.Common.Helpers.ColorHelper.ToWpfColor("#12239E")}, //light blue
            {SimplificationType.VisvalingamWhyatt, IRI.Maptor.Jab.Common.Helpers.ColorHelper.ToWpfColor("#E645AB")}, //light blue
            {SimplificationType.NormalOpeningWindow, IRI.Maptor.Jab.Common.Helpers.ColorHelper.ToWpfColor("#E66C37")}, //light blue
            {SimplificationType.BeforeOpeningWindow, IRI.Maptor.Jab.Common.Helpers.ColorHelper.ToWpfColor("#118DFF")}, //light blue
            {SimplificationType.TriangleRoutine, System.Windows.Media.Colors.Green}, //light blue
        };


        var redColor = ColorHelper.ToWpfColor("#DE36A1");
        var grayColor = ColorHelper.ToWpfColor("#ADADAD");
        var greenColor = ColorHelper.ToWpfColor("#08686E");

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            //if (!CheckZoom2(featureIndex, fileName)) continue;

            var outputDirectoryForFeature = $"{outputDirectory}\\{fileName}\\{featureIndex}";

            if (!Directory.Exists(outputDirectoryForFeature))
                Directory.CreateDirectory(outputDirectoryForFeature);

            var boundingBox = feature.GetBoundingBox().Expand(1.1);

            if (boundingBox.Width == 0 && boundingBox.Height == 0)
            {

            }

            foreach (var renderSize in renderSizes)
            {
                var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ renderSize, renderSize);

                if (estimatedZoomLevel == 0)
                {

                }

                var threshold = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 4);

                var screenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

                if (screenSize.Width * screenSize.Height <= 0)
                    continue;

                var scale = Math.Floor((1.0 / WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel)) / 1000.0) * 1000;

                var originalFrame = feature.AsDrawingVisual(VisualParameters.Get(Colors.Transparent, redColor /*ColorHelper.ToWpfColor("#C0C0C0")*/, 2, 1), screenSize.Width, screenSize.Height, boundingBox);

                GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

                //originalFeatureSet.Save($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-original.json", false, true);

                var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

                List<DrawingVisual> drawingVisuals = [originalFrame!];

                foreach (var method in methods)
                {
                    var simplified = feature.Simplify(method, parameters);

                    if (simplified.IsNullOrEmpty())
                        continue;

                    if (simplified.Type == GeometryType.Point)
                        continue;

                    var simplifiedFrame = simplified.AsDrawingVisual(VisualParameters.Get(Colors.Transparent, greenColor /*colros[method]*/, 2, 1), screenSize.Width, screenSize.Height, boundingBox);

                    drawingVisuals.Add(simplifiedFrame!);

                    GeoJsonFeatureSet featureSet = simplified.AsGeoJsonFeatureSet();

                    var compression = Math.Round(feature.Compression(simplified) * 100);

                    var fullFileName = $"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{renderSize}-{method.GetDescription()}-{scale:N0}-{compression}%.png";

                    ImageUtility.MergeAndSave(fullFileName, [originalFrame!, simplifiedFrame!], screenSize.Width, screenSize.Height);

                    //featureSet.Save($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{renderSize}-{method}.json", false, true);
                }

                //Save($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{coef}.png", drawingVisuals, estimatedZoomLevel, boundingBox);

                //IRI.Maptor.Jab.Common.Helpers.ImageUtility.MergeAndSave($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{renderSize}-{scale:N}.png", drawingVisuals, screenSize.Width, screenSize.Height);
            }
        }
    }



    #endregion



    //// برای یک فایل مشخص به صورت خودکار
    //// زوم‌های مناسب فایل رو پیدا کرده و 
    //// برای هر زوم بر اساس حد آستانه‌های از پیش تعریف شده
    //// الگوریتم‌ها رو اجرا و اختلاف عکسی اون‌ها رو ذخیره می‌کنه
    //// نهایتا لاگ مقادیر محاسبه شده رو هم ذخیره می کنه
    //public static async Task TestThresholdChangesAlgo(string shpFile)
    //{
    //    var outputDirectoryBase = @"E:\University.Ph.D\Paper1_ReviewPaper\AlgorithmRun\1401-03-13";
    //    var fileName = $"{System.IO.Path.GetFileNameWithoutExtension(shpFile)}";
    //    var outputDirectory = $"{outputDirectoryBase}\\{fileName}";
    //    if (!Directory.Exists(outputDirectory))
    //        Directory.CreateDirectory(outputDirectory);

    //    var groundResolutionCoefs = new List<double>()
    //        {
    //            //1.0 / 6,
    //            //1.0 / 5,
    //            1.0 / 4,
    //            1.0 / 3,
    //            1.0 / 2,
    //            1,
    //            2,
    //            3,
    //            4,
    //            //5,
    //            //6
    //        };

    //    var groundResolutionCoefs_Range = groundResolutionCoefs.Max() - groundResolutionCoefs.Min();
    //    var groundResolutionCoefs_Min = groundResolutionCoefs.Min();

    //    foreach (var coef in groundResolutionCoefs)
    //    {
    //        if (!Directory.Exists($"{outputDirectory}\\{coef:N2}"))
    //            Directory.CreateDirectory($"{outputDirectory}\\{coef:N2}");
    //    }

    //    //******************************************************
    //    //***************** read features **********************
    //    var features = Shapefile.ReadShapes(shpFile)
    //                            .Select(g => g.AsGeometry())
    //                            .Where(g => !g.IsNullOrEmpty())
    //                            .ToList();

    //    foreach (var feature in features)
    //        feature.RemoveConsecutiveDuplicatePoints();

    //    var totalNumberOfPoints = features.Sum(f => f.TotalNumberOfPoints);

    //    var sqlFeatures = features.Select(f => f.AsSqlGeometry()).ToList();

    //    var groundBoundingBox = sqlFeatures.GetBoundingBox();

    //    var estimatedScale = WebMercatorUtility.EstimateZoomLevel(groundBoundingBox, /*34,*/ 900, 900);
    //    var screenSize = WebMercatorUtility.ToScreenSize(estimatedScale, groundBoundingBox);

    //    StringBuilder builder = new StringBuilder();
    //    builder.AppendLine(LogStructure.GetHeader());
    //    var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);
    //    builder.AppendLine(new LogStructure(fileName, totalNumberOfPoints, features, 0, 0, "original", 0, new SimplificationParamters(), 1).ToTsv());

    //    var startIndex = Math.Max(3, estimatedScale - 5);
    //    var count = Math.Max(estimatedScale - startIndex + 2, 3);
    //    var groundLevels = Enumerable.Range(startIndex, count).ToList();

    //    List<SimplificationType> methods = new List<SimplificationType>()
    //        {
    //            SimplificationType.TriangleRoutine,
    //            SimplificationType.Angle,
    //            SimplificationType.CumulativeAngle,
    //            SimplificationType.EuclideanDistance,
    //            SimplificationType.CumulativeEuclideanDistance,
    //            SimplificationType.CumulativeAreaAngle,
    //            SimplificationType.NthPoint,
    //            SimplificationType.CumulativeTriangleRoutine,
    //            SimplificationType.VisvalingamWhyatt,
    //            SimplificationType.RamerDouglasPeucker,
    //            SimplificationType.ReumannWitkam,
    //            SimplificationType.Lang,
    //            SimplificationType.PerpendicularDistance,
    //            SimplificationType.NormalOpeningWindow,
    //            SimplificationType.BeforeOpeningWindow,
    //        };

    //    Stopwatch stopwatch = Stopwatch.StartNew();

    //    foreach (var level in groundLevels)
    //    {
    //        var currentScreenSize = WebMercatorUtility.ToScreenSize(level, groundBoundingBox);
    //        var scale = WebMercatorUtility.GetGoogleMapScale(level);

    //        var originalBitmap = await originalVectorLayer.ParseToBitmapImage(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

    //        originalBitmap.Save($"{outputDirectory}\\{fileName}-{level}-original.png", System.Drawing.Imaging.ImageFormat.Tiff);

    //        var groundResolution = WebMercatorUtility.CalculateGroundResolution(level, 30); // mean latitude: 30

    //        var webMercatorResolution = Math.Max(groundBoundingBox.Width / currentScreenSize.Width, groundBoundingBox.Height / currentScreenSize.Height);

    //        foreach (var coef in groundResolutionCoefs)
    //        {
    //            foreach (var method in methods)
    //            {
    //                //if (method != SimplificationType.DouglasPeucker)
    //                //    continue;

    //                var threshold = webMercatorResolution * coef;

    //                var n = Math.Max(2, (uint)(features.Sum(f => f.TotalNumberOfPoints) / features.Count() * (2 + 8 * (coef - groundResolutionCoefs_Min) / groundResolutionCoefs_Range)));

    //                var parameters = new SimplificationParamters()
    //                {
    //                    N = n,
    //                    AreaThreshold = threshold * threshold,
    //                    DistanceThreshold = threshold,
    //                    Retain3Points = retain3Points,
    //                    LookAhead = (int)n,
    //                    AngleThreshold = 0.98
    //                };

    //                stopwatch.Restart();

    //                var simplified = features.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

    //                stopwatch.Stop();

    //                var vectorLayer = GeneralHelper.GetAsLayer($"S-{level}-{method}", simplified);

    //                var tempDirectory = $"{outputDirectory}\\{coef:N2}";

    //                var diff = await GeneralHelper.CreateImages(groundBoundingBox, level, originalBitmap, vectorLayer, tempDirectory, fileName, $"{method}"/*, coef*/);

    //                builder.AppendLine(new LogStructure(fileName, totalNumberOfPoints, simplified, level, diff.percent, $"{method.GetDescription()}", stopwatch.ElapsedMilliseconds, parameters, coef).ToTsv());

    //                diff.image.Dispose();
    //            }
    //        }

    //        originalBitmap.Dispose();
    //    }

    //    File.WriteAllText($"{outputDirectory}\\{fileName}-summary.txt", builder.ToString());
    //}

}

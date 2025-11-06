using System.IO;
using System.Diagnostics;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;


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
        writer.WriteLine(Log.GetHeader());

        List<string> fileNames = ["AdminArea_WM", "Building_WM", "Landuse_WM", "Railway_WM", "Road_WM"];

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
         
        if (features.IsNullOrEmpty())
            return;

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
        }

        features = features.Where(f => !f.HasDuplicatePoints()).ToList();

        var colors = new Dictionary<SimplificationType, System.Windows.Media.Color>()
        {
            {SimplificationType.RamerDouglasPeucker, ColorHelper.ToWpfColor("#6B007B")}, //light blue
            {SimplificationType.CumulativeTriangleRoutine, ColorHelper.ToWpfColor("#12239E")}, //light blue
            {SimplificationType.VisvalingamWhyatt, ColorHelper.ToWpfColor("#E645AB")}, //light blue
            {SimplificationType.NormalOpeningWindow, ColorHelper.ToWpfColor("#E66C37")}, //light blue
            {SimplificationType.BeforeOpeningWindow, ColorHelper.ToWpfColor("#118DFF")}, //light blue
            {SimplificationType.TriangleRoutine, System.Windows.Media.Colors.Green}, //light blue
        };


        var redColor = ColorHelper.ToWpfColor("#DE36A1");
        var grayColor = ColorHelper.ToWpfColor("#ADADAD");
        var greenColor = ColorHelper.ToWpfColor("#08686E");

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;
             
            var outputDirectoryForFeature = $"{outputDirectory}\\{fileName}\\{featureIndex}";

            if (!Directory.Exists(outputDirectoryForFeature))
                Directory.CreateDirectory(outputDirectoryForFeature);

            var boundingBox = feature.GetBoundingBox().Expand(1.1);
             
            foreach (var renderSize in renderSizes)
            {
                var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, /*34,*/ renderSize, renderSize);
                 
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

                    var tlvd = feature.CalculateTotalVectorDisplacementPerLength(simplified);

                    var fullFileName = $"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{renderSize}-{method.GetDescription()}-{scale:N0}-{compression}%-{tlvd:#.00}.png";

                    ImageUtility.MergeAndSave(fullFileName, [originalFrame!, simplifiedFrame!], screenSize.Width, screenSize.Height);

                    //featureSet.Save($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{renderSize}-{method}.json", false, true);
                }
                 
                //IRI.Maptor.Jab.Common.Helpers.ImageUtility.MergeAndSave($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{renderSize}-{scale:N}.png", drawingVisuals, screenSize.Width, screenSize.Height);
            }
        }
    }

}

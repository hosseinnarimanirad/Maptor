using System.IO;
using System.Diagnostics;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;


namespace IRI.Maptor.Res.FastSimplification;

/// <summary>
/// The visual experiment of the ISPRS 2025 paper (Figures 5–8): every feature with 40–500 points is
/// rendered at the zoom levels where its bounding box (expanded by 10 %) fits 64², 128² and 256² screens,
/// and simplified by RDP, VW, NOPW and CTR with a threshold of 4 pixels at that level (squared for the
/// area-based VW and CTR). One PNG per method overlays the original (red) and the simplified (green)
/// geometry; the file name records the scale, the compression and the TLVD per length.
/// <para>
/// CTR is <see cref="SimplificationType.CumulativeTriangleRoutine"/>, i.e. <c>Simplifications.SimplifyByCumulativeTriangleRoutine</c>.
/// The published run is <c>5. ISPRS Conf Paper\AlgorithmRun\2025-05-25-fix colors</c>; the execution-time
/// figures of the paper (Figures 9 and 10) were not produced by this project.
/// </para>
/// </summary>
public static class SimplificationHelper
{
    private const bool Retain3Points = false;

    /// <summary>Screen pixels of the threshold: the smallest visually distinguishable change assumed by the paper.</summary>
    private const double ThresholdInPixels = 4;

    private static readonly List<SimplificationType> Methods = new()
    {
        SimplificationType.RamerDouglasPeucker,
        SimplificationType.VisvalingamWhyatt,
        SimplificationType.NormalOpeningWindow,
        SimplificationType.CumulativeTriangleRoutine,
    };

    private static readonly List<int> RenderSizes = new() { 64, 128, 256 };

    public async static Task GeneralTest()
    {
        // OSM dataset directory should be addressed here
        var dataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";

        var files = Directory.EnumerateFiles(dataFolder, "*.shp", SearchOption.AllDirectories);

        // Output directory should be addressed here; every run gets a dated folder
        var writeFolder = $@"E:\University.Ph.D\5. ISPRS Conf Paper\AlgorithmRun\{DateTime.Now.Date:yyyy-MM-dd}";

        Directory.CreateDirectory(writeFolder);

        // layers that already have an output folder are skipped, so an interrupted run can be resumed
        var finishedLayers = Directory.EnumerateDirectories(writeFolder).Select(Path.GetFileName).ToHashSet();

        string logFile = $"{writeFolder}\\Log-Visual-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt";

        Stopwatch watch = Stopwatch.StartNew();

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (finishedLayers.Contains(fileName))
                continue;

            watch.Restart();

            ProcessVisualQuality(file, writeFolder);

            File.AppendAllLines(logFile, new List<string>() { $"Finished At: {DateTime.Now.ToLongTimeString()}; Elapsed: {watch.ElapsedMilliseconds / 1000.0:N0} (s) - ({fileName})" });
        }
    }

    /// <summary>
    /// The features of the visual experiment, in the order that gives them their published index (1-based):
    /// single parts with 40–500 points, consecutive duplicates removed, features that still repeat a point dropped.
    /// </summary>
    public static List<Geometry<Point>> LoadFeatures(string shpFile)
    {
        var features = Shapefile.ReadShapes(shpFile)
                        .Select(g => g.AsGeometry())
                        .SelectMany(g => g.NumberOfGeometries > 1 ? g.Split(false) : [g])
                        .Where(g => !g.IsNullOrEmpty())
                        .Where(g => g.TotalNumberOfPoints > 40 && g.TotalNumberOfPoints < 500)
                        .ToList();

        foreach (var feature in features)
        {
            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();
            RestorePublishedRingOrientation(feature);
        }

        return features.Where(f => !f.HasDuplicatePoints()).ToList();
    }

    /// <summary>
    /// Outer rings clockwise, holes counter-clockwise: the orientation the shapefile reader gave the rings when the published
    /// runs were made (shapefiles store outer rings clockwise). Since 2026-10-13 the reader turns them to the OGC orientation
    /// (outer counter-clockwise), which reverses the direction in which order-dependent methods (NOPW, BOPW, RW, CTR, LRS) walk
    /// a ring and so changes their results on polygons; lines are not affected.
    /// </summary>
    public static void RestorePublishedRingOrientation(Geometry<Point> feature)
    {
        if (feature.Type == GeometryType.Polygon)
        {
            for (int i = 0; i < feature.Geometries!.Count; i++)
            {
                var points = feature.Geometries[i].Points;

                if (points is null || points.Count < 3)
                    continue;

                var outer = i == 0;

                if (SpatialUtility.IsClockwise(points) != outer)
                    points.Reverse();
            }
        }
        else if (feature.Type == GeometryType.MultiPolygon)
        {
            foreach (var polygon in feature.Geometries!)
                RestorePublishedRingOrientation(polygon);
        }
    }

    public static void ProcessVisualQuality(string shpFile, string outputDirectory)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = LoadFeatures(shpFile);

        if (features.IsNullOrEmpty())
            return;

        var originalColor = ColorHelper.ToWpfColor("#DE36A1");
        var simplifiedColor = ColorHelper.ToWpfColor("#08686E");

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var outputDirectoryForFeature = $"{outputDirectory}\\{fileName}\\{featureIndex}";

            Directory.CreateDirectory(outputDirectoryForFeature);

            var boundingBox = feature.GetBoundingBox().Expand(1.1);

            foreach (var renderSize in RenderSizes)
            {
                var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, renderSize, renderSize);

                var threshold = WebMercatorUtility.ToWebMercatorLength(zoomLevel, ThresholdInPixels);

                var screenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

                if (screenSize.Width * screenSize.Height <= 0)
                    continue;

                // scale denominator rounded down to thousands, for the file name
                var scale = Math.Floor((1.0 / WebMercatorUtility.GetGoogleMapScale(zoomLevel)) / 1000.0) * 1000;

                var originalFrame = feature.AsDrawingVisual(VisualParameters.Get(Colors.Transparent, originalColor, 2, 1), screenSize.Width, screenSize.Height, boundingBox);

                var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = Retain3Points };

                foreach (var method in Methods)
                {
                    var simplified = feature.Simplify(method, parameters);

                    if (simplified.IsNullOrEmpty() || simplified.Type == GeometryType.Point)
                        continue;

                    var simplifiedFrame = simplified.AsDrawingVisual(VisualParameters.Get(Colors.Transparent, simplifiedColor, 2, 1), screenSize.Width, screenSize.Height, boundingBox);

                    var compression = Math.Round(feature.Compression(simplified) * 100);

                    var tlvd = feature.CalculateTotalVectorDisplacementPerLength(simplified);

                    var fullFileName = $"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{zoomLevel}-{renderSize}-{method.GetDescription()}-{scale:N0}-{compression}%-{tlvd:#.00}.png";

                    ImageUtility.MergeAndSave(fullFileName, [originalFrame!, simplifiedFrame!], screenSize.Width, screenSize.Height);
                }
            }
        }
    }
}

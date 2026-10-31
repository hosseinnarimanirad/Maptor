using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Res.TrajectoryCompression;

/// <summary>
/// The assessment of the JSS paper: ten general-purpose algorithms, seven OSM layers, eleven threshold
/// coefficients and six McMaster measures per feature (<see cref="TestSingleFeatures"/>, one TSV row per
/// feature × coefficient × algorithm, the ">26 million records" of the paper). <see cref="StatisticsEnhancedTest"/>
/// writes the same measures as one row per feature × coefficient with per-measure ranks, and
/// <see cref="TestWithOutput"/> renders the hand-picked features of the paper's special-case figures.
/// </summary>
public static class SimplificationHelper
{
    private const bool Retain3Points = false;

    /// <summary>Base threshold in screen pixels on the 256² virtual screen (JSS paper, Section 4).</summary>
    private const double BaseThresholdInPixels = 10;

    /// <summary>Multipliers of the base threshold: 60 px down to about 1.5 px.</summary>
    private static readonly List<double> ThresholdCoefficients = new()
    {
        1.0 / 6, 1.0 / 5, 1.0 / 4, 1.0 / 3, 1.0 / 2, 1, 2, 3, 4, 5, 6
    };

    /// <summary>The ten algorithms of the paper, all accepting a length or area threshold.</summary>
    private static readonly List<SimplificationType> Methods = new()
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

    public enum Experiment
    {
        /// <summary>One row per feature, coefficient and algorithm (the published assessment).</summary>
        SingleFeatures,
        /// <summary>One row per feature and coefficient with all algorithms and their ranks as columns.</summary>
        PerFeatureRanks,
        /// <summary>Rendered comparisons of the hand-picked features listed in <see cref="IsSpecialCase"/>.</summary>
        VisualOutput,
        /// <summary>One row per layer: how many features the protocol of <see cref="LoadFeatures"/> keeps and why (<see cref="CountFeatures"/>).</summary>
        FeatureCounts,
    }

    // OSM dataset directory should be addressed here
    private const string DataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";

    /// <summary>Output directory; every run gets a dated folder below it, never a published one.</summary>
    public const string OutputRoot = @"E:\University.Ph.D\3. Paper1_ReviewPaper\AlgorithmRun";

    public async static Task GeneralTest(Experiment experiment = Experiment.SingleFeatures)
    {
        var files = Directory.EnumerateFiles(DataFolder, "*.shp", SearchOption.AllDirectories);

        var writeFolder = $@"{OutputRoot}\{DateTime.Now:yyyy-MM-dd}-{experiment}";

        Directory.CreateDirectory(writeFolder);

        using var writer = new StreamWriter($"{writeFolder}\\All-summary.txt", false);

        if (experiment == Experiment.SingleFeatures)
            writer.WriteLine(SingleSimplifiedGeometryLog.GetHeader());

        if (experiment == Experiment.FeatureCounts)
            writer.WriteLine("Layer\tRecords\tParts\tPartsWith15To500Points\tFeatures\tPoints\tZoomBelow10\tZoom10To20\tZoomAbove20");

        // layers whose summary already exists in the output folder are skipped, so an interrupted run can be resumed
        var finishedLayers = Directory.EnumerateFiles(writeFolder, "*.txt", SearchOption.AllDirectories)
                                      .Select(s => System.IO.Path.GetFileNameWithoutExtension(s).Replace("-summary", string.Empty))
                                      .ToHashSet();

        string logFile = $"{writeFolder}\\Log-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt";

        Stopwatch watch = Stopwatch.StartNew();

        var includeHeader = true;

        foreach (var file in files)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(file);

            if (finishedLayers.Contains(fileName))
                continue;

            finishedLayers.Add(fileName);

            watch.Restart();

            switch (experiment)
            {
                case Experiment.SingleFeatures:
                    TestSingleFeatures(file, writer);
                    break;

                case Experiment.PerFeatureRanks:
                    StatisticsEnhancedTest(file, writer, includeHeader);
                    includeHeader = false;
                    break;

                case Experiment.VisualOutput:
                    await TestWithOutput(file, writer, includeHeader, writeFolder);
                    includeHeader = false;
                    break;

                case Experiment.FeatureCounts:
                    writer.WriteLine(CountFeatures(file));
                    writer.Flush();
                    break;
            }

            File.AppendAllLines(logFile, new List<string>() { $"Finished At: {DateTime.Now.ToLongTimeString()}; Elapsed: {watch.ElapsedMilliseconds / 1000.0:N0} (s) - ({fileName})" });
        }
    }

    /// <summary>
    /// The published assessment. Each feature (multi-part geometries split, 15 &lt; points &lt; 500) gets the zoom
    /// level at which its bounding box fits a 256² screen; the base threshold is 10 px at that level and each of
    /// the eleven coefficients is applied to it for the ten algorithms.
    /// </summary>
    public static void TestSingleFeatures(string shpFile, StreamWriter writer)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = LoadFeatures(shpFile, splitAllMultiParts: true);

        var methodNames = Methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 256, 256);

            var baseThreshold = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, BaseThresholdInPixels);

            foreach (var coef in ThresholdCoefficients)
            {
                var threshold = baseThreshold * coef;

                var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = Retain3Points };

                foreach (var method in Methods)
                {
                    stopwatch.Restart();

                    var simplified = feature.Simplify(method, parameters);

                    stopwatch.Stop();

                    if (simplified.IsNullOrEmpty())
                        continue;

                    if (simplified.Type == GeometryType.Point)
                        continue;

                    try
                    {
                        writer.WriteLine(
                          new SingleSimplifiedGeometryLog(fileName,
                                                boundingBox,
                                                featureIndex,
                                                feature,
                                                simplified,
                                                estimatedZoomLevel,
                                                methodNames[method],
                                                stopwatch.ElapsedMilliseconds,
                                                parameters,
                                                coef).ToTsv());
                    }
                    catch (Exception ex)
                    {
                        // one failed measure must not abort an hour-long run; the row is skipped and the case recorded
                        var folder = Path.GetDirectoryName(((FileStream)writer.BaseStream).Name)!;

                        File.AppendAllText(Path.Combine(folder, "Failures.txt"),
                            $"{fileName}\t{featureIndex}\t{coef}\t{methodNames[method]}\t{ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                    }
                }
            }
        }
    }


    /// <summary>
    /// Reads the layer as single-part features with 15 &lt; points &lt; 500 (the filter of the published assessment),
    /// removes consecutive duplicate points and drops features that still contain duplicates. Polygons are split
    /// into one polygon per ring. Ring orientation is restored to that of the published run.
    /// </summary>
    private static List<Geometry<Point>> LoadFeatures(string shpFile, bool splitAllMultiParts)
    {
        var features = Shapefile.ReadShapes(shpFile)
                                .Select(g => g.AsGeometry())
                                .SelectMany(g => splitAllMultiParts || g.NumberOfGeometries > 1 ? g.Split(false) : new List<Geometry<Point>>() { g })
                                .Where(g => !g.IsNullOrEmpty())
                                .Where(g => g.TotalNumberOfPoints > 15 && g.TotalNumberOfPoints < 500)
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
    /// Outer rings clockwise, holes counter-clockwise: the orientation the shapefile reader gave the rings when the
    /// published run (1403-03-10) was made. Since 2026-10-13 the reader turns them to the OGC orientation (outer
    /// counter-clockwise), which reverses the direction in which order-dependent methods (RW, NOPW, BOPW, SF, APSC)
    /// walk a ring and so changes their results on polygons; lines are not affected. Same as
    /// IRI.Maptor.Res.FastSimplification.SimplificationHelper.RestorePublishedRingOrientation.
    /// </summary>
    private static void RestorePublishedRingOrientation(Geometry<Point> feature)
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

    /// <summary>
    /// One TSV row for the layer: shapefile records, single parts (rings) after splitting, parts with 15 &lt; points &lt; 500,
    /// features that survive duplicate-point removal (= the features of <see cref="TestSingleFeatures"/>), their points,
    /// and how many of them get a natural zoom level below 10, within 10–20 and above 20 (the paper's Tables 3 and 4
    /// report the 10–20 band only).
    /// </summary>
    public static string CountFeatures(string shpFile)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        int records = 0, parts = 0, inRange = 0;

        foreach (var shape in Shapefile.ReadShapes(shpFile))
        {
            records++;

            foreach (var part in shape.AsGeometry().Split(false))
            {
                if (part.IsNullOrEmpty())
                    continue;

                parts++;

                if (part.TotalNumberOfPoints > 15 && part.TotalNumberOfPoints < 500)
                    inRange++;
            }
        }

        var features = LoadFeatures(shpFile, splitAllMultiParts: true);

        int below = 0, within = 0, above = 0;

        foreach (var feature in features)
        {
            var level = WebMercatorUtility.EstimateZoomLevel(feature.GetBoundingBox(), 256, 256);

            if (level < 10) below++;
            else if (level > 20) above++;
            else within++;
        }

        return string.Join('\t', fileName, records, parts, inRange, features.Count, features.Sum(f => f.TotalNumberOfPoints), below, within, above);
    }

    #region Per Feature

    /// <summary>
    /// Same protocol as <see cref="TestSingleFeatures"/>, but one row per feature and coefficient: the six
    /// measures of all ten algorithms are columns, each with its rank among the algorithms (<see cref="NineAlgoSixMeasureLog"/>).
    /// </summary>
    public static void StatisticsEnhancedTest(string shpFile, StreamWriter writer, bool includeHeader)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = LoadFeatures(shpFile, splitAllMultiParts: true);

        int featureIndex = 0;

        bool addHeaderLine = includeHeader;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 256, 256);

            var baseThreshold = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, BaseThresholdInPixels);

            foreach (var coef in ThresholdCoefficients)
            {
                var threshold = baseThreshold * coef;

                var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = Retain3Points };

                var temp = new NineAlgoSixMeasureLog
                            (fileName,
                                        boundingBox,
                                        featureIndex,
                                        feature,
                                        estimatedZoomLevel,
                                        parameters,
                                        coef);

                foreach (var method in Methods)
                {
                    var simplified = feature.Simplify(method, parameters);

                    if (simplified.IsNullOrEmpty())
                        continue;

                    if (simplified.Type == GeometryType.Point)
                        continue;

                    temp.AddAlgoResult(method, feature, simplified);
                }

                temp.UpdateAllRanks();

                if (addHeaderLine == true)
                {
                    writer.WriteLine(temp.GetHeader());
                    addHeaderLine = false;
                }

                writer.WriteLine(temp.ToTsv());
            }
        }
    }

    #endregion

    #region Test with visual output

    /// <summary>
    /// Renders the hand-picked features of <see cref="IsSpecialCase"/> (the RDP-versus-SF cases of the JSS paper,
    /// Figure 9) with RDP and SF at seven coefficients, as one PNG per coefficient plus the GeoJSON of every
    /// result, and writes the per-feature rank rows.
    /// </summary>
    public static async Task TestWithOutput(string shpFile, StreamWriter writer, bool includeHeader, string outputDirectory)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var coefficients = new List<double> { 1.0 / 5, 1.0 / 4, 1.0 / 2, 1, 2, 4, 5 };

        // only geometries that are actually multi-part are split, so the feature indices match the published run
        var features = LoadFeatures(shpFile, splitAllMultiParts: false);

        var methods = new List<SimplificationType>
        {
            SimplificationType.RamerDouglasPeucker,
            SimplificationType.SleeveFitting,
        };

        var colors = new Dictionary<SimplificationType, System.Windows.Media.Color>()
        {
            {SimplificationType.RamerDouglasPeucker, IRI.Maptor.Presentation.Wpf.Helpers.ColorHelper.ToWpfColor("#E051AD")},
            {SimplificationType.SleeveFitting, IRI.Maptor.Presentation.Wpf.Helpers.ColorHelper.ToWpfColor("#3B878C")},
        };

        int featureIndex = 0;

        bool addHeaderLine = includeHeader;

        foreach (var feature in features)
        {
            featureIndex++;

            if (!IsSpecialCase(featureIndex, fileName)) continue;

            var outputDirectoryForFeature = $"{outputDirectory}\\{fileName}\\{featureIndex}";

            if (!Directory.Exists(outputDirectoryForFeature))
                Directory.CreateDirectory(outputDirectoryForFeature);

            var boundingBox = feature.GetBoundingBox().Expand(1.1);

            var estimatedZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 256, 256);

            var baseThreshold = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, BaseThresholdInPixels);

            var originalFrame = await GetAsFrame(
                "original",
                estimatedZoomLevel,
                boundingBox,
                feature,
                IRI.Maptor.Presentation.Wpf.Helpers.ColorHelper.ToWpfColor("#C0C0C0"),
                4,
                1);

            GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

            await originalFeatureSet.SaveAsync($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-original.json", false, true);


            foreach (var coef in coefficients)
            {
                var threshold = baseThreshold * coef;

                var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = Retain3Points };

                var temp = new NineAlgoSixMeasureLog(fileName, boundingBox, featureIndex, feature, estimatedZoomLevel, parameters, coef);

                List<DrawingVisual> drawingVisuals = [.. originalFrame];

                foreach (var method in methods)
                {
                    var simplified = feature.Simplify(method, parameters);

                    if (simplified.IsNullOrEmpty())
                        continue;

                    if (simplified.Type == GeometryType.Point)
                        continue;

                    temp.AddAlgoResult(method, feature, simplified);

                    var simplifiedFrame = await GetAsFrame(method.ToString(), estimatedZoomLevel, boundingBox, simplified, colors[method], 2, 0.9);

                    drawingVisuals.AddRange(simplifiedFrame);

                    GeoJsonFeatureSet featureSet = simplified.AsGeoJsonFeatureSet();

                    await featureSet.SaveAsync($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{coef}-{method}.json", false, true);
                }

                var screenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

                IRI.Maptor.Presentation.Wpf.Helpers.ImageUtility.MergeAndSave($"{outputDirectoryForFeature}\\{fileName}-{featureIndex}-{estimatedZoomLevel}-{coef}.png", drawingVisuals, screenSize.Width, screenSize.Height);

                temp.UpdateAllRanks();

                if (addHeaderLine == true)
                {
                    writer.WriteLine(temp.GetHeader());
                    addHeaderLine = false;
                }

                writer.WriteLine(temp.ToTsv());
            }
        }
    }

    /// <summary>Feature indices (in the order of <see cref="LoadFeatures"/>) of the JSS special cases, per layer.</summary>
    private static bool IsSpecialCase(int featureIndex, string fileName)
    {
        var type = fileName.Split('_').First();

        switch (type)
        {
            case "AdminArea":
                return featureIndex == 1507 || featureIndex == 995 || featureIndex == 1117 || featureIndex == 1219
                        || featureIndex == 1184 || featureIndex == 1219 || featureIndex == 1529;

            case "Building":
                // SF is better (less TLVD per length) -Z2
                return featureIndex == 1322 || featureIndex == 2181 ||
                    // RDP is better (less TLVD per length)
                    featureIndex == 902 || featureIndex == 8760 || featureIndex == 3815 || featureIndex == 8760 ||

                    // SF is better (less TLVD per length) - Z1
                    featureIndex == 22523;

            case "LandUse":
            case "Water":
                return featureIndex == 36;

            case "Railway":
                // SF is better (less TLVD per length) -Z2
                return featureIndex == 149 || featureIndex == 359 ||
                    // RDP is better (less TLVD per length)
                    featureIndex == 747 || featureIndex == 192;

            case "Road":
                return new List<int>()
                {
                    // SF is better (less TLVD per length)
                     136284, 7727, 108622, 134791, 8593,51092, 115606,

                     // RDP is better (less TLVD per length)
                     80721, 119726, 33390, 91799, 103402, 112277, 43487, 7823, 127214
                }.Contains(featureIndex);

            case "Waterways":
                // SF is better (less TLVD per length)
                return featureIndex == 17658 ||

                    // RDP is better (less TLVD per length)
                    featureIndex == 5054;

            default:
                return false;
        }
    }

    private static async Task<List<DrawingVisual>> GetAsFrame(
        string layerName,
        int level,
        BoundingBox groundBoundingBox,
        Geometry<Point> geometry,
        System.Windows.Media.Color color,
        int strokeThickness,
        double opacity)
    {
        var vectorLayer = new VectorLayer(layerName,
                                            new List<Geometry<Point>>() { geometry },
                                            VisualParameters.Get(Colors.Transparent, color, strokeThickness, opacity),
                                            LayerType.VectorLayer,
                                            RenderMode.Default,
                                            RasterizationMethod.DrawingVisual);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(level, groundBoundingBox);

        var scale = WebMercatorUtility.GetGoogleMapScale(level);

        return await vectorLayer.AsDrawingVisual(groundBoundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);
    }

    #endregion

}

using System.IO;
using System.Text;
using System.Diagnostics;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// The comparison published in the LAS paper (Figure 8, Appendix A) and run on 2024-10-08 into
/// <c>4. Paper2\AlgorithmRun\1403-07-17-single feature multi threshold 128</c> (and <c>... 256</c>
/// with <paramref name="screenSize"/> = 256).
/// <para>
/// Protocol: 50 random features per layer with 30–2000 points; each feature gets its own zoom level,
/// the largest at which its bounding box fits a <paramref name="screenSize"/>² screen. The LRS models
/// (threshold-free) and RDP, RW, NOPW and BOPW at 2, 1 and 0.5 times the 1-pixel threshold are
/// rendered as 1-px rasters and compared with the original feature through the pixel confusion
/// matrix (TP/FP/FN, F1), compression and TLVD per length; since 2026-09-28 also the tolerant F1,
/// pixel displacement and Hausdorff distance of <see cref="Core.Common.Mathematics.RasterComparison"/>.
/// </para>
/// <para>
/// Changes since the published run (2026-09-26): the reference raster contains only the feature under
/// test (it used to contain all sampled features, so neighbours counted as false negatives); the
/// sample is seeded; the LRS rows are written once per feature with the 1-pixel thresholds instead of
/// once per coefficient with the previous loop's thresholds; elapsed milliseconds measure the
/// simplification only. The published numbers therefore do not reproduce exactly with this code.
/// </para>
/// </summary>
public static class SingleFeatureVaryingThresholdExperiment
{
    public static readonly List<SimplificationType> Methods = new()
    {
        SimplificationType.RamerDouglasPeucker,
        SimplificationType.ReumannWitkam,
        SimplificationType.NormalOpeningWindow,
        SimplificationType.BeforeOpeningWindow,
    };

    /// <summary>The published thresholds: 2, 1 and 0.5 pixels.</summary>
    public static readonly double[] ThresholdCoefficients = { 2, 1, 0.5 };

    /// <summary>
    /// The denser grid of the revision, so that every baseline traces a compression–quality curve per
    /// feature and can be interpolated at the compression a threshold-free model reaches.
    /// </summary>
    public static readonly double[] RevisionThresholdCoefficients = { 4, 3, 2, 1.5, 1, 0.75, 0.5, 0.25 };

    /// <summary>The published protocol: a seeded sample of 50 features (30–2000 points) of the layer.</summary>
    public static Task RunAsync(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int screenSize = 128)
    {
        var features = ExperimentData.LoadFeatures(shpFile, minPoints: 30, maxPoints: 2000, sampleSize: 50)
                                     .Select((feature, i) => (i + 1, feature))
                                     .ToList();

        return RunAsync(Path.GetFileNameWithoutExtension(shpFile), features, writer, outputDirectory, lrModels, screenSize, ThresholdCoefficients);
    }

    /// <summary>
    /// The protocol on a given list of features, each with the index it is reported under (the revision
    /// re-runs the features of the published run under their published indices), with RDP, RW, NOPW and BOPW
    /// at the given threshold coefficients.
    /// </summary>
    public static Task RunAsync(string fileName, List<(int Index, Geometry<Point> Feature)> features, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int screenSize, double[] thresholdCoefficients)
    {
        return RunAsync(fileName, features, writer, outputDirectory, lrModels, screenSize, StandardBaselines(thresholdCoefficients));
    }

    /// <summary>
    /// The protocol with any baselines: every feature at the zoom level where it fits a <paramref name="screenSize"/>²
    /// screen, every model once (threshold-free), every baseline at each of its coefficients; one row per run in
    /// <paramref name="writer"/>. With <paramref name="saveFiles"/> the rasters, difference images and per-feature
    /// logs are written too.
    /// </summary>
    public static async Task RunAsync(string fileName, List<(int Index, Geometry<Point> Feature)> features, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int screenSize, IReadOnlyList<Baseline> baselines, bool saveFiles = true)
    {
        var stopwatch = new Stopwatch();

        foreach (var (featureIndex, feature) in features)
        {
            var boundingBox = feature.GetBoundingBox();

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, screenSize, screenSize);

            var pixelLength = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            // the reference raster: this feature alone
            var originalBitmap = SimplificationHelper.RenderForMetric(new() { feature }, boundingBox, currentScreenSize);

            if (saveFiles)
            {
                originalBitmap.Save($"{outputDirectory}\\{fileName}-original-F{featureIndex}-Z{zoomLevel}.png", System.Drawing.Imaging.ImageFormat.Tiff);

                await feature.AsGeoJsonFeatureSet().SaveAsync($"{outputDirectory}\\{fileName}-F{featureIndex}.json", false, true);
            }

            var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            // LRS needs no threshold: one run per model per feature; the rows carry the 1-px thresholds for reference
            var baseParameters = CreateParameters(pixelLength);

            var accuracies = new List<SimplificationAccuracy>();

            foreach (var lrModel in lrModels)
            {
                var result = await SimplificationHelper.CompareBySingleFeature(
                                feature,
                                g => g.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points),
                                boundingBox,
                                currentScreenSize,
                                originalBitmap,
                                $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C1",
                                lrModel.Title,
                                zoomLevel,
                                stopwatch,
                                saveFiles);

                if (result.Item1 is null)
                    continue;

                result.Item1.FeatureIndex = featureIndex;

                accuracies.Add(result.Item1);

                writer.WriteLine(ToLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, lrModel.Title, 1, baseParameters, result.Item1).ToTsv());
            }

            foreach (var baseline in baselines)
            {
                foreach (var coef in baseline.Coefficients)
                {
                    var parameters = baseline.Parameters(pixelLength, coef);

                    var result = await SimplificationHelper.CompareBySingleFeature(
                                   feature,
                                   g => baseline.Apply(g, parameters),
                                   boundingBox,
                                   currentScreenSize,
                                   originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{coef}",
                                   baseline.Name,
                                   zoomLevel,
                                   stopwatch,
                                   saveFiles);

                    if (result.Item1 is null)
                        continue;

                    result.Item1.FeatureIndex = featureIndex;

                    accuracies.Add(result.Item1);

                    writer.WriteLine(ToLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, baseline.Name, coef, parameters, result.Item1).ToTsv());
                }
            }

            if (saveFiles)
                WriteAccuracyLog($"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-log.txt", accuracies);
        }
    }

    /// <summary>
    /// The visual-limit protocol (E11): every feature at the zoom level where it fits a <paramref name="screenSize"/>² screen,
    /// every model once, and every baseline at the thresholds of <paramref name="thresholdsPx"/> (pixels, ascending) until
    /// <paramref name="stopAfterFailures"/> consecutive results are visibly different (excess displacement &gt; 0, i.e. some
    /// pixel of either raster lies beyond the 8-neighbourhood of the other). One row per run, as in <see cref="RunAsync"/>;
    /// the coefficient column holds the threshold in pixels. The largest compression among the visually equivalent rows
    /// of a feature is its "compression at the visual limit", computed afterwards from the rows.
    /// </summary>
    public static async Task RunVisualLimitAsync(string fileName, List<(int Index, Geometry<Point> Feature)> features, StreamWriter writer, List<LogisticSimplification<Point>> lrModels, int screenSize, IReadOnlyList<Baseline> baselines, double[] thresholdsPx, int stopAfterFailures, int marginPixels = -1)
    {
        var stopwatch = new Stopwatch();

        foreach (var (featureIndex, feature) in features)
        {
            var boundingBox = feature.GetBoundingBox();

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, screenSize, screenSize);

            var pixelLength = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            // marginPixels >= 0 (resubmission runs, 2026-10-06): exact canvas, see MetricCanvas
            if (marginPixels >= 0)
                (boundingBox, currentScreenSize) = MetricCanvas(boundingBox, pixelLength, marginPixels);

            var originalBitmap = SimplificationHelper.RenderForMetric(new() { feature }, boundingBox, currentScreenSize);

            var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var baseParameters = CreateParameters(pixelLength);

            foreach (var lrModel in lrModels)
            {
                var result = await SimplificationHelper.CompareBySingleFeature(feature, g => g.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points),
                                                                               boundingBox, currentScreenSize, originalBitmap, fileName, lrModel.Title, zoomLevel, stopwatch, false, measureCollapsed: true);
                if (result.Item1 is null)
                    continue;

                writer.WriteLine(ToLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, lrModel.Title, 1, baseParameters, result.Item1).ToTsv());
            }

            foreach (var baseline in baselines)
            {
                var failures = 0;

                foreach (var threshold in thresholdsPx)
                {
                    var parameters = baseline.Parameters(pixelLength, threshold);

                    var result = await SimplificationHelper.CompareBySingleFeature(feature, g => baseline.Apply(g, parameters),
                                                                                   boundingBox, currentScreenSize, originalBitmap, fileName, baseline.Name, zoomLevel, stopwatch, false, measureCollapsed: true);
                    if (result.Item1 is null)
                        continue;

                    writer.WriteLine(ToLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, baseline.Name, threshold, parameters, result.Item1).ToTsv());

                    failures = result.Item1.Raster is { ExcessDisplacement: > 0 } ? failures + 1 : 0;

                    if (failures >= stopAfterFailures)
                        break;
                }
            }

            originalBitmap.Dispose();
        }
    }

    /// <summary>
    /// The canvas of the resubmission runs (2026-10-06): one screen pixel per pixel of the zoom level in both axes and
    /// <paramref name="marginPixels"/> empty pixels on every side. <see cref="WebMercatorUtility.ToScreenSize"/> enlarges the
    /// extent by 1.2 and truncates, and the renderer fits the extent into it, so a feature used to be drawn at 1.0–1.2 times
    /// its zoom level's scale with its tighter side on the canvas edge, where the aliased renderer can round lines off the
    /// canvas (a 2-px-high canvas drew 3 of a feature's pixels). The returned extent has exactly the returned size in pixels.
    /// </summary>
    public static (BoundingBox Extent, System.Drawing.Size Size) MetricCanvas(BoundingBox featureBox, double pixelLength, int marginPixels)
    {
        var width = (int)Math.Ceiling(featureBox.Width / pixelLength) + 2 * marginPixels;
        var height = (int)Math.Ceiling(featureBox.Height / pixelLength) + 2 * marginPixels;

        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        var xMin = featureBox.XMin - marginPixels * pixelLength;
        var yMax = featureBox.YMax + marginPixels * pixelLength;

        return (new BoundingBox(xMin, yMax - height * pixelLength, xMin + width * pixelLength, yMax), new System.Drawing.Size(width, height));
    }

    /// <summary>A threshold-driven baseline: its parameters for a pixel length (map units) and a coefficient.</summary>
    public sealed record Baseline(string Name, SimplificationType Type, double[] Coefficients, Func<double, double, SimplificationParameters> Parameters, Func<List<Point>, SimplificationParameters, List<Point>>? Filter = null)
    {
        /// <summary>
        /// The library method of <see cref="Type"/>, or <see cref="Filter"/> applied to every ring/part when given (variants
        /// outside the enum). Like <see cref="Geometry{T}.Simplify(SimplificationType, SimplificationParameters)"/>, the
        /// filter sees the rings of a polygon with <see cref="SimplificationParameters.IsRing"/> set (on a copy).
        /// </summary>
        public Geometry<Point> Apply(Geometry<Point> geometry, SimplificationParameters parameters)
        {
            if (Filter is null)
                return geometry.Simplify(Type, parameters);

            var filterParameters = parameters.Clone();

            filterParameters.IsRing = geometry.IsRingBase();

            return geometry.FilterPoints(points => Filter(points, filterParameters));
        }
    }

    /// <summary>RDP, RW, NOPW and BOPW with distance (and area) thresholds of coefficient × 1 px.</summary>
    public static List<Baseline> StandardBaselines(double[] coefficients)
        => Methods.Select(m => new Baseline(m.GetDescription(), m, coefficients, (pixel, coef) => CreateParameters(pixel * coef))).ToList();

    private static VisualSimplificationLog ToLog(string fileName, BoundingBox boundingBox, int featureIndex, Geometry<Point> feature, Geometry<Point> simplified, int zoomLevel, string methodName, double coef, SimplificationParameters parameters, SimplificationAccuracy accuracy)
        => new(fileName, boundingBox, featureIndex, feature, simplified, zoomLevel, methodName, accuracy.ElapsedMilliseconds, coef, parameters, accuracy.TlvdPerLength, accuracy.ConfusionMatrix, accuracy.Raster);

    internal static SimplificationParameters CreateParameters(double threshold)
        => new() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = LRHelper.Retain3Points };

    /// <summary>Wraps a simplification so that <paramref name="stopwatch"/> measures it alone (not the rendering that follows); used by the layer and multi-scale experiments.</summary>
    internal static Func<Geometry<Point>, Geometry<Point>> Timed(Stopwatch stopwatch, Func<Geometry<Point>, Geometry<Point>> simplify)
        => geometry =>
        {
            stopwatch.Restart();

            var result = simplify(geometry);

            stopwatch.Stop();

            return result;
        };

    internal static void WriteAccuracyLog(string fileName, IEnumerable<SimplificationAccuracy> accuracies)
    {
        var builder = new StringBuilder();

        builder.AppendLine(SimplificationAccuracy.GetTsvHeader());

        foreach (var accuracy in accuracies)
            builder.AppendLine(accuracy.ToTsv());

        File.WriteAllText(fileName, builder.ToString());
    }
}

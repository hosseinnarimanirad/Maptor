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
/// matrix (TP/FP/FN, F1), compression and TLVD per length.
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

    public static readonly double[] ThresholdCoefficients = { 2, 1, 0.5 };

    public static async Task RunAsync(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int screenSize = 128)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = ExperimentData.LoadFeatures(shpFile, minPoints: 30, maxPoints: 2000, sampleSize: 50);

        var methodNames = Methods.ToDictionary(m => m, m => m.GetDescription());

        var stopwatch = new Stopwatch();

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, screenSize, screenSize);

            var pixelLength = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

            // the reference raster: this feature alone
            var originalBitmap = await GeneralHelper.GetAsLayer("original", new() { feature })
                                                    .AsGdiBitmapAsync(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

            originalBitmap.Save($"{outputDirectory}\\{fileName}-original-F{featureIndex}-Z{zoomLevel}.png", System.Drawing.Imaging.ImageFormat.Tiff);

            var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            await feature.AsGeoJsonFeatureSet().SaveAsync($"{outputDirectory}\\{fileName}-F{featureIndex}.json", false, true);

            // LRS needs no threshold: one run per model per feature; the rows carry the 1-px thresholds for reference
            var baseParameters = CreateParameters(pixelLength);

            var lrAccuracies = new List<SimplificationAccuracy>();

            foreach (var lrModel in lrModels)
            {
                var result = await SimplificationHelper.CompareBySingleFeature(
                                feature,
                                Timed(stopwatch, g => g.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points)),
                                boundingBox,
                                currentScreenSize,
                                originalBitmap,
                                $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C1",
                                lrModel.Title,
                                zoomLevel);

                if (result.Item1 is null)
                    continue;

                lrAccuracies.Add(result.Item1);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, lrModel.Title,
                                                             stopwatch.ElapsedMilliseconds, 1, baseParameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
            }

            foreach (var coef in ThresholdCoefficients)
            {
                var accuracies = new List<SimplificationAccuracy>(lrAccuracies);

                var parameters = CreateParameters(pixelLength * coef);

                foreach (var method in Methods)
                {
                    var result = await SimplificationHelper.CompareBySingleFeature(
                                   feature,
                                   Timed(stopwatch, g => g.Simplify(method, parameters)),
                                   boundingBox,
                                   currentScreenSize,
                                   originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{coef}",
                                   methodNames[method],
                                   zoomLevel);

                    if (result.Item1 is null)
                        continue;

                    accuracies.Add(result.Item1);

                    writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, methodNames[method],
                                                                 stopwatch.ElapsedMilliseconds, coef, parameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
                }

                WriteAccuracyLog($"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{coef}-log.txt", accuracies);
            }
        }
    }

    internal static SimplificationParameters CreateParameters(double threshold)
        => new() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = LRHelper.Retain3Points };

    /// <summary>Wraps a simplification so that <paramref name="stopwatch"/> measures it alone (not the rendering that follows).</summary>
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

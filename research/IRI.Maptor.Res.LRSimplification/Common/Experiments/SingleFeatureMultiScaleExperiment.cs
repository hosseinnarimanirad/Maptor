using System.IO;
using System.Diagnostics;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Single features rendered at several scales: the zoom level at which the feature fits a 512² screen and
/// two levels below it. LRS models run at the base level only (they are threshold-free); RDP, RW, NOPW and
/// BOPW run at every level with the 1-pixel threshold of that level. Output naming matches
/// <c>4. Paper2\AlgorithmRun\1403-07-17-single feature multi scale</c> and <c>... multi scale-2</c>.
/// The sample is seeded and the elapsed time measures the simplification only (2026-09-26).
/// </summary>
public static class SingleFeatureMultiScaleExperiment
{
    public static readonly List<SimplificationType> Methods = new()
    {
        SimplificationType.RamerDouglasPeucker,
        SimplificationType.ReumannWitkam,
        SimplificationType.NormalOpeningWindow,
        SimplificationType.BeforeOpeningWindow,
    };

    /// <summary>Zoom-level offsets below the base level.</summary>
    public static readonly int[] LevelOffsets = { 0, 2 };

    public static async Task RunAsync(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = ExperimentData.LoadFeatures(shpFile, minPoints: 15, maxPoints: 500, sampleSize: 100);

        var methodNames = Methods.ToDictionary(m => m, m => m.GetDescription());

        var stopwatch = new Stopwatch();

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var baseZoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 512, 512);

            await feature.AsGeoJsonFeatureSet().SaveAsync($"{outputDirectory}\\{fileName}-F{featureIndex}.json", false, true);

            foreach (var offset in LevelOffsets)
            {
                var zoomLevel = baseZoomLevel - offset;

                if (zoomLevel < 4)
                    continue;

                var pixelLength = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

                var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);


                if (currentScreenSize.Width <= 0 || currentScreenSize.Height <= 0 || currentScreenSize.Width > 10000 || currentScreenSize.Height > 10000)
                    continue;

                var originalBitmap = SimplificationHelper.RenderForMetric(new() { feature }, boundingBox, currentScreenSize);

                originalBitmap.Save($"{outputDirectory}\\{fileName}-{zoomLevel}-F{featureIndex}-original.png", System.Drawing.Imaging.ImageFormat.Tiff);

                var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

                var parameters = SingleFeatureVaryingThresholdExperiment.CreateParameters(pixelLength);

                var accuracies = new List<SimplificationAccuracy>();

                var layerName = $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{offset}";

                if (offset == 0)
                {
                    foreach (var lrModel in lrModels)
                    {
                        var result = await SimplificationHelper.CompareBySingleFeature(
                                        feature,
                                        SingleFeatureVaryingThresholdExperiment.Timed(stopwatch, g => g.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points)),
                                        boundingBox, currentScreenSize, originalBitmap, layerName, lrModel.Title, zoomLevel);

                        if (result.Item1 is null)
                            continue;

                        accuracies.Add(result.Item1);

                        writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, lrModel.Title,
                                                                     stopwatch.ElapsedMilliseconds, offset, parameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
                    }
                }

                foreach (var method in Methods)
                {
                    var result = await SimplificationHelper.CompareBySingleFeature(
                                   feature,
                                   SingleFeatureVaryingThresholdExperiment.Timed(stopwatch, g => g.Simplify(method, parameters)),
                                   boundingBox, currentScreenSize, originalBitmap, layerName, methodNames[method], zoomLevel);

                    if (result.Item1 is null)
                        continue;

                    accuracies.Add(result.Item1);

                    writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, methodNames[method],
                                                                 stopwatch.ElapsedMilliseconds, offset, parameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
                }

                SingleFeatureVaryingThresholdExperiment.WriteAccuracyLog($"{layerName}-log.txt", accuracies);
            }
        }
    }
}

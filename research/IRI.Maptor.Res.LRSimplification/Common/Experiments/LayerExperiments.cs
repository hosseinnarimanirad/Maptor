using System.IO;
using System.Diagnostics;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Layer-level comparisons: all features of a layer are simplified and rendered together at one scale
/// (the scale of the whole layer, not of each feature), so the pixel confusion matrix, compression and
/// TLVD are layer aggregates.
/// </summary>
public static class LayerExperiments
{
    /// <summary>
    /// Whole layer at the zoom level where its bounding box fits a <paramref name="screenSize"/>² screen; LRS
    /// models versus RDP, RW, NOPW and BOPW at 2, 1 and 0.5 times the 1-pixel threshold. Output naming matches
    /// <c>4. Paper2\AlgorithmRun\1403-07-17-layerbased-large data 512</c> and <c>... 1024</c>.
    /// The elapsed time measures the simplification of the whole layer (2026-09-26; it used to be cumulative).
    /// </summary>
    public static async Task RunVaryingThresholdAsync(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int screenSize = 1024)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = ExperimentData.LoadFeatures(shpFile, minPoints: 15, maxPoints: 3000, excludeGeometryCollections: true);

        var methods = SingleFeatureVaryingThresholdExperiment.Methods;

        var methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        var coefs = SingleFeatureVaryingThresholdExperiment.ThresholdCoefficients;

        var stopwatch = new Stopwatch();

        var boundingBox = features.GetBoundingBox();

        var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, screenSize, screenSize);

        var pixelLength = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

        var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

        var originalBitmap = await GeneralHelper.GetAsLayer("original", features).AsGdiBitmapAsync(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

        originalBitmap.Save($"{outputDirectory}\\{fileName}-{zoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

        var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

        var baseParameters = SingleFeatureVaryingThresholdExperiment.CreateParameters(pixelLength);

        foreach (var lrModel in lrModels)
        {
            var result = await SimplificationHelper.CompareByLayer(
                     features,
                     SingleFeatureVaryingThresholdExperiment.Timed(stopwatch, g => g.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points)),
                     boundingBox, currentScreenSize, originalBitmap, $"{outputDirectory}\\{fileName}-{zoomLevel}", lrModel.Title, zoomLevel);

            writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, result.simplifieds, zoomLevel, lrModel.Title,
                                                         stopwatch.ElapsedMilliseconds, 1, baseParameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
        }

        foreach (var method in methods)
        {
            foreach (var coef in coefs)
            {
                var parameters = SingleFeatureVaryingThresholdExperiment.CreateParameters(pixelLength * coef);

                var result = await SimplificationHelper.CompareByLayer(
                         features,
                         SingleFeatureVaryingThresholdExperiment.Timed(stopwatch, g => g.Simplify(method, parameters)),
                         boundingBox, currentScreenSize, originalBitmap, $"{outputDirectory}\\{fileName}-{zoomLevel}-{coef}", methodNames[method], zoomLevel);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, result.simplifieds, zoomLevel, methodNames[method],
                                                             stopwatch.ElapsedMilliseconds, coef, parameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
            }
        }
    }

    /// <summary>
    /// Whole layer (multi-part geometries not split) at fixed web-map zoom levels, LRS models versus
    /// RDP, VW, NOPW, BOPW and APSC at the 1-pixel threshold of each level; writes the rendered layers and
    /// difference images. Output naming matches <c>4. Paper2\AlgorithmRun\1403-07-17-single feature admin area</c>.
    /// </summary>
    public static async Task RunByZoomLevelAsync(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int[] zoomLevels)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = ExperimentData.LoadFeatures(shpFile, minPoints: 0, maxPoints: 4000, splitMultiPart: false);

        var methods = new List<SimplificationType>
        {
            SimplificationType.RamerDouglasPeucker,
            SimplificationType.VisvalingamWhyatt,
            SimplificationType.NormalOpeningWindow,
            SimplificationType.BeforeOpeningWindow,
            SimplificationType.APSC,
        };

        var methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        var stopwatch = new Stopwatch();

        var boundingBox = features.GetBoundingBox();

        foreach (var zoomLevel in zoomLevels)
        {
            var pixelLength = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

            var originalBitmap = await GeneralHelper.GetAsLayer("original", features).AsGdiBitmapAsync(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

            originalBitmap.Save($"{outputDirectory}\\{fileName}-{zoomLevel}-original.png", System.Drawing.Imaging.ImageFormat.Png);

            var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var parameters = SingleFeatureVaryingThresholdExperiment.CreateParameters(pixelLength);

            foreach (var lrModel in lrModels)
            {
                stopwatch.Restart();

                var simplified = features.Select(f => f.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points)).Where(f => f.Type != GeometryType.Point).ToList();

                stopwatch.Stop();

                await GeneralHelper.CreateImages(boundingBox, zoomLevel, originalBitmap, GeneralHelper.GetAsLayer($"S-{zoomLevel}-{lrModel.Title}", simplified), outputDirectory, fileName, lrModel.Title);

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, zoomLevel, lrModel.Title, stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());
            }

            foreach (var method in methods)
            {
                stopwatch.Restart();

                var simplified = features.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

                stopwatch.Stop();

                await GeneralHelper.CreateImages(boundingBox, zoomLevel, originalBitmap, GeneralHelper.GetAsLayer($"S-{zoomLevel}-{method}", simplified), outputDirectory, fileName, $"{method}");

                writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, features, simplified, zoomLevel, methodNames[method], stopwatch.ElapsedMilliseconds, 1, parameters).ToTsv());
            }
        }
    }
}

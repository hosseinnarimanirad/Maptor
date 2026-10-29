using System.IO;
using System.Globalization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Checks that the visual metric of the published LAS run (<see cref="LRHelper.PublishedRunFolder"/>) is reproduced
/// by <see cref="MetricRasterizer"/> with <see cref="MetricRenderOptions.Published"/>: for the 348 published features
/// and the four baselines at the three threshold coefficients, the simplified point count and the pixel confusion
/// counts are recomputed and compared row by row with the published <c>All-summary.txt</c>. The published driver
/// rendered the reference raster from all sampled features of the layer (neighbours inside the bounding box count
/// as false negatives); that is replicated here, and the counts against the feature alone are reported as well.
/// The LRS rows are not checked: their models came from the 2024 trainer (see the test PublishedCoefficientsTest).
/// </summary>
public static class PublishedMetricReproduction
{
    public static void Run(string outputFolder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var published = File.ReadLines(Path.Combine(LRHelper.PublishedRunFolder, "All-summary.txt"))
                            .Skip(1)
                            .Select(l => l.Split('\t'))
                            .Where(c => c.Length > 22)
                            .GroupBy(c => (File: c[0], Index: c[2], Method: c[7], Coef: double.Parse(c[4], CultureInfo.InvariantCulture)))
                            .ToDictionary(g => g.Key, g => g.First());

        var layers = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder);

        int rows = 0, pointsEqual = 0, countsEqual = 0, countsEqualSingle = 0;

        using var writer = new StreamWriter(Path.Combine(outputFolder, "Published-metric-reproduction.txt"));

        writer.WriteLine("layer\tfeature\tmethod\tcoef\tpoints (published / now)\tTP FP FN published\tTP FP FN now, all features\tTP FP FN now, feature alone");

        foreach (var (layer, list) in layers)
        {
            var all = list.Select(t => t.Feature).ToList();

            foreach (var (index, feature) in list)
            {
                var box = feature.GetBoundingBox();
                var zoom = WebMercatorUtility.EstimateZoomLevel(box, 128, 128);
                var size = WebMercatorUtility.ToScreenSize(zoom, box);
                var pixel = WebMercatorUtility.ToWebMercatorLength(zoom, 1);

                using var originalAll = MetricRasterizer.Render(all, box, size.Width, size.Height, MetricRenderOptions.Published);
                using var originalAlone = MetricRasterizer.Render(new List<Geometry<Point>> { feature }, box, size.Width, size.Height, MetricRenderOptions.Published);

                foreach (var method in SingleFeatureVaryingThresholdExperiment.Methods)
                {
                    foreach (var coef in SingleFeatureVaryingThresholdExperiment.ThresholdCoefficients)
                    {
                        if (!published.TryGetValue((layer, index.ToString(), method.GetDescription(), coef), out var row))
                            continue;

                        var simplified = feature.Simplify(method, SingleFeatureVaryingThresholdExperiment.CreateParameters(pixel * coef));

                        using var raster = MetricRasterizer.Render(new List<Geometry<Point>> { simplified }, box, size.Width, size.Height, MetricRenderOptions.Published);

                        var now = ImageHelper.CalculateConfusionMatrixBitmaps(originalAll, raster).confusionMatrix;
                        var alone = ImageHelper.CalculateConfusionMatrixBitmaps(originalAlone, raster).confusionMatrix;

                        var publishedCounts = $"{row[14]} {row[15]} {row[16]}";
                        var nowCounts = $"{now.TruePositive} {now.FalsePositive} {now.FalseNegative}";
                        var aloneCounts = $"{alone.TruePositive} {alone.FalsePositive} {alone.FalseNegative}";

                        rows++;
                        if (row[11] == simplified.TotalNumberOfPoints.ToString()) pointsEqual++;
                        if (publishedCounts == nowCounts) countsEqual++;
                        if (publishedCounts == aloneCounts) countsEqualSingle++;

                        writer.WriteLine($"{layer}\t{index}\t{method.GetDescription()}\t{coef}\t{row[11]} / {simplified.TotalNumberOfPoints}\t{publishedCounts}\t{nowCounts}\t{aloneCounts}");
                    }
                }
            }
        }

        File.WriteAllText(Path.Combine(outputFolder, "Published-metric-reproduction-summary.txt"),
            $"rendering: {MetricRenderOptions.Published}{Environment.NewLine}" +
            $"rows compared (348 features × 4 baselines × 3 coefficients present in the published log): {rows}{Environment.NewLine}" +
            $"simplified point count equal: {pointsEqual}{Environment.NewLine}" +
            $"TP/FP/FN equal, reference raster = all sampled features of the layer (published protocol): {countsEqual}{Environment.NewLine}" +
            $"TP/FP/FN equal, reference raster = the feature alone (corrected protocol): {countsEqualSingle}{Environment.NewLine}");
    }
}

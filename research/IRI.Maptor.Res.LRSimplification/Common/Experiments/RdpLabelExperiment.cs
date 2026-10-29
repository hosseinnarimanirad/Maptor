using System.IO;
using System.Text;
using System.Globalization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// E1 of the resubmission: why the training labels are not taken from an existing algorithm. Real OSM features
/// (a seeded sample of 50 per layer, disjoint from the 348 published features) are simplified with RDP at a 1-px
/// threshold at the zoom level where they fit a 128² screen; every vertex becomes a triad in screen pixels
/// (<see cref="LRSimplificationTrainingData{T}.Create"/>: last retained vertex, vertex, next vertex) labelled
/// retained when RDP kept it. The same logistic regressions as LRSv7/LRSv4 are trained on these labels and then
/// run, through the same traversal, on the 348 published features next to the models trained on the synthetic
/// labels and the baselines.
/// Writes <c>E1-RdpLabels.txt</c> (training set, label conflicts, cross-validation, coefficients) and the usual
/// <c>All-summary.txt</c> with per-feature images.
/// </summary>
public static class RdpLabelExperiment
{
    public const int TrainingSampleSeed = 1404;

    public const int TrainingSampleSize = 50;

    /// <summary>Parts whose RDP result could not be matched vertex by vertex (reported in the output).</summary>
    public static int SkippedParts { get; private set; }

    public static async Task RunAsync(string outputFolder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var report = new StringBuilder();

        report.AppendLine($"# E1: labels from RDP (1 px at the 128² zoom level), {DateTime.Now:yyyy-MM-dd HH:mm}");
        report.AppendLine($"# training sample: {TrainingSampleSize} features per layer, 30–2000 points, seed {TrainingSampleSeed}, disjoint from the published features");
        report.AppendLine();

        var published = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder);

        var data7 = new List<LRSimplificationParameters<Point>>();
        var data4 = new List<LRSimplificationParameters<Point>>();

        report.AppendLine("layer\tfeatures\ttriads\tretained by RDP");

        foreach (var (layer, features) in SampleTrainingFeatures(published))
        {
            int before = data7.Count;

            foreach (var feature in features)
            {
                data7.AddRange(Label(feature, ModelSelectionExperiment.SevenFeatures));
                data4.AddRange(Label(feature, ModelSelectionExperiment.FourFeatures));
            }

            report.AppendLine($"{layer}\t{features.Count}\t{data7.Count - before}\t{data7.Skip(before).Count(r => r.IsRetained)}");
        }

        report.AppendLine($"all\t\t{data7.Count}\t{data7.Count(r => r.IsRetained)} ({data7.Count(r => r.IsRetained) / (double)data7.Count:P1})");
        report.AppendLine($"parts skipped because RDP's vertices could not be matched to the original (counted once per feature set): {SkippedParts}");
        report.AppendLine();

        var training7 = new LRSimplificationTrainingData<Point>(data7);
        var training4 = new LRSimplificationTrainingData<Point>(data4);

        var synthetic7 = SyntheticDataFactory.CreateTrainingData(ModelSelectionExperiment.SevenFeatures);

        // conflicts: the same triad (features rounded) with both labels
        report.AppendLine("## Label conflicts (seven features rounded to the given number of decimals; pixels, square pixels, cosines)");
        report.AppendLine("decimals\tRDP labels on OSM triads\tsynthetic labels");

        foreach (var decimals in new[] { 1, 0 })
        {
            var rdp = ModelEvaluation.FindLabelConflicts(training7.Records.Select(r => (IReadOnlyList<double>)r.FeatureValues).ToList(), training7.Records.Select(r => r.IsRetained).ToList(), decimals);
            var syn = ModelEvaluation.FindLabelConflicts(synthetic7.Records.Select(r => (IReadOnlyList<double>)r.FeatureValues).ToList(), synthetic7.Records.Select(r => r.IsRetained).ToList(), decimals);

            report.AppendLine($"{decimals}\t{rdp}\t{syn}");
        }

        report.AppendLine();

        // how well a triad classifier can reproduce RDP's decisions
        report.AppendLine($"## Stratified {ModelSelectionExperiment.Folds}-fold cross-validation, λ = 1 (seed {ModelSelectionExperiment.Seed})");
        report.AppendLine("model\tCV accuracy\tsd\tpooled precision\tpooled recall\tpooled F1\talways 'retain'");

        foreach (var (name, data) in new[] { ("LR7 on RDP labels", training7), ("LR4 on RDP labels", training4), ("LR7 on synthetic labels", synthetic7) })
        {
            var x = data.ToMatrix(out var labels);

            var cv = ModelEvaluation.CrossValidate(() => new LogisticRegression(LogisticSimplification<Point>.CreateDefaultRegressionOptions()), x, labels, ModelSelectionExperiment.Folds, ModelSelectionExperiment.Seed);

            var pooled = cv.Pooled;

            report.AppendLine($"{name}\t{cv.MeanAccuracy:0.0000}\t{cv.StandardDeviationOfAccuracy:0.0000}\t{pooled.Precision:0.0000}\t{pooled.Recall:0.0000}\t{pooled.F1:0.0000}\t{labels.Count(l => l) / (double)labels.Length:0.0000}");
        }

        report.AppendLine();

        var lr7Rdp = LogisticSimplification<Point>.Create(training7, "LR7-RDP");
        var lr4Rdp = LogisticSimplification<Point>.Create(training4, "LR4-RDP");

        var models = LRHelper.GetLRAModels();
        models.Add(lr7Rdp);
        models.Add(lr4Rdp);

        report.AppendLine("## Coefficients (z-scored features, intercept first)");

        foreach (var model in models)
        {
            report.AppendLine($"{model.Title}\t{string.Join("\t", ((LogisticRegression)model.Classifier).Beta.Select(b => b.ToString("0.000")))}");
        }

        File.WriteAllText(Path.Combine(outputFolder, "E1-RdpLabels.txt"), report.ToString());

        LRHelper.WriteModelDescriptions(Path.Combine(outputFolder, "Models.txt"), models);

        using (var writer = new StreamWriter(Path.Combine(outputFolder, "All-summary.txt"), false))
        {
            writer.WriteLine(VisualSimplificationLog.GetHeader());

            foreach (var (layer, features) in published)
            {
                await SingleFeatureVaryingThresholdExperiment.RunAsync(layer, features, writer, outputFolder, models, 128, SingleFeatureVaryingThresholdExperiment.ThresholdCoefficients);

                writer.Flush();
            }
        }

        ExperimentStatistics.WriteAll(outputFolder, models.Select(m => m.Title).ToList(), SingleFeatureVaryingThresholdExperiment.Methods.Select(m => m.GetDescription()).ToList());
    }

    /// <summary>
    /// The training sample of E1 (also used by E2): per layer of the published run, <see cref="TrainingSampleSize"/>
    /// features with 30–2000 points drawn with seed <see cref="TrainingSampleSeed"/>, none of them a published feature.
    /// </summary>
    public static List<(string Layer, List<Geometry<Point>> Features)> SampleTrainingFeatures(SortedDictionary<string, List<(int Index, Geometry<Point> Feature)>>? published = null)
    {
        published ??= PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder);

        var publishedBoxes = published.Values.SelectMany(l => l).Select(t => t.Feature.GetBoundingBox()).ToList();

        var result = new List<(string, List<Geometry<Point>>)>();

        foreach (var layer in published.Keys)
        {
            var shp = Path.Combine(LRHelper.DataFolder, layer + ".shp");

            var features = ExperimentData.LoadFeatures(shp, minPoints: 30, maxPoints: 2000, sampleSize: TrainingSampleSize + 10, seed: TrainingSampleSeed)
                                         .Where(f => !publishedBoxes.Any(b => SameBox(b, f.GetBoundingBox())))
                                         .Take(TrainingSampleSize)
                                         .ToList();

            result.Add((layer, features));
        }

        return result;
    }

    /// <summary>
    /// Triads of every part of <paramref name="feature"/> in screen pixels, labelled by RDP at 1 px. Parts are
    /// simplified separately, as <see cref="GeometryExtensions.Simplify{T}(Geometry{T}, LogisticSimplification{T}, Func{T, T}, bool)"/> does.
    /// </summary>
    public static List<LRSimplificationParameters<Point>> Label(Geometry<Point> feature, List<LRSimplificationFeatures> features)
    {
        var box = feature.GetBoundingBox();
        var zoom = WebMercatorUtility.EstimateZoomLevel(box, 128, 128);
        var size = WebMercatorUtility.ToScreenSize(zoom, box);
        var pixel = WebMercatorUtility.ToWebMercatorLength(zoom, 1);
        var toScreen = Utility.CreateMapToScreenMapFunc(box, size.Width, size.Height);
        var parameters = SingleFeatureVaryingThresholdExperiment.CreateParameters(pixel);

        // the rings of a polygon are simplified as rings (RDP divides them at the vertex farthest from the first one) and
        // labelled in ring mode (the last stored vertex has the first one as its next neighbour); since 2026-10-06
        var isRing = feature.IsRingBase();

        parameters.IsRing = isRing;

        var result = new List<LRSimplificationParameters<Point>>();

        foreach (var part in Leaves(feature))
        {
            if (part.Count < 3)
                continue;

            var simplified = Simplifications.SimplifyByRamerDouglasPeucker(part, parameters);

            if (simplified is null || simplified.Count < 2)
                continue;

            try
            {
                result.AddRange(LRSimplificationTrainingData<Point>.Create(part, simplified, isRing, features, toScreen).Records);
            }
            catch (NotImplementedException)
            {
                // a simplified vertex could not be matched to the original part (repeated coordinates); skip the part
                SkippedParts++;
            }
        }

        return result;
    }

    private static IEnumerable<List<Point>> Leaves(Geometry<Point> geometry)
    {
        if (geometry.Points is not null)
        {
            yield return geometry.Points;

            yield break;
        }

        foreach (var part in geometry.Geometries ?? new List<Geometry<Point>>())
        {
            foreach (var leaf in Leaves(part))
                yield return leaf;
        }
    }

    private static bool SameBox(BoundingBox a, BoundingBox b)
        => Math.Abs(a.XMin - b.XMin) < 0.05 && Math.Abs(a.YMin - b.YMin) < 0.05 && Math.Abs(a.XMax - b.XMax) < 0.05 && Math.Abs(a.YMax - b.YMax) < 0.05;
}

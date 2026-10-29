using System.IO;
using System.Text;
using System.Globalization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;
using static IRI.Maptor.Res.LRSimplification.Common.SingleFeatureVaryingThresholdExperiment;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Step 1b of the resubmission (2026-10-06): which training labels for LRSv4, chosen on a validation sample that shares
/// no feature with the test sets (the 348 published features and the 124 thin-part polygons of E12).
/// <para>
/// Validation sample: the published protocol (<see cref="ExperimentData.LoadFeatures"/>: multi-part features split,
/// 30 &lt; points &lt; 2000, consecutive duplicates removed) on the same seven layers, minus every feature whose
/// bounding box matches a test feature, then <see cref="PerLayer"/> features per layer drawn with <see cref="Seed"/>.
/// Written once to <see cref="ValidationFolder"/> as <c>{layer}-F{i}.json</c> (WGS84, as the published features)
/// with <c>selection.tsv</c>; later runs read the files.
/// </para>
/// <para>
/// Candidates: LRSv4 (the four published features) trained on the V23 table, on V23 with the boundary moved by one
/// grid cell (wide/narrow, as E3), and on the oracle rules any-k2, hd-1 and hd-1.5 (all rotations and offsets of the
/// oracle grid, as E9). Baselines RDP, RW, NOPW, BOPW at every threshold of
/// <see cref="ResubmissionExperiments.VisualLimitThresholdsPx"/> (no early stop), screens 128², 64², 32², 16².
/// The selection rule is fixed before the run and written to <c>protocol.txt</c>.
/// </para>
/// </summary>
public static class LabelChoiceExperiment
{
    public const string ValidationFolder = @"E:\University.Ph.D\4. Paper2\DataWm\Validation-2026-10-06";

    public const int Seed = 2027;

    public const int PerLayer = 30;

    public static readonly int[] ScreenSizes = { 128, 64, 32, 16 };

    public const string SelectionRule =
        "Primary criterion 'loose': a result is visually equivalent when no pixel of either raster is farther than 2 px from the other (FractionBeyond2 = 0);\r\n" +
        "'strict' (ExcessDisplacement = 0) is reported alongside.\r\n" +
        "Per model and screen: R = share of equivalent features, C = median compression over the features.\r\n" +
        "Per baseline and screen: for every threshold the share of equivalent features and the median compression; this curve interpolated at R gives the\r\n" +
        "baseline's compression at the model's reliability; gap = C - the best baseline's compression.\r\n" +
        "Eligible: R >= 0.85 under the loose criterion at every screen. Chosen: the eligible candidate with the largest mean gap over the four screens;\r\n" +
        "a candidate replaces V23 (the published labels) only if its mean gap is larger by at least 0.01.\r\n";

    public static async Task RunAsync(string folder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Directory.CreateDirectory(folder);

        if (!Directory.Exists(ValidationFolder) || !Directory.EnumerateFiles(ValidationFolder, "*.json").Any())
            await BuildValidationSampleAsync();

        var models = Candidates();

        LRHelper.WriteModelDescriptions(Path.Combine(folder, "Models.txt"), models);

        var baselines = StandardBaselines(RevisionThresholdCoefficients);

        File.WriteAllText(Path.Combine(folder, "protocol.txt"),
            $"features: {ValidationFolder}\r\nscreens: {string.Join(", ", ScreenSizes)}\r\n" +
            $"thresholds (px): {string.Join(", ", ResubmissionExperiments.VisualLimitThresholdsPx)} (all run)\r\n" +
            $"baselines: {string.Join(", ", baselines.Select(b => b.Name))}\r\nmodels: {string.Join(", ", models.Select(m => m.Title))}\r\n\r\n" + SelectionRule);

        var layers = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(ValidationFolder);

        foreach (var size in ScreenSizes)
        {
            var sub = Path.Combine(folder, $"Screen{size}");

            Directory.CreateDirectory(sub);

            using (var writer = new StreamWriter(Path.Combine(sub, "All-summary.txt"), false))
            {
                writer.WriteLine(VisualSimplificationLog.GetHeader());

                foreach (var (layer, features) in layers)
                {
                    await RunVisualLimitAsync(layer, features, writer, models, size, baselines, ResubmissionExperiments.VisualLimitThresholdsPx, int.MaxValue, ResubmissionExperiments.MetricMarginPixels);

                    writer.Flush();
                }
            }

            ExperimentStatistics.WriteAll(sub, models.Select(m => m.Title).ToList(), baselines.Select(b => b.Name).ToList());
        }
    }

    /// <summary>LRSv4 on each label set.</summary>
    public static List<LogisticSimplification<Point>> Candidates()
    {
        var models = LRHelper.GetLRAModels().Where(m => m.Title == "LR4v23").ToList();

        models.Add(LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(ModelSelectionExperiment.FourFeatures, SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, +1)), "LR4-wide"));
        models.Add(LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(ModelSelectionExperiment.FourFeatures, SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, -1)), "LR4-narrow"));

        var oracle = OracleSyntheticDataFile.Load(LRHelper.OracleFile)
                     ?? throw new FileNotFoundException("oracle-labelled triads not found; run --label-oracle first", LRHelper.OracleFile);

        foreach (var ruleName in new[] { "any-k2", "hd-1", "hd-1.5" })
        {
            var rule = OracleLabeller.LabelRules.Single(r => r.Name == ruleName);

            models.Add(LogisticSimplification<Point>.Create(oracle.ToTrainingData(rule.IsRetained, ModelSelectionExperiment.FourFeatures), $"LR4-{ruleName.Replace("-", "")}"));
        }

        return models;
    }

    /// <summary>Draws the validation sample and writes it to <see cref="ValidationFolder"/>.</summary>
    public static async Task BuildValidationSampleAsync()
    {
        Directory.CreateDirectory(ValidationFolder);

        // test features (both test sets), identified by their bounding box rounded to 1 m (they were stored in WGS84)
        var test = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder).Values
                   .Concat(PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(ResubmissionExperiments.ThinPartsFolder).Values)
                   .SelectMany(list => list.Select(f => Key(f.Feature)))
                   .ToHashSet();

        var selection = new StringBuilder("Layer\tIndex\tPoints\tCandidates\tExcludedAsTest\tMinX\tMinY\tMaxX\tMaxY\r\n");

        foreach (var shp in Directory.EnumerateFiles(LRHelper.DataFolder, "*_WM.shp").OrderBy(f => f, StringComparer.Ordinal))
        {
            var layer = Path.GetFileNameWithoutExtension(shp);

            var all = ExperimentData.LoadFeatures(shp, 30, 2000);
            var candidates = all.Where(f => !test.Contains(Key(f))).ToList();

            var random = new Random(Seed);
            var chosen = candidates.OrderBy(_ => random.Next()).Take(PerLayer).ToList();

            for (int i = 0; i < chosen.Count; i++)
            {
                var feature = chosen[i];
                var box = feature.GetBoundingBox();

                await feature.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(ValidationFolder, $"{layer}-F{i + 1}.json"), false, true);

                selection.Append($"{layer}\t{i + 1}\t{feature.TotalNumberOfPoints}\t{candidates.Count}\t{all.Count - candidates.Count}\t{box.XMin:F2}\t{box.YMin:F2}\t{box.XMax:F2}\t{box.YMax:F2}\r\n");
            }
        }

        File.WriteAllText(Path.Combine(ValidationFolder, "selection.tsv"), selection.ToString());
    }

    private static (long, long, long, long) Key(Geometry<Point> feature)
    {
        var box = feature.GetBoundingBox();

        return ((long)Math.Round(box.XMin), (long)Math.Round(box.YMin), (long)Math.Round(box.XMax), (long)Math.Round(box.YMax));
    }
}

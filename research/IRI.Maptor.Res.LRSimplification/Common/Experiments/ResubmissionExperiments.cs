using System.IO;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.Drawing.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;

using Point = IRI.Maptor.Core.Common.Primitives.Point;
using static IRI.Maptor.Res.LRSimplification.Common.SingleFeatureVaryingThresholdExperiment;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// The experiments of the resubmission (phase 2, 2026-10), all on the 348 features of the published run with the
/// published metric rendering unless stated otherwise:
/// <list type="bullet">
/// <item>E2 <see cref="OracleOnRealTriadsAsync"/>: the rasterization oracle on triads of real OSM features.</item>
/// <item>E3 <see cref="BoundarySensitivityAsync"/>: models retrained after moving the label boundary by ±0.5 px.</item>
/// <item>E4 + E7 <see cref="MainComparisonAsync"/>: models, the hand-written screen-space rule and eight online/offline
///       baselines (RDP, RW, NOPW, BOPW, Sleeve-fitting, Lang, CTR, every n-th point) on a dense threshold grid, for
///       the comparison at matched compression.</item>
/// <item>E5 <see cref="TraversalAblationAsync"/>: the published power-of-two traversal against a full scan, the previous
///       vertex only and the midpoint only, with the same classifier.</item>
/// <item>E6 <see cref="Timing"/>: simplification time against the number of points, real features and a worst case.</item>
/// <item>E8 <see cref="ScreenSizesAsync"/>: the comparison on 128², 256² and 512² screens.</item>
/// </list>
/// </summary>
public static class ResubmissionExperiments
{
    public const int Seed = 2026;

    /// <summary>Every n-th point at these n; the baselines' coefficient column holds n.</summary>
    public static readonly double[] NthPointCoefficients = { 2, 3, 4, 5, 6, 8, 10, 12 };

    /// <summary>Look-ahead of the Lang algorithm (vertices): fixed, so that it stays a bounded-window online method.</summary>
    public const int LangLookAhead = 8;

    /// <summary>
    /// The CTR variant run as the "CTR" baseline: <see cref="SimplificationType.CumulativeTriangleRoutine"/> (default, the variant
    /// behind the published ISPRS and LRS-paper numbers) or <see cref="SimplificationType.CumulativeTriangleRoutine2"/> (the ISPRS
    /// pseudo-code; set by the E16Ctr2 experiments for the thesis, author's decision 2026-10-26).
    /// </summary>
    public static SimplificationType CtrType { get; set; } = SimplificationType.CumulativeTriangleRoutine;

    public static List<Baseline> ExtendedBaselines()
    {
        var coefs = RevisionThresholdCoefficients;

        var result = StandardBaselines(coefs);

        result.Add(new Baseline("SF", SimplificationType.SleeveFitting, coefs, (px, c) => CreateParameters(px * c)));
        result.Add(new Baseline("LA", SimplificationType.Lang, coefs, (px, c) => { var p = CreateParameters(px * c); p.LookAhead = LangLookAhead; return p; }));
        result.Add(new Baseline("CTR", CtrType, coefs, (px, c) => CreateParameters(px * c)));
        result.Add(new Baseline("NTH", SimplificationType.NthPoint, NthPointCoefficients, (px, n) => new SimplificationParameters() { N = (uint)n, Retain3Points = LRHelper.Retain3Points }));

        return result;
    }

    // ---------------------------------------------------------------------------------------------------------
    // the comparison runs
    // ---------------------------------------------------------------------------------------------------------

    private static async Task RunOnPublishedFeaturesAsync(string folder, List<LogisticSimplification<Point>> models, List<Baseline> baselines, int screenSize, bool saveFiles)
    {
        Directory.CreateDirectory(folder);

        LRHelper.WriteModelDescriptions(Path.Combine(folder, "Models.txt"), models);

        var layers = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder);

        using (var writer = new StreamWriter(Path.Combine(folder, "All-summary.txt"), false))
        {
            writer.WriteLine(VisualSimplificationLog.GetHeader());

            foreach (var (layer, features) in layers)
            {
                await RunAsync(layer, features, writer, folder, models, screenSize, baselines, saveFiles);

                writer.Flush();
            }
        }

        ExperimentStatistics.WriteAll(folder, models.Select(m => m.Title).ToList(), baselines.Select(b => b.Name).ToList());
    }

    /// <summary>E4 + E7: LR7v23, LR4v23 and the hand rule against the extended baselines, 128² screen.</summary>
    public static Task MainComparisonAsync(string folder)
    {
        var models = LRHelper.GetLRAModels();

        models.Add(LogisticSimplification<Point>.FromFitted(LRHelper.CreateHandRule(), LRHelper.PixelFeatures, "Rule"));

        return RunOnPublishedFeaturesAsync(folder, models, ExtendedBaselines(), 128, saveFiles: false);
    }

    /// <summary>The oracle label rules E9 trains on: displacement-based, with the pixel-count rule closest to the published table for comparison.</summary>
    public static readonly string[] DisplacementLabelRules = { "hd-1", "hd-1.5", "hd-2", "any-k2" };

    /// <summary>
    /// E9: the published LR7v23/LR4v23 next to LR7, LR4 and a depth-5 tree (pixel features) trained on each rule of
    /// <see cref="DisplacementLabelRules"/> over all rotations and offsets of the oracle grid, against the baselines
    /// (without SF, which duplicates BOPW), 128² screen; the statistics include the displacement measures.
    /// </summary>
    public static async Task DisplacementLabelsAsync(string folder)
    {
        Directory.CreateDirectory(folder);

        var oracle = OracleSyntheticDataFile.Load(LRHelper.OracleFile)
                     ?? throw new FileNotFoundException("oracle-labelled triads not found; run --label-oracle first", LRHelper.OracleFile);

        if (oracle.Triads.All(t => t.MaxDisplacement == 0))
            throw new InvalidOperationException("the oracle file holds no displacement; re-run --label-oracle");

        var report = new StringBuilder();
        report.AppendLine("# E9: oracle labels by displacement (retain when some drawn pixel moves farther than d) and by pixel count");
        report.AppendLine("rule\tremoved triads (all rotations/offsets)\tremoved fraction\tremoved unrotated cells (of 4,182)");

        var models = LRHelper.GetLRAModels();

        foreach (var ruleName in DisplacementLabelRules)
        {
            var rule = OracleLabeller.LabelRules.Single(r => r.Name == ruleName);

            var removed = oracle.Triads.Count(t => !rule.IsRetained(t));
            var removedCells = oracle.Triads.Count(t => t.RotationDegrees == 0 && t.OffsetX == 0 && t.OffsetY == 0 && !rule.IsRetained(t));

            report.AppendLine($"{ruleName}\t{removed}\t{(double)removed / oracle.Triads.Count:0.000}\t{removedCells}");

            var suffix = ruleName.Replace("-", "");

            models.Add(LogisticSimplification<Point>.Create(oracle.ToTrainingData(rule.IsRetained, ModelSelectionExperiment.SevenFeatures), $"LR7-{suffix}"));
            models.Add(LogisticSimplification<Point>.Create(oracle.ToTrainingData(rule.IsRetained, ModelSelectionExperiment.FourFeatures), $"LR4-{suffix}"));
            models.Add(LogisticSimplification<Point>.Create(oracle.ToTrainingData(rule.IsRetained, LRHelper.PixelFeatures), $"Tree5-{suffix}", new DecisionTree(new DecisionTreeOptions() { MaxDepth = LRHelper.OracleTreeDepth })));
        }

        File.WriteAllText(Path.Combine(folder, "E9-labels.txt"), report.ToString());

        await RunOnPublishedFeaturesAsync(folder, models, ExtendedBaselines().Where(b => b.Name != "SF").ToList(), 128, saveFiles: false);
    }

    /// <summary>E3: LR7 and LR4 on the V23 labels and on V23 with the boundary moved outwards (wide) and inwards (narrow).</summary>
    public static async Task BoundarySensitivityAsync(string folder)
    {
        Directory.CreateDirectory(folder);

        var report = new StringBuilder();
        report.AppendLine("# E3: label boundary moved by one grid cell (0.5 px), SyntheticDataFactory.ShiftBoundary");
        report.AppendLine("labels\tremoved triads (of 4,182)");

        var variants = new (string Suffix, Func<double, Predicate<(double dx1, double dx2, double dy)>> Rule)[]
        {
            ("v23", b => SyntheticDataFactory.GetRule(b, SyntheticLabelRules.V23)),
            ("wide", SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, +1)),
            ("narrow", SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, -1)),
        };

        var models = new List<LogisticSimplification<Point>>();

        foreach (var (suffix, rule) in variants)
        {
            var data7 = SyntheticDataFactory.CreateTrainingData(ModelSelectionExperiment.SevenFeatures, rule);
            var data4 = SyntheticDataFactory.CreateTrainingData(ModelSelectionExperiment.FourFeatures, rule);

            report.AppendLine($"{suffix}\t{data7.Records.Count(r => !r.IsRetained)}");

            models.Add(LogisticSimplification<Point>.Create(data7, $"LR7-{suffix}"));
            models.Add(LogisticSimplification<Point>.Create(data4, $"LR4-{suffix}"));
        }

        File.WriteAllText(Path.Combine(folder, "E3-labels.txt"), report.ToString());

        await RunOnPublishedFeaturesAsync(folder, models, StandardBaselines(RevisionThresholdCoefficients).Where(b => b.Name == "RDP").ToList(), 128, saveFiles: false);
    }

    /// <summary>E5: the same fitted LR7v23 classifier under four traversals.</summary>
    public static Task TraversalAblationAsync(string folder)
    {
        // LRSv4 is the model of the resubmission (2026-10-06); the 2026-10-02 run used LR7v23
        var lr4 = LRHelper.GetLRAModels().Single(m => m.Title == "LR4v23");

        var models = new List<LogisticSimplification<Point>>
        {
            lr4.WithTraversal(LrsTraversal.PowerOfTwo, "LR4-PowerOfTwo"),
            lr4.WithTraversal(LrsTraversal.FullScan, "LR4-FullScan"),
            lr4.WithTraversal(LrsTraversal.PreviousOnly, "LR4-PreviousOnly"),
            lr4.WithTraversal(LrsTraversal.Midpoint, "LR4-Midpoint"),
        };

        return RunOnPublishedFeaturesAsync(folder, models, StandardBaselines(RevisionThresholdCoefficients).Where(b => b.Name == "RDP").ToList(), 128, saveFiles: false);
    }

    /// <summary>E8: LR7v23, LR4v23 and the four standard baselines on 128², 256² and 512² screens (one sub-folder each).</summary>
    public static async Task ScreenSizesAsync(string folder)
    {
        foreach (var size in new[] { 128, 256, 512 })
        {
            await RunOnPublishedFeaturesAsync(Path.Combine(folder, $"Screen{size}"), LRHelper.GetLRAModels(), StandardBaselines(RevisionThresholdCoefficients), size, saveFiles: false);
        }
    }

    /// <summary>The E12 features: polygons with thin parts selected from the full layers (`select_thin_parts.py` in the folder).</summary>
    public const string ThinPartsFolder = @"E:\University.Ph.D\4. Paper2\DataWm\ThinParts-2026-10-06";

    /// <summary>E12 thresholds: 0.125 · 2^(k/3) px, k = 0 … 15 (0.125 … 4 px), every one run (no early stop) so matched-compression curves are complete.</summary>
    public static readonly double[] ThinPartThresholdsPx = Enumerable.Range(0, 16).Select(k => Math.Round(0.125 * Math.Pow(2, k / 3.0), 4)).ToArray();

    /// <summary>
    /// E12: polygons with thin parts (share of area in parts narrower than 2 px &gt; 0.2 at the natural scale; 124 features,
    /// see <see cref="ThinPartsFolder"/>). Resubmission protocol (2026-10-06, as <see cref="CombinedScalesAsync"/>): LRSv4
    /// against RDP, RW, NOPW, BOPW at every threshold of <see cref="VisualLimitThresholdsPx"/>, screens
    /// <see cref="CombinedScreenSizes"/>. The 2026-10-05 run also had the hand rule, LR4-hd1, Lang, CTR and the
    /// segment-distance variants RDPs/BOPWs on 128–16 px with thresholds up to 4 px.
    /// </summary>
    public static async Task ThinPartsAsync(string folder)
    {
        await RunScalesAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(ThinPartsFolder), $"features: {ThinPartsFolder}", CombinedScreenSizes);
    }

    /// <summary>E12 on the 512² screen alone, run as a separate process next to <see cref="ThinPartsAsync"/>.</summary>
    public static async Task ThinParts512Async(string folder)
    {
        await RunScalesAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(ThinPartsFolder), $"features: {ThinPartsFolder}", new[] { 512 });
    }

    /// <summary>Empty pixels around every feature on the metric canvas of the resubmission runs (<see cref="MetricCanvas"/>).</summary>
    public const int MetricMarginPixels = 2;

    /// <summary>The screens of the resubmission runs: two finer than the natural 128² screen, three coarser.</summary>
    public static readonly int[] CombinedScreenSizes = { 256, 128, 64, 32, 16 };

    /// <summary>
    /// E13 (2026-10-06, replaces E7, E8, E10 and E11 for the resubmission): the 348 published features on screens of
    /// <see cref="CombinedScreenSizes"/> (each at the zoom level where it fits the screen), LRSv4 once, RDP, RW, NOPW and BOPW
    /// at every threshold of <see cref="VisualLimitThresholdsPx"/> without early stop, so that from one log both views
    /// follow: compression at equal visual reliability and visual error at matched compression, per scale.
    /// </summary>
    public static async Task CombinedScalesAsync(string folder)
    {
        await RunScalesAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder), $"features: {LRHelper.PublishedRunFolder}", CombinedScreenSizes);
    }

    /// <summary>E13 on the 512² screen alone (about 2 h), run as a separate process next to <see cref="CombinedScalesAsync"/>.</summary>
    public static async Task CombinedScales512Async(string folder)
    {
        await RunScalesAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder), $"features: {LRHelper.PublishedRunFolder}", new[] { 512 });
    }

    /// <summary>
    /// E14 (2026-10-07, diagnostic): the published LRSv4 traversal against <see cref="LrsTraversal.RestartAtRetained"/>
    /// (same classifier) on the 348 features, exact canvas, screens 128 and 16; no baselines (E13 holds them).
    /// </summary>
    public static Task TraversalRestartAsync(string folder) => TraversalRestartAsync(folder, LRHelper.PublishedRunFolder, new[] { 128, 16 });

    /// <summary>E14 on the thin-part polygons (<see cref="ThinPartsFolder"/>), screens 128, 32 and 16.</summary>
    public static Task TraversalRestartThinAsync(string folder) => TraversalRestartAsync(folder, ThinPartsFolder, new[] { 128, 32, 16 });

    private static async Task TraversalRestartAsync(string folder, string featureFolder, int[] screens)
    {
        Directory.CreateDirectory(folder);

        var lr4 = LRHelper.GetLRAModels().Single(m => m.Title == "LR4v23");

        var models = new List<LogisticSimplification<Point>> { lr4, lr4.WithTraversal(LrsTraversal.RestartAtRetained, "LR4-restart"), lr4.WithTraversal(LrsTraversal.RestartAtRetainedNoRewind, "LR4-restartNR") };

        LRHelper.WriteModelDescriptions(Path.Combine(folder, "Models.txt"), models);

        File.WriteAllText(Path.Combine(folder, "protocol.txt"), $"features: {featureFolder}\r\nscreens: {string.Join(", ", screens)}\r\nmodels only; baselines in E13 / E12\r\n");

        var layers = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(featureFolder);

        foreach (var size in screens)
        {
            var sub = Path.Combine(folder, $"Screen{size}");

            Directory.CreateDirectory(sub);

            using var writer = new StreamWriter(Path.Combine(sub, "All-summary.txt"), false);

            writer.WriteLine(VisualSimplificationLog.GetHeader());

            foreach (var (layer, features) in layers)
            {
                await RunVisualLimitAsync(layer, features, writer, models, size, new List<Baseline>(), VisualLimitThresholdsPx, int.MaxValue, MetricMarginPixels);

                writer.Flush();
            }
        }
    }

    /// <summary>The traversal of LRS in the resubmission (author's decision, 2026-10-07): restart at the retained vertex, no rewind.</summary>
    public const LrsTraversal ResubmissionTraversal = LrsTraversal.RestartAtRetainedNoRewind;

    /// <summary>LRS of the resubmission: LRSv4 (V23 labels) with <see cref="ResubmissionTraversal"/>, titled "LRS".</summary>
    public static LogisticSimplification<Point> ResubmissionLrs()
        => LRHelper.GetLRAModels().Single(m => m.Title == "LR4v23").WithTraversal(ResubmissionTraversal, "LRS");

    /// <summary>
    /// E15 (2026-10-07): the rows of every model-dependent result with the resubmission traversal, models only (the
    /// baselines do not depend on LRS and are taken from E13/E12 when merging; `AlgorithmRun\merge_lrs.py`):
    /// E13 (348 features, 512–16 px) and E12 (thin parts, 512–16 px) with LRS; the label choice (validation sample,
    /// 128–16 px) with the six label sets; E3 (label boundary ±0.5 px, 128 px); E5 (all traversals, 128 px).
    /// </summary>
    public static async Task ResubmissionLrsAsync(string folder)
    {
        Directory.CreateDirectory(folder);

        LogisticSimplification<Point> NoRewind(LogisticSimplification<Point> m, string title) => m.WithTraversal(ResubmissionTraversal, title);

        var lr4 = LRHelper.GetLRAModels().Single(m => m.Title == "LR4v23");
        var lrs = ResubmissionLrs();
        var published = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder);
        var all = new[] { 512 }.Concat(CombinedScreenSizes).ToArray();

        await ModelsOnlyAsync(Path.Combine(folder, "E13"), published, all, new() { lrs });
        await ModelsOnlyAsync(Path.Combine(folder, "E12"), PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(ThinPartsFolder), all, new() { lrs });

        var candidates = LabelChoiceExperiment.Candidates();
        await ModelsOnlyAsync(Path.Combine(folder, "LabelChoice"), PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LabelChoiceExperiment.ValidationFolder),
                              LabelChoiceExperiment.ScreenSizes, candidates.Select(m => NoRewind(m, m.Title)).ToList());

        await ModelsOnlyAsync(Path.Combine(folder, "E3"), published, new[] { 128 },
                              candidates.Where(m => m.Title is "LR4v23" or "LR4-wide" or "LR4-narrow").Select(m => NoRewind(m, m.Title.Replace("LR4v23", "LR4-v23"))).ToList());

        await ModelsOnlyAsync(Path.Combine(folder, "E5"), published, new[] { 128 }, new()
        {
            lr4.WithTraversal(LrsTraversal.PowerOfTwo, "LR4-PowerOfTwo"),
            lr4.WithTraversal(LrsTraversal.FullScan, "LR4-FullScan"),
            lr4.WithTraversal(LrsTraversal.PreviousOnly, "LR4-PreviousOnly"),
            lr4.WithTraversal(LrsTraversal.Midpoint, "LR4-Midpoint"),
            lr4.WithTraversal(LrsTraversal.RestartAtRetained, "LR4-RestartRewind"),
            lr4.WithTraversal(LrsTraversal.RestartAtRetainedNoRewind, "LR4-RestartNoRewind"),
        });
    }

    private static async Task ModelsOnlyAsync(string folder, SortedDictionary<string, List<(int Index, Geometry<Point> Feature)>> layers, int[] screens, List<LogisticSimplification<Point>> models)
    {
        Directory.CreateDirectory(folder);

        LRHelper.WriteModelDescriptions(Path.Combine(folder, "Models.txt"), models);

        File.WriteAllText(Path.Combine(folder, "protocol.txt"),
            $"features ({layers.Values.Sum(l => l.Count)})\r\nscreens: {string.Join(", ", screens)}\r\nmodels: {string.Join(", ", models.Select(m => $"{m.Title} [{m.Traversal}]"))}\r\nbaselines: none (models only)\r\n");

        foreach (var size in screens)
        {
            var sub = Path.Combine(folder, $"Screen{size}");

            Directory.CreateDirectory(sub);

            using var writer = new StreamWriter(Path.Combine(sub, "All-summary.txt"), false);

            writer.WriteLine(VisualSimplificationLog.GetHeader());

            foreach (var (layer, features) in layers)
            {
                await RunVisualLimitAsync(layer, features, writer, models, size, new List<Baseline>(), VisualLimitThresholdsPx, int.MaxValue, MetricMarginPixels);

                writer.Flush();
            }
        }
    }

    /// <summary>
    /// E16 (2026-10-09, exploratory, not in the manuscript): CTR (the published variant, <see cref="SimplificationType.CumulativeTriangleRoutine"/>)
    /// as an extra baseline under the E13 protocol: the 348 published features, screens 512 … 16, every threshold of
    /// <see cref="VisualLimitThresholdsPx"/> (area threshold = t² px², as in <see cref="ExtendedBaselines"/>), exact metric canvas;
    /// no models (the LRS rows are those of E13Final). Merged with E13Final by `AlgorithmRun\2026-10-09-E16Ctr\ctr_analysis.py`.
    /// </summary>
    public static Task CtrRandomFeaturesAsync(string folder)
        => CtrOnlyAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder), $"features: {LRHelper.PublishedRunFolder}");

    /// <summary>E16 on the 123 thin-part polygons (<see cref="ThinPartsFolder"/>), merged with E12Final.</summary>
    public static Task CtrThinPartsAsync(string folder)
        => CtrOnlyAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(ThinPartsFolder), $"features: {ThinPartsFolder}");

    private static async Task CtrOnlyAsync(string folder, SortedDictionary<string, List<(int Index, Geometry<Point> Feature)>> layers, string description)
    {
        Directory.CreateDirectory(folder);

        var screens = new[] { 512 }.Concat(CombinedScreenSizes).ToArray();
        var baselines = ExtendedBaselines().Where(b => b.Name == "CTR").ToList();

        File.WriteAllText(Path.Combine(folder, "protocol.txt"),
            $"{description} ({layers.Values.Sum(l => l.Count)})\r\nscreens: {string.Join(", ", screens)}\r\n" +
            $"thresholds (px): {string.Join(", ", VisualLimitThresholdsPx)} (all run; CTR area threshold = t^2 px^2)\r\n" +
            $"baselines: CTR ({CtrType})\r\nmodels: none (LRS rows from E13Final / E12Final)\r\n");

        foreach (var size in screens)
        {
            var sub = Path.Combine(folder, $"Screen{size}");

            Directory.CreateDirectory(sub);

            using var writer = new StreamWriter(Path.Combine(sub, "All-summary.txt"), false);

            writer.WriteLine(VisualSimplificationLog.GetHeader());

            foreach (var (layer, features) in layers)
            {
                await RunVisualLimitAsync(layer, features, writer, new List<LogisticSimplification<Point>>(), size, baselines, VisualLimitThresholdsPx, int.MaxValue, MetricMarginPixels);

                writer.Flush();
            }
        }
    }

    /// <summary>E13 on the 16² screen alone: re-run after the Retain3Points fix for a ring reduced to one vertex (2026-10-06).</summary>
    public static async Task CombinedScales16Async(string folder)
    {
        await RunScalesAsync(folder, PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder), $"features: {LRHelper.PublishedRunFolder}", new[] { 16 });
    }

    private static async Task RunScalesAsync(string folder, SortedDictionary<string, List<(int Index, Geometry<Point> Feature)>> layers, string description, int[] screenSizes)
    {
        Directory.CreateDirectory(folder);

        var models = LRHelper.GetLRAModels().Where(m => m.Title == "LR4v23").ToList();

        LRHelper.WriteModelDescriptions(Path.Combine(folder, "Models.txt"), models);

        var baselines = StandardBaselines(RevisionThresholdCoefficients);

        File.WriteAllText(Path.Combine(folder, "protocol.txt"),
            $"{description} ({layers.Values.Sum(l => l.Count)})\r\nscreens: {string.Join(", ", screenSizes)}\r\n" +
            $"thresholds (px): {string.Join(", ", VisualLimitThresholdsPx)} (all run)\r\n" +
            $"baselines: {string.Join(", ", baselines.Select(b => b.Name))}\r\nmodels: {string.Join(", ", models.Select(m => m.Title))}\r\n");

        foreach (var size in screenSizes)
        {
            var sub = Path.Combine(folder, $"Screen{size}");

            Directory.CreateDirectory(sub);

            using (var writer = new StreamWriter(Path.Combine(sub, "All-summary.txt"), false))
            {
                writer.WriteLine(VisualSimplificationLog.GetHeader());

                foreach (var (layer, features) in layers)
                {
                    await RunVisualLimitAsync(layer, features, writer, models, size, baselines, VisualLimitThresholdsPx, int.MaxValue, MetricMarginPixels);

                    writer.Flush();
                }
            }

            ExperimentStatistics.WriteAll(sub, models.Select(m => m.Title).ToList(), baselines.Select(b => b.Name).ToList());
        }
    }

    /// <summary>E11 thresholds: 0.125 · 2^(k/3) px, k = 0 … 18 (0.125 … 8 px, three steps per doubling).</summary>
    public static readonly double[] VisualLimitThresholdsPx = Enumerable.Range(0, 19).Select(k => Math.Round(0.125 * Math.Pow(2, k / 3.0), 4)).ToArray();

    /// <summary>
    /// E11: compression at the visual-equivalence limit, per scale. For every feature and screen (128², 64², 32², 16²) every
    /// baseline is swept over <see cref="VisualLimitThresholdsPx"/> until three consecutive results are visibly different;
    /// the models (published, hand rule, the oracle family any-k2 / hd-1 / hd-1.5 of E9) run once. The question is not
    /// which method compresses more, but which compresses more while the result still looks the same, at every scale.
    /// </summary>
    public static async Task VisualLimitAsync(string folder)
    {
        Directory.CreateDirectory(folder);

        var models = LRHelper.GetLRAModels();

        models.Add(LogisticSimplification<Point>.FromFitted(LRHelper.CreateHandRule(), LRHelper.PixelFeatures, "Rule"));

        var oracle = OracleSyntheticDataFile.Load(LRHelper.OracleFile);

        if (oracle is not null)
        {
            foreach (var ruleName in new[] { "any-k2", "hd-1", "hd-1.5" })
            {
                var rule = OracleLabeller.LabelRules.Single(r => r.Name == ruleName);

                models.Add(LogisticSimplification<Point>.Create(oracle.ToTrainingData(rule.IsRetained, ModelSelectionExperiment.SevenFeatures), $"LR7-{ruleName.Replace("-", "")}"));
            }
        }

        LRHelper.WriteModelDescriptions(Path.Combine(folder, "Models.txt"), models);

        var baselines = ExtendedBaselines().Where(b => b.Name is not ("SF" or "NTH")).ToList();

        File.WriteAllText(Path.Combine(folder, "E11-protocol.txt"),
            $"thresholds (px): {string.Join(", ", VisualLimitThresholdsPx)}\r\n" +
            "a baseline's sweep stops after 3 consecutive results with ExcessDisplacement > 0\r\n" +
            $"baselines: {string.Join(", ", baselines.Select(b => b.Name))}\r\nmodels: {string.Join(", ", models.Select(m => m.Title))}\r\n");

        var layers = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(LRHelper.PublishedRunFolder);

        foreach (var size in new[] { 128, 64, 32, 16 })
        {
            var sub = Path.Combine(folder, $"Screen{size}");

            Directory.CreateDirectory(sub);

            using var writer = new StreamWriter(Path.Combine(sub, "All-summary.txt"), false);

            writer.WriteLine(VisualSimplificationLog.GetHeader());

            foreach (var (layer, features) in layers)
            {
                await RunVisualLimitAsync(layer, features, writer, models, size, baselines, VisualLimitThresholdsPx, 3);

                writer.Flush();
            }
        }
    }

    /// <summary>
    /// E10: scale robustness towards coarse scales. The same 348 features at the zoom levels where each fits a 64², 32²
    /// and 16² screen (one, two and three zoom levels coarser than the natural scale of the published run), the published
    /// models, the hand rule and the baselines at their pixel thresholds, so that omitted parts and other visible
    /// errors at coarse scales are measured with the displacement columns. Rendering stays 1 px wide.
    /// </summary>
    public static async Task CoarseScreensAsync(string folder)
    {
        var models = LRHelper.GetLRAModels();

        models.Add(LogisticSimplification<Point>.FromFitted(LRHelper.CreateHandRule(), LRHelper.PixelFeatures, "Rule"));

        foreach (var size in new[] { 64, 32, 16 })
        {
            await RunOnPublishedFeaturesAsync(Path.Combine(folder, $"Screen{size}"), models, ExtendedBaselines().Where(b => b.Name is not ("SF" or "NTH")).ToList(), size, saveFiles: false);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // E6: timing
    // ---------------------------------------------------------------------------------------------------------

    // Run(points, toScreen, pixel, isRing): isRing marks a polygon ring (stored without its closing vertex)
    private record TimedMethod(string Name, Func<List<Point>, Func<Point, Point>, double, bool, List<Point>> Run, int MaxPoints);

    private static List<TimedMethod> TimedMethods()
    {
        // LRSv4 is the model of the resubmission (2026-10-06); earlier E6 runs timed LR7v23
        var lr4 = LRHelper.GetLRAModels().Single(m => m.Title == "LR4v23");
        var full = lr4.WithTraversal(LrsTraversal.FullScan, "LR4-FullScan");
        var restart = lr4.WithTraversal(LrsTraversal.RestartAtRetained, "LR4-restart");
        var restartNR = lr4.WithTraversal(LrsTraversal.RestartAtRetainedNoRewind, "LR4-restartNR");

        List<Point> Baseline(SimplificationType type, List<Point> points, double pixel, bool isRing)
        {
            var parameters = CreateParameters(pixel);

            parameters.IsRing = isRing;

            return Geometry<Point>.Create(points, GeometryType.LineString).Simplify(type, parameters).Points ?? new List<Point>();
        }

        return new List<TimedMethod>
        {
            new("LR4-PowerOfTwo", (p, s, px, ring) => lr4.Simplify(p, s, LRHelper.Retain3Points), int.MaxValue),
            new("LR4-FullScan", (p, s, px, ring) => full.Simplify(p, s, LRHelper.Retain3Points), 10_000),
            new("LR4-restart", (p, s, px, ring) => restart.Simplify(p, s, LRHelper.Retain3Points), int.MaxValue),
            new("LR4-restartNR", (p, s, px, ring) => restartNR.Simplify(p, s, LRHelper.Retain3Points), int.MaxValue),
            new("RW", (p, s, px, ring) => Baseline(SimplificationType.ReumannWitkam, p, px, ring), int.MaxValue),
            new("NOPW", (p, s, px, ring) => Baseline(SimplificationType.NormalOpeningWindow, p, px, ring), 20_000),
            new("BOPW", (p, s, px, ring) => Baseline(SimplificationType.BeforeOpeningWindow, p, px, ring), 20_000),
            new("RDP", (p, s, px, ring) => Baseline(SimplificationType.RamerDouglasPeucker, p, px, ring), int.MaxValue),
        };
    }

    // median of `repeats` runs after one warm-up, in milliseconds
    private static (double Ms, int Kept) Measure(Func<List<Point>> run, int repeats)
    {
        var kept = run().Count;

        var times = new List<double>();

        var watch = new Stopwatch();

        for (int i = 0; i < repeats; i++)
        {
            watch.Restart();
            run();
            watch.Stop();

            times.Add(watch.Elapsed.TotalMilliseconds);
        }

        times.Sort();

        return (times[times.Count / 2], kept);
    }

    /// <summary>
    /// E6: single-threaded simplification time (median of 5 runs after a warm-up, rendering excluded).
    /// Real: the leaves (lines and rings) of all features of every layer with at least 100 points, stratified by
    /// log10(n) (up to 15 per 0.25-wide bin and layer), each at the zoom level where its feature fits a 128² screen.
    /// Worst case: a straight line with 0.1-px jitter and 0.2-px spacing, which no method simplifies early, in pixel
    /// units, n from 1,000 to 200,000. Writes the rows and the log–log slopes.
    /// </summary>
    public static void Timing(string folder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Directory.CreateDirectory(folder);

        var methods = TimedMethods();

        var rows = new List<(string Set, string Layer, int N, string Method, double Ms, int Kept)>();

        // real features
        var random = new Random(Seed);

        foreach (var shp in Directory.EnumerateFiles(LRHelper.DataFolder, "*.shp"))
        {
            var layer = Path.GetFileNameWithoutExtension(shp);

            var features = ExperimentData.LoadFeatures(shp, minPoints: 99, maxPoints: int.MaxValue, splitMultiPart: false, excludeGeometryCollections: true);

            var leaves = features.SelectMany(f => Leaves(f).Select(points => (Feature: f, Points: points)))
                                 .Where(l => l.Points.Count >= 100)
                                 .GroupBy(l => Math.Floor(Math.Log10(l.Points.Count) * 4))
                                 .SelectMany(g => g.OrderBy(_ => random.Next()).Take(15))
                                 .ToList();

            foreach (var (feature, points) in leaves)
            {
                var box = feature.GetBoundingBox();
                var zoom = WebMercatorUtility.EstimateZoomLevel(box, 128, 128);
                var size = WebMercatorUtility.ToScreenSize(zoom, box);
                var pixel = WebMercatorUtility.ToWebMercatorLength(zoom, 1);
                var toScreen = Utility.CreateMapToScreenMapFunc(box, size.Width, size.Height);

                foreach (var method in methods.Where(m => points.Count <= m.MaxPoints))
                {
                    var (ms, kept) = Measure(() => method.Run(points, toScreen, pixel, feature.IsRingBase()), 5);

                    rows.Add(("real", layer, points.Count, method.Name, ms, kept));
                }
            }
        }

        // worst case
        var jitter = new Random(Seed);

        foreach (var n in new[] { 1_000, 2_000, 5_000, 10_000, 20_000, 50_000, 100_000, 200_000 })
        {
            var points = Enumerable.Range(0, n).Select(i => new Point(i * 0.2, (jitter.NextDouble() - 0.5) * 0.2)).ToList();

            foreach (var method in methods.Where(m => n <= m.MaxPoints))
            {
                var (ms, kept) = Measure(() => method.Run(points, p => p, 1, false), n >= 10_000 ? 3 : 5);

                rows.Add(("straight", "synthetic", n, method.Name, ms, kept));
            }
        }

        using (var writer = new StreamWriter(Path.Combine(folder, "Timing.tsv")))
        {
            writer.WriteLine("Set\tLayer\tN\tMethod\tMilliseconds\tKept");

            foreach (var r in rows)
                writer.WriteLine($"{r.Set}\t{r.Layer}\t{r.N}\t{r.Method}\t{r.Ms:0.####}\t{r.Kept}");
        }

        var summary = new StringBuilder();
#if DEBUG
        const string build = "Debug (unoptimized JIT: absolute times are not comparable; run with -c Release)";
#else
        const string build = "Release";
#endif
        summary.AppendLine($"# E6: build {build}; single thread, median of 5 runs after a warm-up, simplification only");
        summary.AppendLine("# log10(ms) = a + b·log10(n), least squares; O(n) gives b ≈ 1, O(n log n) slightly above 1, O(n²) b ≈ 2");
        summary.AppendLine("set\tmethod\tfeatures\tn range\tslope b\tmedian ms per 1,000 points");

        foreach (var group in rows.Where(r => r.Ms > 0).GroupBy(r => (r.Set, r.Method)))
        {
            var x = group.Select(r => Math.Log10(r.N)).ToArray();
            var y = group.Select(r => Math.Log10(r.Ms)).ToArray();
            var mx = x.Average();
            var my = y.Average();
            var slope = x.Zip(y, (a, b) => (a - mx) * (b - my)).Sum() / x.Sum(a => (a - mx) * (a - mx));
            var perThousand = group.Select(r => r.Ms / r.N * 1000).OrderBy(v => v).ElementAt(group.Count() / 2);

            summary.AppendLine($"{group.Key.Set}\t{group.Key.Method}\t{group.Count()}\t{group.Min(r => r.N)}–{group.Max(r => r.N)}\t{slope:0.00}\t{perThousand:0.0000}");
        }

        File.WriteAllText(Path.Combine(folder, "Timing-summary.txt"), summary.ToString());
    }

    // ---------------------------------------------------------------------------------------------------------
    // E2: oracle on real triads
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// E2: consecutive triads (v[i−1], v[i], v[i+1]) of the E1 training sample (350 OSM features, none of the published
    /// ones), at the zoom level where each feature fits a 128² screen, are drawn three-point and two-point with the
    /// metric rendering at their true sub-pixel position; a triad is "invisible" when at most k pixels differ. The
    /// decisions of LR7v23, LR4v23 and the hand rule on the same triads are compared with the oracle.
    /// Triads whose bounding box exceeds 200 px are skipped.
    /// </summary>
    public static Task OracleOnRealTriadsAsync(string folder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Directory.CreateDirectory(folder);

        var models = LRHelper.GetLRAModels();
        models.Add(LogisticSimplification<Point>.FromFitted(LRHelper.CreateHandRule(), LRHelper.PixelFeatures, "Rule"));

        var render = SimplificationHelper.RenderOptions;

        var rows = new List<(string Layer, int Pixels, double Perp, bool[] Retained)>();

        int skipped = 0;

        foreach (var (layer, features) in RdpLabelExperiment.SampleTrainingFeatures())
        {
            foreach (var feature in features)
            {
                var box = feature.GetBoundingBox();
                var zoom = WebMercatorUtility.EstimateZoomLevel(box, 128, 128);
                var size = WebMercatorUtility.ToScreenSize(zoom, box);
                var toScreen = Utility.CreateMapToScreenMapFunc(box, size.Width, size.Height);

                foreach (var part in Leaves(feature))
                {
                    var screen = part.Select(toScreen).ToList();

                    for (int i = 1; i < screen.Count - 1; i++)
                    {
                        var (a, b, c) = (screen[i - 1], screen[i], screen[i + 1]);

                        var pixels = DifferingPixels(a, b, c, render);

                        if (pixels is null)
                        {
                            skipped++;
                            continue;
                        }

                        var decisions = models.Select(m => IsRetained(m, a, b, c)).ToArray();

                        var perp = new LRSimplificationParameters<Point>(a, b, c, new() { LRSimplificationFeatures.VerticalDistance }, null).FeatureValues[0];

                        rows.Add((layer, pixels.Value, perp, decisions));
                    }
                }
            }
        }

        var report = new StringBuilder();
        report.AppendLine($"# E2: rasterization oracle on {rows.Count} consecutive triads of 350 OSM features (128² zoom levels); rendering: {render}; skipped (larger than 200 px): {skipped}");
        report.AppendLine();
        report.AppendLine("## Differing pixels when the middle vertex is removed");
        report.AppendLine("pixels\ttriads\tshare");

        foreach (var g in rows.GroupBy(r => Math.Min(r.Pixels, 5)).OrderBy(g => g.Key))
            report.AppendLine($"{(g.Key == 5 ? "≥5" : g.Key.ToString())}\t{g.Count()}\t{g.Count() / (double)rows.Count:P1}");

        report.AppendLine();
        report.AppendLine("## Model decisions against the oracle (oracle: retained ⇔ more than k pixels differ)");
        report.AppendLine("model\tk\tagreement\tmodel removes, oracle retains\tmodel retains, oracle removes\tmodel removes (share)");

        for (int m = 0; m < models.Count; m++)
        {
            foreach (var k in new[] { 0, 1, 2 })
            {
                var agree = rows.Count(r => r.Retained[m] == (r.Pixels > k));
                var removesVisible = rows.Count(r => !r.Retained[m] && r.Pixels > k);
                var retainsInvisible = rows.Count(r => r.Retained[m] && r.Pixels <= k);

                report.AppendLine($"{models[m].Title}\t{k}\t{agree / (double)rows.Count:P1}\t{removesVisible}\t{retainsInvisible}\t{rows.Count(r => !r.Retained[m]) / (double)rows.Count:P1}");
            }
        }

        report.AppendLine();
        report.AppendLine("## By layer, LR7v23 at k = 1");
        report.AppendLine("layer\ttriads\tagreement\tmodel removes, oracle retains\tmodel retains, oracle removes");

        foreach (var g in rows.GroupBy(r => r.Layer).OrderBy(g => g.Key))
        {
            report.AppendLine($"{g.Key}\t{g.Count()}\t{g.Count(r => r.Retained[1] == (r.Pixels > 1)) / (double)g.Count():P1}\t{g.Count(r => !r.Retained[1] && r.Pixels > 1)}\t{g.Count(r => r.Retained[1] && r.Pixels <= 1)}");
        }

        File.WriteAllText(Path.Combine(folder, "E2-oracle-real-triads.txt"), report.ToString());

        return Task.CompletedTask;
    }

    private static bool IsRetained(LogisticSimplification<Point> model, Point a, Point b, Point c)
    {
        // a three-point line keeps its middle point iff the model retains it (Retain3Points off)
        return model.Simplify(new List<Point> { a, b, c }, p => p, false).Count == 3;
    }

    // number of differing drawn pixels between the three-point and the two-point line, drawn at their true
    // sub-pixel position on a canvas around them; null when the triad is too large
    private static int? DifferingPixels(Point a, Point b, Point c, MetricRenderOptions render)
    {
        var minX = Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X))) - 3;
        var minY = Math.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))) - 3;
        var width = (int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X)) - minX) + 4;
        var height = (int)Math.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y)) - minY) + 4;

        if (width > 200 || height > 200)
            return null;

        PointF P(Point p) => new((float)(p.X - minX), (float)(p.Y - minY));

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        MetricRasterizer.RenderScreenPolyline(bitmap, new[] { P(a), P(b), P(c) }, render);
        var original = Mask(bitmap);

        MetricRasterizer.RenderScreenPolyline(bitmap, new[] { P(a), P(c) }, render);
        var simplified = Mask(bitmap);

        return original.Zip(simplified, (u, v) => u != v).Count(d => d);
    }

    private static bool[] Mask(Bitmap bitmap)
    {
        var white = Color.White.ToArgb();
        var result = new bool[bitmap.Width * bitmap.Height];
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var row = new int[bitmap.Width];

            for (int y = 0; y < bitmap.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, bitmap.Width);

                for (int x = 0; x < bitmap.Width; x++)
                    result[y * bitmap.Width + x] = row[x] != 0 && row[x] != white;
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return result;
    }

    internal static IEnumerable<List<Point>> Leaves(Geometry<Point> geometry)
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
}

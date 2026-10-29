using System.IO;
using System.Text;
using System.Diagnostics;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>The experiments of the LAS paper; see the README of this project for what each one produced.</summary>
public enum LrsExperiment
{
    /// <summary>The published comparison (Figure 8, Appendix A), 128² screen.</summary>
    SingleFeatureVaryingThreshold128,
    /// <summary>The same with a 256² screen.</summary>
    SingleFeatureVaryingThreshold256,
    SingleFeatureMultiScale,
    LayersVaryingThreshold512,
    LayersVaryingThreshold1024,
    LayersByZoomLevel,
    SpecialCases,
    /// <summary>
    /// Revision for Transactions in GIS: the 348 features of the published 128² run, the published models plus
    /// the decision trees and the hand rule, all measures of <see cref="RasterComparison"/>, timing, and the
    /// per-layer statistics of <see cref="ExperimentStatistics"/>.
    /// </summary>
    RevisionPublishedFeatures128,
    /// <summary>
    /// The same features and baselines as <see cref="RevisionPublishedFeatures128"/> with the models trained on the
    /// oracle-labelled triads (<see cref="LRHelper.GetOracleModels"/>): decision trees for tolerances 0, 1 and 2
    /// pixels, next to LR7v23 and Tree3v23 as references. Needs the oracle file (<c>--label-oracle</c>).
    /// </summary>
    RevisionOracle128,
    /// <summary>
    /// Feature ranking, λ by cross-validation, feature subsets and 80/20 test metrics of the logistic regression
    /// on the synthetic training set, for the V23 and V24 label rules (<see cref="ModelSelectionExperiment"/>);
    /// also writes the label grids. Needs no map data.
    /// </summary>
    ModelSelection,
    /// <summary>
    /// Checks of the feature choice on the synthetic training set: every feature subset by cross-validation and
    /// leave-one-base-length-out, bootstrap of the coefficient ranking, stability selection
    /// (<see cref="FeatureSelectionChecksExperiment"/>). Needs no map data.
    /// </summary>
    FeatureSelectionChecks,
    /// <summary>Step 1b: LRSv4 on six label sets against RDP/RW/NOPW/BOPW on a validation sample (<see cref="LabelChoiceExperiment"/>).</summary>
    LabelChoiceValidation,
    /// <summary>E13: the 348 features on 512² … 16² screens, LRSv4 against RDP/RW/NOPW/BOPW at every threshold (<see cref="ResubmissionExperiments.CombinedScalesAsync"/>).</summary>
    E13CombinedScales,
    /// <summary>E13 on the 512² screen alone (<see cref="ResubmissionExperiments.CombinedScales512Async"/>).</summary>
    E13CombinedScales512,
    /// <summary>E13 on the 16² screen alone (<see cref="ResubmissionExperiments.CombinedScales16Async"/>).</summary>
    E13CombinedScales16,
    /// <summary>E14 (diagnostic): published traversal vs RestartAtRetained, 128 and 16 px (<see cref="ResubmissionExperiments.TraversalRestartAsync"/>).</summary>
    E14TraversalRestart,
    /// <summary>E14 on the thin-part polygons (<see cref="ResubmissionExperiments.TraversalRestartThinAsync"/>).</summary>
    E14TraversalRestartThin,
    /// <summary>E15: model rows of the resubmission LRS (no-rewind restart traversal) for E13, E12, the label choice, E3, E5 (<see cref="ResubmissionExperiments.ResubmissionLrsAsync"/>).</summary>
    E15ResubmissionLrs,
    /// <summary>E16 (exploratory): CTR alone under the E13 protocol, 348 features, 512² … 16² (<see cref="ResubmissionExperiments.CtrRandomFeaturesAsync"/>).</summary>
    E16CtrRandomFeatures,
    /// <summary>E16 (exploratory): CTR alone on the thin-part polygons under the E12 protocol (<see cref="ResubmissionExperiments.CtrThinPartsAsync"/>).</summary>
    E16CtrThinParts,
    /// <summary>E16 with the pseudo-code CTR (SimplificationType.CumulativeTriangleRoutine2) for the thesis, 348 features.</summary>
    E16Ctr2RandomFeatures,
    /// <summary>E16 with the pseudo-code CTR on the thin-part polygons, for the thesis.</summary>
    E16Ctr2ThinParts,
    /// <summary><see cref="Figure910Original"/> with the pseudo-code CTR, for the thesis.</summary>
    Figure910OriginalCtr2,
    /// <summary>Data of the resubmission Appendix A (<see cref="AppendixSamplesExperiment"/>).</summary>
    AppendixSamples,
    /// <summary>Data of the resubmission Figures 9 and 10 (<see cref="AppendixSamplesExperiment.RunThinPartExamplesAsync"/>).</summary>
    Figure910Samples,
    /// <summary>Data for the redraw of the 2024 Figures 9 and 10 (<see cref="AppendixSamplesExperiment.RunOriginalFigures910Async"/>).</summary>
    Figure910Original,
    /// <summary>Data of the resubmission Figure 7 (<see cref="Figure7SamplesExperiment"/>).</summary>
    Figure7Samples,
    /// <summary>E12 on the 512² screen alone (<see cref="ResubmissionExperiments.ThinParts512Async"/>).</summary>
    E12ThinParts512,
    /// <summary>
    /// Recomputes the visual metric of the published run for the four baselines with the pinned metric rendering
    /// and compares it row by row with the published log (<see cref="PublishedMetricReproduction"/>).
    /// </summary>
    ReproducePublishedMetric,
    /// <summary>
    /// E1: logistic regressions trained on labels from RDP (real OSM triads) instead of the synthetic labels, compared
    /// on the 348 published features (<see cref="RdpLabelExperiment"/>).
    /// </summary>
    E1RdpLabels,
    /// <summary>E2: the rasterization oracle on real OSM triads (<see cref="ResubmissionExperiments.OracleOnRealTriadsAsync"/>).</summary>
    E2OracleRealTriads,
    /// <summary>E3: label boundary moved by ±0.5 px (<see cref="ResubmissionExperiments.BoundarySensitivityAsync"/>).</summary>
    E3BoundarySensitivity,
    /// <summary>E4 + E7: models, hand rule and eight baselines at matched compression (<see cref="ResubmissionExperiments.MainComparisonAsync"/>).</summary>
    E7MainComparison,
    /// <summary>E5: traversal ablation (<see cref="ResubmissionExperiments.TraversalAblationAsync"/>).</summary>
    E5TraversalAblation,
    /// <summary>E6: time against the number of points (<see cref="ResubmissionExperiments.Timing"/>).</summary>
    E6Timing,
    /// <summary>E8: 128², 256² and 512² screens (<see cref="ResubmissionExperiments.ScreenSizesAsync"/>).</summary>
    E8ScreenSizes,
    /// <summary>
    /// E9: the published models against models trained on displacement-based oracle labels (remove when no drawn
    /// pixel moves farther than d) with the displacement measures (<see cref="ResubmissionExperiments.DisplacementLabelsAsync"/>).
    /// </summary>
    E9DisplacementLabels,
    /// <summary>E10: the comparison at coarse scales, 64², 32² and 16² screens (<see cref="ResubmissionExperiments.CoarseScreensAsync"/>).</summary>
    E10CoarseScreens,
    /// <summary>E11: compression at the visual-equivalence limit, threshold sweep per feature, 128² … 16² (<see cref="ResubmissionExperiments.VisualLimitAsync"/>).</summary>
    E11VisualLimit,
    /// <summary>E12: polygons with thin parts from the full layers, 128² … 16², with segment-distance RDP/BOPW (<see cref="ResubmissionExperiments.ThinPartsAsync"/>).</summary>
    E12ThinParts,
}

/// <summary>
/// Entry points of the LRS experiments. Data and output folders are the author's; the models are
/// re-fitted from their training data at load time (<see cref="GetLRAModels"/>).
/// </summary>
public static class LRHelper
{
    /// <summary>All LRS experiments keep a middle point when everything between the ends was removed.</summary>
    public const bool Retain3Points = true;

    /// <summary>OSM layers in Web Mercator: AdminArea, Building, Landuse, Railway, Road, Water, Waterways.</summary>
    public const string DataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";

    public const string ModelFolder = @"E:\University.Ph.D\4. Paper2\Model";

    /// <summary>Every run writes into a new dated sub-folder so that published run folders are never touched.</summary>
    public const string OutputRoot = @"E:\University.Ph.D\4. Paper2\AlgorithmRun";

    /// <summary>The run behind the LAS paper's Figure 8 and Appendix A; its 348 features are stored as <c>{layer}-F{i}.json</c>.</summary>
    public const string PublishedRunFolder = @"E:\University.Ph.D\4. Paper2\AlgorithmRun\1403-07-17-single feature multi threshold 128";

    /// <summary>
    /// The oracle-labelled synthetic triads (<see cref="OracleLabeller"/>), drawn with the metric rendering of the
    /// published run (<see cref="MetricRenderOptions.Published"/>, aliased); a new file next to, not instead of, the
    /// published <c>LR*v23.json</c> training data. Written by <see cref="LabelOracle"/>.
    /// </summary>
    public const string OracleFile = @"E:\University.Ph.D\4. Paper2\Model\Oracle\oracle-triads-v3.json";

    /// <summary>The first oracle file (2026-09-28), drawn anti-aliased; kept for the record and never overwritten.</summary>
    public const string AntiAliasedOracleFile = @"E:\University.Ph.D\4. Paper2\Model\Oracle\oracle-triads-v1.json";

    /// <summary>The second oracle file (2026-10-02): the published rendering, pixel counts only (no displacement); kept and never overwritten.</summary>
    public const string PixelCountOracleFile = @"E:\University.Ph.D\4. Paper2\Model\Oracle\oracle-triads-v2.json";

    /// <summary>
    /// The labelling rules the oracle models are trained with (names become model suffixes): differing drawn
    /// pixels ≤ k with any coverage ("any-k"), with at least half coverage ("opaque-k"), or ≤ a fraction of the
    /// original's pixels ("relative-r"). See <see cref="OracleLabeller.LabelRules"/> for the definitions.
    /// </summary>
    public static readonly string[] OracleModelRules = { "any-k4", "opaque-k1", "opaque-k2", "relative-25", "relative-50" };

    /// <summary>
    /// Retain thresholds on the tree's leaf probability (<see cref="LogisticSimplification{T}.DecisionThreshold"/>):
    /// with labels mixed over rotations and offsets, the leaf probability is the fraction of orientations in which
    /// removing the point is visible, and the threshold says how sure the model must be to keep a point.
    /// </summary>
    public static readonly double[] OracleDecisionThresholds = { 0.5, 0.65, 0.8 };

    public const int OracleTreeDepth = 5;

    /// <summary>
    /// The three pixel quantities the synthetic labels are written in: distance to the base line, overhang
    /// beyond the base, base length. Not the feature set of the published LRSv4/LRSv7.
    /// </summary>
    public static readonly List<LRSimplificationFeatures> PixelFeatures = new()
    {
        LRSimplificationFeatures.VerticalDistance,
        LRSimplificationFeatures.Overhang,
        LRSimplificationFeatures.BaseLength,
    };

    /// <summary>
    /// The two models of the LAS paper (LRSv4 = LR4v23, LRSv7 = LR7v23), re-fitted from their training data.
    /// The training data equals <see cref="SyntheticDataFactory.CreateAll"/>; the JSON files also store the
    /// feature list of each model. Other versions (v3–v22, the LF and LR5 variants) are in the same folder.
    /// </summary>
    public static List<LogisticSimplification<Point>> GetLRAModels()
    {
        var names = new Dictionary<string, string>
        {
            { Path.Combine(ModelFolder, "LR4v23.json"), "LR4v23" },
            { Path.Combine(ModelFolder, "LR7v23.json"), "LR7v23" },
        };

        var result = new List<LogisticSimplification<Point>>();

        foreach (var item in names)
        {
            var trainingData = LRSimplificationTrainingData<Point>.LoadFromJson(item.Key)
                                ?? throw new FileNotFoundException("training data of the LRS model not found", item.Key);

            var model = LogisticSimplification<Point>.Create(trainingData, item.Value);

            result.Add(model);
        }

        return result;
    }

    /// <summary>
    /// The models of the revision: the two published logistic regressions, decision trees of depth 3 and 4 on
    /// <see cref="PixelFeatures"/> (same synthetic labels, same traversal), and the hand-written screen-space
    /// rule "remove when the middle point is at most 0.5 px off the base line and overhangs it by at most
    /// 2.5 px" (the rule-based baseline the reviewer asked for). All run through the same traversal, so the
    /// comparison isolates the decision function.
    /// </summary>
    public static List<LogisticSimplification<Point>> GetRevisionModels()
    {
        var result = GetLRAModels();

        var trainingData = SyntheticDataFactory.CreateTrainingData(PixelFeatures);

        result.Add(LogisticSimplification<Point>.Create(trainingData, "Tree3v23", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 3 })));
        result.Add(LogisticSimplification<Point>.Create(trainingData, "Tree4v23", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 4 })));
        result.Add(LogisticSimplification<Point>.FromFitted(CreateHandRule(), PixelFeatures, "Rule"));

        return result;
    }

    /// <summary>
    /// Renders and labels the synthetic grid with the rasterization oracle and writes <see cref="OracleFile"/>,
    /// the agreement with the published rule table (<c>oracle-agreement.txt</c>) and the label-grid images
    /// (<c>{file}-grids\</c>) next to it, drawn with <see cref="SimplificationHelper.RenderOptions"/>.
    /// The anti-aliased file of 2026-09-28 is never overwritten.
    /// </summary>
    public static void LabelOracle(string? fileName = null)
    {
        fileName ??= OracleFile;

        foreach (var kept in new[] { AntiAliasedOracleFile, PixelCountOracleFile })
            if (string.Equals(Path.GetFullPath(fileName), Path.GetFullPath(kept), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{Path.GetFileName(kept)} is kept as it is; write to another file");

        var folder = Path.GetDirectoryName(fileName)!;

        Directory.CreateDirectory(folder);

        var watch = Stopwatch.StartNew();

        var file = OracleLabeller.Label();

        file.Save(fileName);

        File.WriteAllText(Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + "-agreement.txt"),
                          $"{file.Triads.Count} triads, {file.RotationsDegrees.Count} rotations × {file.Offsets.Count} offsets, labelled in {watch.Elapsed.TotalSeconds:N0} s\r\n\r\n"
                          + OracleLabeller.AgreementWithPublishedLabels(file));

        OracleLabeller.PrintGrids(file, Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + "-grids"), OracleLabeller.LabelRules.Where(r => r.Name is "any-k0" or "any-k4" or "opaque-k0" or "opaque-k1" or "relative-25"));
    }

    /// <summary>
    /// The models of the oracle experiment: LR7v23 and Tree3v23 for reference, and for every rule of
    /// <see cref="OracleModelRules"/> one depth-<see cref="OracleTreeDepth"/> decision tree on
    /// <see cref="PixelFeatures"/>, trained on all rotations and offsets of the oracle-labelled grid, wrapped once
    /// per retain threshold of <see cref="OracleDecisionThresholds"/> ("OracleT5-opaque-k2-p65" = rule opaque-k2,
    /// retain when p &gt; 0.65). The same fitted tree serves all thresholds.
    /// </summary>
    public static List<LogisticSimplification<Point>> GetOracleModels()
    {
        var oracle = OracleSyntheticDataFile.Load(OracleFile)
                     ?? throw new FileNotFoundException("oracle-labelled triads not found; run --label-oracle first", OracleFile);

        var result = GetLRAModels().Where(m => m.Title == "LR7v23").ToList();

        result.Add(LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(PixelFeatures), "Tree3v23", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 3 })));

        foreach (var ruleName in OracleModelRules)
        {
            var rule = OracleLabeller.LabelRules.Single(r => r.Name == ruleName);

            var trainingData = oracle.ToTrainingData(rule.IsRetained, PixelFeatures);

            var fitted = LogisticSimplification<Point>.Create(trainingData, $"OracleT{OracleTreeDepth}-{ruleName}", new DecisionTree(new DecisionTreeOptions() { MaxDepth = OracleTreeDepth }));

            foreach (var threshold in OracleDecisionThresholds)
            {
                result.Add(LogisticSimplification<Point>.FromFitted(fitted.Classifier, PixelFeatures, $"{fitted.Title}-p{threshold * 100:0}"));

                result[^1].DecisionThreshold = threshold;
            }
        }

        return result;
    }

    /// <summary>remove ⇔ VerticalDistance ≤ 0.5 px ∧ Overhang ≤ 2.5 px, as a two-node tree over <see cref="PixelFeatures"/>.</summary>
    public static DecisionTree CreateHandRule()
    {
        var root = new DecisionTreeNode()
        {
            FeatureIndex = PixelFeatures.IndexOf(LRSimplificationFeatures.VerticalDistance),
            Threshold = 0.5,
            Left = new DecisionTreeNode()
            {
                FeatureIndex = PixelFeatures.IndexOf(LRSimplificationFeatures.Overhang),
                Threshold = 2.5,
                Left = new DecisionTreeNode() { Probability = 0 },
                Right = new DecisionTreeNode() { Probability = 1 },
            },
            Right = new DecisionTreeNode() { Probability = 1 },
        };

        return new DecisionTree(root, PixelFeatures.Count);
    }

    /// <summary>Writes every model's decision function (coefficients or tree rules) so a run folder documents what it ran.</summary>
    public static void WriteModelDescriptions(string fileName, IEnumerable<LogisticSimplification<Point>> models)
    {
        var builder = new StringBuilder();

        foreach (var model in models)
        {
            builder.AppendLine($"== {model.Title}");

            switch (model.Classifier)
            {
                case DecisionTree tree:
                    builder.AppendLine($"decision tree, depth {tree.Depth}, {tree.LeafCount} leaves; retain when p > {model.DecisionThreshold}");
                    builder.Append(tree.ToText(PixelFeatures.Select(f => f.ToString()).ToList()));
                    break;

                case LogisticRegression regression:
                    builder.AppendLine($"logistic regression on z-scored features (intercept first); retain when p > {model.DecisionThreshold}");
                    builder.AppendLine(string.Join("\t", regression.Beta.Select(b => b.ToString("N4"))));
                    break;

                default:
                    builder.AppendLine(model.Classifier.GetType().Name);
                    break;
            }

            builder.AppendLine();
        }

        File.WriteAllText(fileName, builder.ToString());
    }

    /// <summary>
    /// Writes the synthetic label grid of every rule set (<see cref="SyntheticDataFactory.CreateLabelGrid"/>) as
    /// <c>label-grid-{rules}.json</c>: the single source of Figure 3 of the LAS paper.
    /// </summary>
    public static void ExportLabelGrids(string? folder = null)
    {
        folder ??= ModelFolder;

        Directory.CreateDirectory(folder);

        foreach (var rules in new[] { SyntheticLabelRules.V23, SyntheticLabelRules.V24 })
        {
            var json = System.Text.Json.JsonSerializer.Serialize(SyntheticDataFactory.CreateLabelGrid(rules), new System.Text.Json.JsonSerializerOptions() { WriteIndented = false });

            File.WriteAllText(Path.Combine(folder, $"label-grid-{rules}.json"), json);
        }
    }

    public static async Task GeneralTest(LrsExperiment experiment = LrsExperiment.SingleFeatureVaryingThreshold128)
    {
        var writeFolder = Path.Combine(OutputRoot, $"{DateTime.Now:yyyy-MM-dd}-{experiment}");

        Directory.CreateDirectory(writeFolder);

        File.WriteAllText(Path.Combine(writeFolder, "Rendering.txt"), $"visual metric rendering: {SimplificationHelper.RenderOptions}{Environment.NewLine}");

        if (experiment == LrsExperiment.ModelSelection)
        {
            ModelSelectionExperiment.Run(writeFolder);

            return;
        }

        if (experiment == LrsExperiment.FeatureSelectionChecks)
        {
            FeatureSelectionChecksExperiment.Run(writeFolder);

            return;
        }

        switch (experiment)
        {
            case LrsExperiment.E12ThinParts512: await ResubmissionExperiments.ThinParts512Async(writeFolder); return;
            case LrsExperiment.Figure7Samples: await Figure7SamplesExperiment.RunAsync(writeFolder); return;
            case LrsExperiment.Figure910Samples: await AppendixSamplesExperiment.RunThinPartExamplesAsync(writeFolder); return;
            case LrsExperiment.Figure910Original: await AppendixSamplesExperiment.RunOriginalFigures910Async(writeFolder); return;
            case LrsExperiment.AppendixSamples: await AppendixSamplesExperiment.RunAsync(writeFolder); return;
            case LrsExperiment.E15ResubmissionLrs: await ResubmissionExperiments.ResubmissionLrsAsync(writeFolder); return;
            case LrsExperiment.E16CtrRandomFeatures: await ResubmissionExperiments.CtrRandomFeaturesAsync(writeFolder); return;
            case LrsExperiment.E16CtrThinParts: await ResubmissionExperiments.CtrThinPartsAsync(writeFolder); return;
            case LrsExperiment.E16Ctr2RandomFeatures: ResubmissionExperiments.CtrType = SimplificationType.CumulativeTriangleRoutine2; await ResubmissionExperiments.CtrRandomFeaturesAsync(writeFolder); return;
            case LrsExperiment.E16Ctr2ThinParts: ResubmissionExperiments.CtrType = SimplificationType.CumulativeTriangleRoutine2; await ResubmissionExperiments.CtrThinPartsAsync(writeFolder); return;
            case LrsExperiment.Figure910OriginalCtr2: ResubmissionExperiments.CtrType = SimplificationType.CumulativeTriangleRoutine2; await AppendixSamplesExperiment.RunOriginalFigures910Async(writeFolder); return;
            case LrsExperiment.E14TraversalRestartThin: await ResubmissionExperiments.TraversalRestartThinAsync(writeFolder); return;
            case LrsExperiment.E14TraversalRestart: await ResubmissionExperiments.TraversalRestartAsync(writeFolder); return;
            case LrsExperiment.E13CombinedScales16: await ResubmissionExperiments.CombinedScales16Async(writeFolder); return;
            case LrsExperiment.E13CombinedScales512: await ResubmissionExperiments.CombinedScales512Async(writeFolder); return;
            case LrsExperiment.E13CombinedScales: await ResubmissionExperiments.CombinedScalesAsync(writeFolder); return;
            case LrsExperiment.LabelChoiceValidation: await LabelChoiceExperiment.RunAsync(writeFolder); return;
            case LrsExperiment.E2OracleRealTriads: await ResubmissionExperiments.OracleOnRealTriadsAsync(writeFolder); return;
            case LrsExperiment.E3BoundarySensitivity: await ResubmissionExperiments.BoundarySensitivityAsync(writeFolder); return;
            case LrsExperiment.E7MainComparison: await ResubmissionExperiments.MainComparisonAsync(writeFolder); return;
            case LrsExperiment.E5TraversalAblation: await ResubmissionExperiments.TraversalAblationAsync(writeFolder); return;
            case LrsExperiment.E6Timing: ResubmissionExperiments.Timing(writeFolder); return;
            case LrsExperiment.E8ScreenSizes: await ResubmissionExperiments.ScreenSizesAsync(writeFolder); return;
            case LrsExperiment.E9DisplacementLabels: await ResubmissionExperiments.DisplacementLabelsAsync(writeFolder); return;
            case LrsExperiment.E10CoarseScreens: await ResubmissionExperiments.CoarseScreensAsync(writeFolder); return;
            case LrsExperiment.E11VisualLimit: await ResubmissionExperiments.VisualLimitAsync(writeFolder); return;
            case LrsExperiment.E12ThinParts: await ResubmissionExperiments.ThinPartsAsync(writeFolder); return;
        }

        if (experiment == LrsExperiment.E1RdpLabels)
        {
            await RdpLabelExperiment.RunAsync(writeFolder);

            return;
        }

        if (experiment == LrsExperiment.ReproducePublishedMetric)
        {
            PublishedMetricReproduction.Run(writeFolder);

            return;
        }

        var logFile = Path.Combine(writeFolder, $"Log-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt");

        var watch = new Stopwatch();

        var isRevision = experiment is LrsExperiment.RevisionPublishedFeatures128 or LrsExperiment.RevisionOracle128;

        var lrModels = experiment switch
        {
            LrsExperiment.RevisionPublishedFeatures128 => GetRevisionModels(),
            LrsExperiment.RevisionOracle128 => GetOracleModels(),
            _ => GetLRAModels(),
        };

        WriteModelDescriptions(Path.Combine(writeFolder, "Models.txt"), lrModels);

        // the revision re-runs the features of the published run; every other experiment samples the layers
        var layers = isRevision
            ? PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(PublishedRunFolder).Select(l => (name: l.Key, features: l.Value)).ToList()
            : Directory.EnumerateFiles(DataFolder, "*.shp", SearchOption.AllDirectories).Select(f => (name: Path.GetFileNameWithoutExtension(f), features: (List<(int, Geometry<Point>)>?)null)).ToList();

        using (var writer = new StreamWriter(Path.Combine(writeFolder, "All-summary.txt"), false))
        {
            writer.WriteLine(experiment == LrsExperiment.SpecialCases ? SimplificationAccuracy.GetTsvHeader() : VisualSimplificationLog.GetHeader());

            foreach (var (fileName, publishedFeatures) in layers)
            {
                var file = Path.Combine(DataFolder, fileName + ".shp");

                watch.Restart();

                switch (experiment)
                {
                    case LrsExperiment.SingleFeatureVaryingThreshold128:
                        await SingleFeatureVaryingThresholdExperiment.RunAsync(file, writer, writeFolder, lrModels, screenSize: 128);
                        break;

                    case LrsExperiment.SingleFeatureVaryingThreshold256:
                        await SingleFeatureVaryingThresholdExperiment.RunAsync(file, writer, writeFolder, lrModels, screenSize: 256);
                        break;

                    case LrsExperiment.SingleFeatureMultiScale:
                        await SingleFeatureMultiScaleExperiment.RunAsync(file, writer, writeFolder, lrModels);
                        break;

                    case LrsExperiment.LayersVaryingThreshold512:
                        await LayerExperiments.RunVaryingThresholdAsync(file, writer, writeFolder, lrModels, screenSize: 512);
                        break;

                    case LrsExperiment.LayersVaryingThreshold1024:
                        await LayerExperiments.RunVaryingThresholdAsync(file, writer, writeFolder, lrModels, screenSize: 1024);
                        break;

                    case LrsExperiment.LayersByZoomLevel:
                        await LayerExperiments.RunByZoomLevelAsync(file, writer, writeFolder, lrModels, new[] { 10, 11, 12, 13, 14, 15 });
                        break;

                    case LrsExperiment.SpecialCases:
                        await SpecialCaseExperiment.RunAsync(file, writer, writeFolder, lrModels);
                        break;

                    case LrsExperiment.RevisionPublishedFeatures128:
                    case LrsExperiment.RevisionOracle128:
                        await SingleFeatureVaryingThresholdExperiment.RunAsync(fileName, publishedFeatures!, writer, writeFolder, lrModels, 128, SingleFeatureVaryingThresholdExperiment.RevisionThresholdCoefficients);
                        break;

                    default:
                        throw new NotImplementedException(experiment.ToString());
                }

                writer.Flush();

                File.AppendAllLines(logFile, new[] { $"Finished At: {DateTime.Now.ToLongTimeString()}; Elapsed: {watch.ElapsedMilliseconds / 1000.0:N0} (s) - ({fileName})" });
            }
        }

        if (experiment != LrsExperiment.SpecialCases)
        {
            ExperimentStatistics.WriteAll(writeFolder, lrModels.Select(m => m.Title).ToList(), SingleFeatureVaryingThresholdExperiment.Methods.Select(m => m.GetDescription()).ToList());

            File.AppendAllLines(logFile, new[] { $"Statistics written at: {DateTime.Now.ToLongTimeString()}" });
        }
    }

    /// <summary>
    /// Renders hand-picked sample features (GeoJSON, WGS84) with every method at the zoom level where the
    /// feature fits a 256² screen: the material of the LAS figures. Outputs go next to the input files.
    /// </summary>
    public static async Task ProcessGeoJson(string dataFolder = @"E:\University.Ph.D\4. Paper2\DataWm\samples")
    {
        var files = Directory.EnumerateFiles(dataFolder, "*.json", SearchOption.AllDirectories);

        var lrModels = GetLRAModels();

        foreach (var jsonFile in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(jsonFile);

            var features = GeoJson.LoadFromFile(jsonFile).Select(f => f.Geometry.TransformToWeMercator()).ToList();

            foreach (var feature in features)
            {
                feature.Srid = SridHelper.WebMercator;
                feature.RemoveConsecutiveDuplicatePoints();
            }

            features = features.Where(f => !f.HasDuplicatePoints()).ToList();

            int featureIndex = 0;

            foreach (var feature in features)
            {
                featureIndex++;

                var zoomLevel = WebMercatorUtility.EstimateZoomLevel(feature.GetBoundingBox(), 256, 256);

                await SimplificationHelper.Compare(feature, featureIndex, dataFolder, zoomLevel, fileName, SpecialCaseExperiment.Methods, lrModels);
            }
        }
    }
}

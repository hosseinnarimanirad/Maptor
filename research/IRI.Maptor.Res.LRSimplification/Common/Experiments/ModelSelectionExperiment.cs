using System.IO;
using System.Text;
using System.Globalization;

using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Feature ranking, model selection and test metrics of the LRS logistic regression (LAS paper, Sections 3.2
/// and 4.1, Tables 3 and 4), computed from the synthetic training set of <see cref="SyntheticDataFactory"/> with
/// the library's own trainer, so that every number of those tables comes from the data the deployed models are
/// trained on. All random choices use <see cref="Seed"/>.
/// <list type="number">
/// <item>The training set: triads and removed triads per base length, and the label conflicts.</item>
/// <item>Feature ranking: by the size of the standardized coefficients of the L1 model at λ = 1 (the ranking of
///       the paper), and by the order in which the features enter the L1 path as λ decreases.</item>
/// <item>λ chosen by stratified 5-fold cross-validation with the one-standard-error rule, for the seven-feature
///       and the four-feature model.</item>
/// <item>Feature subsets compared by the same cross-validation.</item>
/// <item>Stratified 80/20 split: metrics on the test set of a model fitted on the training set, and the
///       coefficients of the deployed model (fitted on all triads).</item>
/// </list>
/// </summary>
public static class ModelSelectionExperiment
{
    public const int Seed = 2026;

    public const int Folds = 5;

    public const double TestFraction = 0.2;

    /// <summary>The candidate features of the paper (LRSv7); LRSv4 is the first four of <see cref="FourFeatures"/>.</summary>
    public static readonly List<LRSimplificationFeatures> SevenFeatures = new()
    {
        LRSimplificationFeatures.Area,
        LRSimplificationFeatures.BaseLength,
        LRSimplificationFeatures.CosineOfAngle,
        LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious,
        LRSimplificationFeatures.SquareCosineOfAngle,
        LRSimplificationFeatures.VerticalDistance,
    };

    /// <summary>The published LRSv4 feature set.</summary>
    public static readonly List<LRSimplificationFeatures> FourFeatures = new()
    {
        LRSimplificationFeatures.Area,
        LRSimplificationFeatures.BaseLength,
        LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious,
    };

    public static readonly double[] LambdaGrid = { 0.001, 0.003, 0.01, 0.03, 0.1, 0.3, 1, 3, 10, 30, 100 };

    public static void Run(string outputFolder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        foreach (var rules in new[] { SyntheticLabelRules.V23, SyntheticLabelRules.V24 })
        {
            var builder = new StringBuilder();

            Write(builder, rules);

            File.WriteAllText(Path.Combine(outputFolder, $"ModelSelection-{rules}.txt"), builder.ToString());
        }

        LRHelper.ExportLabelGrids(outputFolder);
    }

    private static void Write(StringBuilder builder, SyntheticLabelRules rules)
    {
        builder.AppendLine($"# LRS model selection, label rules {rules}, seed {Seed}, {DateTime.Now:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"# trainer: LogisticRegression (proximal Newton, run to convergence), L1, z-scored features; λ as in scikit-learn 1/C");
        builder.AppendLine();

        var data7 = SyntheticDataFactory.CreateTrainingData(SevenFeatures, rules);
        var x7 = data7.ToMatrix(out var labels);

        // 1. training set
        builder.AppendLine("## 1. Training set");
        builder.AppendLine("base length (px)\ttriads\tretained\tremoved");

        foreach (var group in data7.Records.GroupBy(r => r.FeatureValues[SevenFeatures.IndexOf(LRSimplificationFeatures.BaseLength)]).OrderBy(g => g.Key))
        {
            builder.AppendLine($"{group.Key:0.###}\t{group.Count()}\t{group.Count(r => r.IsRetained)}\t{group.Count(r => !r.IsRetained)}");
        }

        builder.AppendLine($"all\t{labels.Length}\t{labels.Count(l => l)}\t{labels.Count(l => !l)}");
        builder.AppendLine();
        builder.AppendLine($"label conflicts, seven features: {ModelEvaluation.FindLabelConflicts(data7.Records.Select(r => (IReadOnlyList<double>)r.FeatureValues).ToList(), labels)}");

        var data4 = SyntheticDataFactory.CreateTrainingData(FourFeatures, rules);
        var x4 = data4.ToMatrix(out _);

        builder.AppendLine($"label conflicts, four features:  {ModelEvaluation.FindLabelConflicts(data4.Records.Select(r => (IReadOnlyList<double>)r.FeatureValues).ToList(), labels)}");
        builder.AppendLine();

        // 2. ranking
        builder.AppendLine("## 2. Feature ranking (all triads)");
        builder.AppendLine("### by |standardized coefficient| of the L1 model at λ = 1");

        var full = Fit(x7, labels, 1);
        var byMagnitude = Enumerable.Range(0, SevenFeatures.Count).OrderByDescending(j => Math.Abs(full.Beta[j + 1])).ToList();

        builder.AppendLine("rank\tfeature\tcoefficient");

        for (int r = 0; r < byMagnitude.Count; r++)
        {
            builder.AppendLine($"{r + 1}\t{SevenFeatures[byMagnitude[r]]}\t{full.Beta[byMagnitude[r] + 1]:0.000}");
        }

        builder.AppendLine();
        builder.AppendLine("### by entry into the L1 path (largest λ at which the coefficient becomes non-zero)");

        var path = Enumerable.Range(0, 61).Select(i => Math.Pow(10, 3 - i * 0.1)).ToArray();
        var entry = new double?[SevenFeatures.Count];

        foreach (var lambda in path)
        {
            var regression = Fit(x7, labels, lambda);

            for (int j = 0; j < SevenFeatures.Count; j++)
            {
                if (entry[j] is null && Math.Abs(regression.Beta[j + 1]) > 1e-9)
                    entry[j] = lambda;
            }
        }

        var ranking = Enumerable.Range(0, SevenFeatures.Count).OrderByDescending(j => entry[j] ?? 0).ToList();

        builder.AppendLine("rank\tfeature\tenters at λ");

        for (int r = 0; r < ranking.Count; r++)
        {
            builder.AppendLine($"{r + 1}\t{SevenFeatures[ranking[r]]}\t{(entry[ranking[r]] is double e ? e.ToString("G3") : "never")}");
        }

        builder.AppendLine();

        // 3. λ by cross-validation
        builder.AppendLine($"## 3. λ by stratified {Folds}-fold cross-validation (mean accuracy, sd over the folds); chosen = largest λ within one standard error of the best");

        var lambda7 = SelectLambda(builder, "seven features", x7, labels);
        var lambda4 = SelectLambda(builder, "four features (LRSv4)", x4, labels);

        // 4. subsets
        builder.AppendLine($"## 4. Feature subsets, {Folds}-fold cross-validation at λ = 1 and at the chosen λ");
        builder.AppendLine("subset\tfeatures\tCV accuracy (λ = 1)\tCV accuracy (chosen λ)\tchosen λ");

        var topByMagnitude = byMagnitude.Take(4).Select(j => SevenFeatures[j]).ToList();
        var topByEntry = ranking.Take(4).Select(j => SevenFeatures[j]).ToList();

        foreach (var (name, features) in new[] { ("all seven (LRSv7)", SevenFeatures), ("LRSv4 as published", FourFeatures), ("top four by coefficient size", topByMagnitude), ("top four by entry into the L1 path", topByEntry) })
        {
            var x = SyntheticDataFactory.CreateTrainingData(features, rules).ToMatrix(out _);
            var chosen = ModelEvaluation.SelectRegularizationStrength(l => new LogisticRegression(Options(l)), LambdaGrid, x, labels, Folds, Seed).OneStandardError;
            var at1 = ModelEvaluation.CrossValidate(() => new LogisticRegression(Options(1)), x, labels, Folds, Seed).MeanAccuracy;
            var atChosen = ModelEvaluation.CrossValidate(() => new LogisticRegression(Options(chosen)), x, labels, Folds, Seed).MeanAccuracy;

            builder.AppendLine($"{name}\t{string.Join(", ", features)}\t{at1:0.0000}\t{atChosen:0.0000}\t{chosen:G3}");
        }

        builder.AppendLine();

        // 5. split and coefficients
        builder.AppendLine($"## 5. Stratified {1 - TestFraction:P0}/{TestFraction:P0} split (seed {Seed}); label 1 = retained");

        var (train, test) = ModelEvaluation.TrainTestSplit(labels, TestFraction, Seed);

        builder.AppendLine($"training rows {train.Length} ({train.Count(i => labels[i])} retained), test rows {test.Length} ({test.Count(i => labels[i])} retained)");
        builder.AppendLine();

        foreach (var (name, x, features, chosen) in new[] { ("LRSv7", x7, SevenFeatures, lambda7), ("LRSv4", x4, FourFeatures, lambda4) })
        {
            foreach (var lambda in new[] { 1.0, chosen }.Distinct())
            {
                var metrics = ModelEvaluation.Evaluate(() => new LogisticRegression(Options(lambda)), x, labels, train, test);
                var deployed = Fit(x, labels, lambda);
                var training = ModelEvaluation.Evaluate(deployed, x, labels, Enumerable.Range(0, labels.Length).ToArray());

                builder.AppendLine($"### {name}, λ = {lambda:G3}");
                builder.AppendLine($"test set:  {metrics}");
                builder.AppendLine($"all triads, deployed model: {training}");
                builder.AppendLine("coefficients of the deployed model (fitted on all triads, z-scored features)");
                builder.AppendLine("intercept\t" + string.Join("\t", features));
                builder.AppendLine(string.Join("\t", deployed.Beta.Select(b => b.ToString("0.000"))));
                builder.AppendLine();
            }
        }
    }

    private static double SelectLambda(StringBuilder builder, string name, Matrix x, bool[] labels)
    {
        var selection = ModelEvaluation.SelectRegularizationStrength(l => new LogisticRegression(Options(l)), LambdaGrid, x, labels, Folds, Seed);

        builder.AppendLine($"### {name}: best λ = {selection.Best:G3}, standard error {selection.StandardError:0.0000}, chosen λ = {selection.OneStandardError:G3}");
        builder.AppendLine("λ\tmean accuracy\tsd\tpooled F1");

        foreach (var (lambda, result) in selection.All)
        {
            builder.AppendLine($"{lambda:G3}\t{result.MeanAccuracy:0.0000}\t{result.StandardDeviationOfAccuracy:0.0000}\t{result.Pooled.F1:0.0000}");
        }

        builder.AppendLine();

        return selection.OneStandardError;
    }

    private static LogisticRegressionOptions Options(double lambda)
    {
        var options = LogisticSimplification<Point>.CreateDefaultRegressionOptions();

        options.RegularizationStrength = lambda;

        return options;
    }

    private static LogisticRegression Fit(Matrix x, bool[] labels, double lambda)
    {
        var regression = new LogisticRegression(Options(lambda));

        regression.Fit(x, labels.Select(l => l ? 1.0 : 0.0).ToArray());

        return regression;
    }
}

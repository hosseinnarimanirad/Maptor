using System.IO;
using System.Text;
using System.Globalization;

using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Checks of the LRSv4 feature choice on the synthetic training set (V23 labels), complementing
/// <see cref="ModelSelectionExperiment"/>. All fits use the library's L1 logistic regression; all random choices use
/// <see cref="ModelSelectionExperiment.Seed"/>.
/// <list type="number">
/// <item>Every non-empty subset of the seven candidate features (127): stratified 5-fold cross-validation and
///       leave-one-base-length-out (train on six base lengths, test on the seventh), at λ = 1.</item>
/// <item>Bootstrap of the coefficient ranking at λ = 1: how often each feature is among the four largest
///       standardized coefficients, and how often the top four equal the LRSv4 set.</item>
/// <item>Stability selection (Meinshausen and Bühlmann 2010): half-samples, L1 path, the largest selection
///       probability of each feature over the path.</item>
/// </list>
/// </summary>
public static class FeatureSelectionChecksExperiment
{
    public const double Lambda = 1;

    public const int Bootstraps = 1000;

    public const int HalfSamples = 200;

    public static void Run(string outputFolder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var rules = SyntheticLabelRules.V23;
        var features = ModelSelectionExperiment.SevenFeatures;

        var data = SyntheticDataFactory.CreateTrainingData(features, rules);
        var x = data.ToMatrix(out var labels);
        var baseColumn = features.IndexOf(LRSimplificationFeatures.BaseLength);
        var baseLengths = Enumerable.Range(0, labels.Length).Select(i => x[i, baseColumn]).ToArray();

        var builder = new StringBuilder();

        builder.AppendLine($"# Feature-selection checks, label rules {rules}, seed {ModelSelectionExperiment.Seed}, λ = {Lambda}, {DateTime.Now:yyyy-MM-dd HH:mm}");
        builder.AppendLine("# accuracy over all triads; 'removed recall' = share of the removed (label 0) triads the model removes, which is what gives compression");
        builder.AppendLine();

        Subsets(builder, features, x, labels, baseLengths);
        Bootstrap(builder, features, x, labels);
        StabilitySelection(builder, features, x, labels);

        File.WriteAllText(Path.Combine(outputFolder, $"FeatureSelectionChecks-{rules}.txt"), builder.ToString());
    }

    private static void Subsets(StringBuilder builder, List<LRSimplificationFeatures> features, Matrix x, bool[] labels, double[] baseLengths)
    {
        builder.AppendLine("## 1. Every feature subset: 5-fold CV and leave-one-base-length-out (LOBO)");
        builder.AppendLine("size\tfeatures\tCV accuracy\tCV removed recall\tLOBO accuracy (pooled)\tLOBO worst base accuracy\tLOBO removed recall");

        var bases = baseLengths.Distinct().OrderBy(b => b).ToArray();
        var rows = new List<(int Size, string Text, double Cv, double Lobo)>();

        for (int mask = 1; mask < 1 << features.Count; mask++)
        {
            var columns = Enumerable.Range(0, features.Count).Where(j => (mask & (1 << j)) != 0).ToArray();
            var subset = SelectColumns(x, columns);

            var cv = ModelEvaluation.CrossValidate(() => Create(Lambda), subset, labels, ModelSelectionExperiment.Folds, ModelSelectionExperiment.Seed);

            int tp = 0, fp = 0, fn = 0, tn = 0;
            var worst = 1.0;

            foreach (var b in bases)
            {
                var train = Enumerable.Range(0, labels.Length).Where(i => baseLengths[i] != b).ToArray();
                var test = Enumerable.Range(0, labels.Length).Where(i => baseLengths[i] == b).ToArray();
                var metrics = ModelEvaluation.Evaluate(() => Create(Lambda), subset, labels, train, test);

                tp += metrics.TruePositive; fp += metrics.FalsePositive; fn += metrics.FalseNegative; tn += metrics.TrueNegative;
                worst = Math.Min(worst, metrics.Accuracy);
            }

            var lobo = (tp + tn) / (double)(tp + fp + fn + tn);
            var text = string.Join(", ", columns.Select(j => features[j]));

            rows.Add((columns.Length, text, cv.MeanAccuracy, lobo));

            builder.AppendLine($"{columns.Length}\t{text}\t{cv.MeanAccuracy:0.0000}\t{cv.Pooled.TrueNegative / (double)(cv.Pooled.TrueNegative + cv.Pooled.FalsePositive):0.0000}\t{lobo:0.0000}\t{worst:0.0000}\t{tn / (double)(tn + fp):0.0000}");
        }

        builder.AppendLine();
        builder.AppendLine("### best subsets of each size (by CV accuracy; LOBO in brackets)");

        foreach (var group in rows.GroupBy(r => r.Size).OrderBy(g => g.Key))
        {
            builder.AppendLine($"size {group.Key}:");

            foreach (var row in group.OrderByDescending(r => r.Cv).Take(3))
            {
                builder.AppendLine($"  {row.Cv:0.0000} ({row.Lobo:0.0000})\t{row.Text}");
            }
        }

        builder.AppendLine();
    }

    private static void Bootstrap(StringBuilder builder, List<LRSimplificationFeatures> features, Matrix x, bool[] labels)
    {
        builder.AppendLine($"## 2. Bootstrap of the coefficient ranking ({Bootstraps} resamples of all triads, λ = {Lambda})");

        var random = new Random(ModelSelectionExperiment.Seed);
        var inTopFour = new int[features.Count];
        var rankSum = new double[features.Count];
        var exactLrsV4 = 0;
        var v4 = ModelSelectionExperiment.FourFeatures.Select(f => features.IndexOf(f)).ToHashSet();
        var topSets = new Dictionary<string, int>();

        for (int b = 0; b < Bootstraps; b++)
        {
            var rows = Enumerable.Range(0, labels.Length).Select(_ => random.Next(labels.Length)).ToArray();
            var model = Fit(ModelEvaluation.SelectRows(x, rows), rows.Select(i => labels[i]).ToArray(), Lambda);
            var order = Enumerable.Range(0, features.Count).OrderByDescending(j => Math.Abs(model.Beta[j + 1])).ToArray();

            for (int r = 0; r < order.Length; r++)
                rankSum[order[r]] += r + 1;

            var top = order.Take(4).ToHashSet();

            foreach (var j in top)
                inTopFour[j]++;

            if (top.SetEquals(v4))
                exactLrsV4++;

            var key = string.Join(", ", top.OrderBy(j => j).Select(j => features[j]));
            topSets[key] = topSets.GetValueOrDefault(key) + 1;
        }

        builder.AppendLine("feature\tshare in top four\tmean rank");

        foreach (var j in Enumerable.Range(0, features.Count).OrderByDescending(j => inTopFour[j]))
        {
            builder.AppendLine($"{features[j]}\t{inTopFour[j] / (double)Bootstraps:0.000}\t{rankSum[j] / Bootstraps:0.00}");
        }

        builder.AppendLine($"top four = LRSv4 set in {exactLrsV4 / (double)Bootstraps:0.000} of the resamples");
        builder.AppendLine("top-four sets:");

        foreach (var (key, count) in topSets.OrderByDescending(p => p.Value))
        {
            builder.AppendLine($"  {count / (double)Bootstraps:0.000}\t{key}");
        }

        builder.AppendLine();
    }

    private static void StabilitySelection(StringBuilder builder, List<LRSimplificationFeatures> features, Matrix x, bool[] labels)
    {
        var path = Enumerable.Range(0, 31).Select(i => Math.Pow(10, 3 - i * 0.1)).ToArray();

        builder.AppendLine($"## 3. Stability selection ({HalfSamples} half-samples without replacement, L1 path λ = 1000 … 1, selection = coefficient ≠ 0)");

        var random = new Random(ModelSelectionExperiment.Seed + 1);
        var selected = new int[path.Length, features.Count];

        for (int s = 0; s < HalfSamples; s++)
        {
            var rows = Enumerable.Range(0, labels.Length).OrderBy(_ => random.Next()).Take(labels.Length / 2).ToArray();
            var xs = ModelEvaluation.SelectRows(x, rows);
            var ls = rows.Select(i => labels[i]).ToArray();

            for (int p = 0; p < path.Length; p++)
            {
                var model = Fit(xs, ls, path[p]);

                for (int j = 0; j < features.Count; j++)
                {
                    if (Math.Abs(model.Beta[j + 1]) > 1e-9)
                        selected[p, j]++;
                }
            }
        }

        builder.AppendLine("λ\t" + string.Join("\t", features));

        for (int p = 0; p < path.Length; p++)
        {
            builder.AppendLine($"{path[p]:G3}\t" + string.Join("\t", Enumerable.Range(0, features.Count).Select(j => (selected[p, j] / (double)HalfSamples).ToString("0.00"))));
        }

        builder.AppendLine();
    }

    private static Matrix SelectColumns(Matrix x, int[] columns)
    {
        var result = new Matrix(x.NumberOfRows, columns.Length);

        for (int i = 0; i < x.NumberOfRows; i++)
        {
            for (int j = 0; j < columns.Length; j++)
            {
                result[i, j] = x[i, columns[j]];
            }
        }

        return result;
    }

    private static LogisticRegression Create(double lambda)
    {
        var options = LogisticSimplification<Point>.CreateDefaultRegressionOptions();

        options.RegularizationStrength = lambda;

        return new LogisticRegression(options);
    }

    private static LogisticRegression Fit(Matrix x, bool[] labels, double lambda)
    {
        var regression = Create(lambda);

        regression.Fit(x, labels.Select(l => l ? 1.0 : 0.0).ToArray());

        return regression;
    }
}

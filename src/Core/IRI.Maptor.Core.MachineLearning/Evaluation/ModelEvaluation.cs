using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>Result of a k-fold cross-validation: the metrics of every fold on its held-out rows.</summary>
public class CrossValidationResult
{
    public IReadOnlyList<BinaryClassificationMetrics> Folds { get; }

    /// <summary>Mean of the fold accuracies (the figure scikit-learn's <c>cross_val_score</c> reports).</summary>
    public double MeanAccuracy => Folds.Average(f => f.Accuracy);

    public double StandardDeviationOfAccuracy
    {
        get
        {
            var mean = MeanAccuracy;

            return Math.Sqrt(Folds.Sum(f => (f.Accuracy - mean) * (f.Accuracy - mean)) / Folds.Count);
        }
    }

    /// <summary>The counts of all folds pooled: every row is predicted once, by the model that did not see it.</summary>
    public BinaryClassificationMetrics Pooled => BinaryClassificationMetrics.Sum(Folds);

    public CrossValidationResult(IReadOnlyList<BinaryClassificationMetrics> folds)
    {
        Folds = folds;
    }
}

/// <summary>
/// Reproducible evaluation of binary classifiers: seeded, stratified train/test split and k-fold
/// cross-validation, the choice of the regularization strength by cross-validation, and the label
/// conflicts of a training set (identical feature vectors with opposite labels).
/// The same seed always gives the same partition, independent of the platform.
/// </summary>
public static class ModelEvaluation
{
    /// <summary>
    /// Stratified random split: the rows of each label are shuffled with <paramref name="seed"/> and the first
    /// <c>round(testFraction × count)</c> of each label form the test set, so both sets keep the label proportions.
    /// </summary>
    /// <returns>Row indices of the training and of the test set, each in increasing order.</returns>
    public static (int[] Train, int[] Test) TrainTestSplit(IReadOnlyList<bool> labels, double testFraction, int seed)
    {
        if (!(testFraction > 0 && testFraction < 1))
            throw new ArgumentOutOfRangeException(nameof(testFraction), testFraction, "must be between 0 and 1");

        var random = new Random(seed);

        var test = new List<int>();
        var train = new List<int>();

        foreach (var label in new[] { true, false })
        {
            var rows = Shuffle(Enumerable.Range(0, labels.Count).Where(i => labels[i] == label).ToArray(), random);

            var testCount = (int)Math.Round(testFraction * rows.Length, MidpointRounding.AwayFromZero);

            test.AddRange(rows.Take(testCount));
            train.AddRange(rows.Skip(testCount));
        }

        return (train.OrderBy(i => i).ToArray(), test.OrderBy(i => i).ToArray());
    }

    /// <summary>
    /// Stratified k folds: the rows of each label are shuffled with <paramref name="seed"/> and dealt to the folds
    /// in turn, so fold sizes differ by at most one per label.
    /// </summary>
    /// <returns>For every fold the indices of its rows (the held-out rows of that fold), in increasing order.</returns>
    public static int[][] StratifiedFolds(IReadOnlyList<bool> labels, int k, int seed)
    {
        if (k < 2 || k > labels.Count)
            throw new ArgumentOutOfRangeException(nameof(k), k, "must be between 2 and the number of rows");

        var random = new Random(seed);

        var folds = Enumerable.Range(0, k).Select(_ => new List<int>()).ToArray();

        var next = 0;

        foreach (var label in new[] { true, false })
        {
            foreach (var row in Shuffle(Enumerable.Range(0, labels.Count).Where(i => labels[i] == label).ToArray(), random))
            {
                folds[next].Add(row);

                next = (next + 1) % k;
            }
        }

        return folds.Select(f => f.OrderBy(i => i).ToArray()).ToArray();
    }

    /// <summary>Fits a new classifier on <paramref name="train"/> and evaluates it on <paramref name="test"/>.</summary>
    /// <param name="createClassifier">Returns a new, unfitted classifier.</param>
    /// <param name="decisionThreshold">A row is predicted positive when its probability exceeds this.</param>
    public static BinaryClassificationMetrics Evaluate(Func<IBinaryClassifier> createClassifier, Matrix x, IReadOnlyList<bool> labels, int[] train, int[] test, double decisionThreshold = 0.5)
    {
        var classifier = createClassifier();

        classifier.Fit(SelectRows(x, train), train.Select(i => labels[i] ? 1.0 : 0.0).ToArray());

        return Evaluate(classifier, x, labels, test, decisionThreshold);
    }

    /// <summary>Metrics of a fitted classifier on the rows <paramref name="rows"/>.</summary>
    public static BinaryClassificationMetrics Evaluate(IBinaryClassifier classifier, Matrix x, IReadOnlyList<bool> labels, int[] rows, double decisionThreshold = 0.5)
    {
        var predicted = rows.Select(i => classifier.Predict(x.GetRow(i).ToList()) > decisionThreshold).ToArray();

        return BinaryClassificationMetrics.FromLabels(rows.Select(i => labels[i]).ToArray(), predicted);
    }

    /// <summary>Stratified k-fold cross-validation (<see cref="StratifiedFolds"/>).</summary>
    public static CrossValidationResult CrossValidate(Func<IBinaryClassifier> createClassifier, Matrix x, IReadOnlyList<bool> labels, int k, int seed, double decisionThreshold = 0.5)
    {
        var folds = StratifiedFolds(labels, k, seed);

        var result = new List<BinaryClassificationMetrics>();

        foreach (var fold in folds)
        {
            var held = new HashSet<int>(fold);

            var train = Enumerable.Range(0, labels.Count).Where(i => !held.Contains(i)).ToArray();

            result.Add(Evaluate(createClassifier, x, labels, train, fold, decisionThreshold));
        }

        return new CrossValidationResult(result);
    }

    /// <summary>
    /// Cross-validates every λ (<see cref="LogisticRegressionOptions.RegularizationStrength"/>) on the same folds and
    /// returns the λ with the highest mean accuracy and the one-standard-error choice: the largest λ (the simplest
    /// model) whose mean accuracy is within one standard error of the best (Hastie, Tibshirani and Friedman 2009,
    /// Section 7.10). The standard error is the standard deviation of the best λ's fold accuracies divided by √k.
    /// </summary>
    /// <param name="createClassifier">Returns a new, unfitted classifier for a given λ.</param>
    public static RegularizationSelection SelectRegularizationStrength(
        Func<double, IBinaryClassifier> createClassifier, IEnumerable<double> lambdas, Matrix x, IReadOnlyList<bool> labels, int k, int seed)
    {
        var all = lambdas.Select(lambda => (lambda, CrossValidate(() => createClassifier(lambda), x, labels, k, seed))).ToList();

        var best = all.OrderByDescending(r => r.Item2.MeanAccuracy).ThenByDescending(r => r.lambda).First();

        var standardError = best.Item2.StandardDeviationOfAccuracy / Math.Sqrt(k);

        var oneStandardError = all.Where(r => r.Item2.MeanAccuracy >= best.Item2.MeanAccuracy - standardError).Max(r => r.lambda);

        return new RegularizationSelection(best.lambda, oneStandardError, standardError, all);
    }

    /// <summary>
    /// Rows whose feature vector, rounded to <paramref name="decimals"/> decimals, also occurs with the opposite
    /// label: observations no classifier on these features can all get right. The groups are reported with the
    /// number of rows of each label.
    /// </summary>
    public static LabelConflicts FindLabelConflicts(IReadOnlyList<IReadOnlyList<double>> features, IReadOnlyList<bool> labels, int decimals = 6)
    {
        var groups = Enumerable.Range(0, labels.Count)
                               .GroupBy(i => string.Join(";", features[i].Select(v => Math.Round(v, decimals).ToString("R", System.Globalization.CultureInfo.InvariantCulture))))
                               .Select(g => (Positive: g.Count(i => labels[i]), Negative: g.Count(i => !labels[i])))
                               .ToList();

        var mixed = groups.Where(g => g.Positive > 0 && g.Negative > 0).ToList();

        return new LabelConflicts(labels.Count, groups.Count, mixed.Count, mixed.Sum(g => g.Positive + g.Negative), mixed.Sum(g => Math.Min(g.Positive, g.Negative)));
    }

    /// <summary>The sub-matrix of the given rows, in that order.</summary>
    public static Matrix SelectRows(Matrix x, int[] rows)
    {
        var result = new Matrix(rows.Length, x.NumberOfColumns);

        for (int i = 0; i < rows.Length; i++)
        {
            for (int j = 0; j < x.NumberOfColumns; j++)
            {
                result[i, j] = x[rows[i], j];
            }
        }

        return result;
    }

    // Fisher–Yates with the given generator
    private static int[] Shuffle(int[] values, Random random)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);

            (values[i], values[j]) = (values[j], values[i]);
        }

        return values;
    }
}

/// <summary>Result of <see cref="ModelEvaluation.SelectRegularizationStrength"/>.</summary>
public class RegularizationSelection
{
    /// <summary>The λ with the highest mean cross-validated accuracy.</summary>
    public double Best { get; }

    /// <summary>The largest λ within one standard error of <see cref="Best"/>.</summary>
    public double OneStandardError { get; }

    public double StandardError { get; }

    public IReadOnlyList<(double Lambda, CrossValidationResult Result)> All { get; }

    public RegularizationSelection(double best, double oneStandardError, double standardError, IReadOnlyList<(double Lambda, CrossValidationResult Result)> all)
    {
        Best = best;
        OneStandardError = oneStandardError;
        StandardError = standardError;
        All = all;
    }
}

/// <summary>Label conflicts of a training set (<see cref="ModelEvaluation.FindLabelConflicts"/>).</summary>
public class LabelConflicts
{
    public int Rows { get; }

    /// <summary>Number of distinct feature vectors.</summary>
    public int DistinctVectors { get; }

    /// <summary>Distinct feature vectors that occur with both labels.</summary>
    public int ConflictingVectors { get; }

    /// <summary>Rows that belong to a conflicting vector.</summary>
    public int RowsInConflict { get; }

    /// <summary>
    /// Rows that any classifier on these features must misclassify (the minority label of each conflicting
    /// vector): the lowest possible training error is this divided by <see cref="Rows"/>.
    /// </summary>
    public int UnavoidableErrors { get; }

    public double ConflictRate => Rows == 0 ? 0 : RowsInConflict / (double)Rows;

    public LabelConflicts(int rows, int distinctVectors, int conflictingVectors, int rowsInConflict, int unavoidableErrors)
    {
        Rows = rows;
        DistinctVectors = distinctVectors;
        ConflictingVectors = conflictingVectors;
        RowsInConflict = rowsInConflict;
        UnavoidableErrors = unavoidableErrors;
    }

    public override string ToString() => $"{Rows} rows, {DistinctVectors} distinct feature vectors, {ConflictingVectors} with both labels ({RowsInConflict} rows, {ConflictRate:P2}); at least {UnavoidableErrors} rows ({UnavoidableErrors / (double)Math.Max(Rows, 1):P2}) are misclassified by any model on these features";
}

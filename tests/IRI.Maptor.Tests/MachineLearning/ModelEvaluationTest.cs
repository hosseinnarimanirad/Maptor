using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

public class ModelEvaluationTest
{
    // 100 rows, 30 positive
    private static readonly bool[] Labels = Enumerable.Range(0, 100).Select(i => i % 10 < 3).ToArray();

    [Fact]
    public void TrainTestSplit_IsStratifiedDisjointCompleteAndReproducible()
    {
        var (train, test) = ModelEvaluation.TrainTestSplit(Labels, 0.2, seed: 7);

        Assert.Equal(80, train.Length);
        Assert.Equal(20, test.Length);
        Assert.Equal(6, test.Count(i => Labels[i]));
        Assert.Empty(train.Intersect(test));
        Assert.Equal(Enumerable.Range(0, 100), train.Concat(test).OrderBy(i => i));

        var again = ModelEvaluation.TrainTestSplit(Labels, 0.2, seed: 7);
        Assert.Equal(test, again.Test);

        var other = ModelEvaluation.TrainTestSplit(Labels, 0.2, seed: 8);
        Assert.NotEqual(test, other.Test);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TrainTestSplit_RejectsFractionOutsideZeroOne(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ModelEvaluation.TrainTestSplit(Labels, fraction, 1));
    }

    [Fact]
    public void StratifiedFolds_PartitionTheRowsWithBalancedLabels()
    {
        var folds = ModelEvaluation.StratifiedFolds(Labels, 5, seed: 3);

        Assert.Equal(5, folds.Length);
        Assert.Equal(Enumerable.Range(0, 100), folds.SelectMany(f => f).OrderBy(i => i));
        Assert.All(folds, f => Assert.Equal(20, f.Length));
        Assert.All(folds, f => Assert.Equal(6, f.Count(i => Labels[i])));
    }

    [Fact]
    public void Metrics_FromLabels()
    {
        var labels = new[] { true, true, true, false, false };
        var predicted = new[] { true, true, false, true, false };

        var m = BinaryClassificationMetrics.FromLabels(labels, predicted);

        Assert.Equal((2, 1, 1, 1), (m.TruePositive, m.FalsePositive, m.FalseNegative, m.TrueNegative));
        Assert.Equal(0.6, m.Accuracy, 12);
        Assert.Equal(2.0 / 3, m.Precision, 12);
        Assert.Equal(2.0 / 3, m.Recall, 12);
        Assert.Equal(2.0 / 3, m.F1, 12);
    }

    [Fact]
    public void CrossValidate_SeparableDataIsLearnedPerfectly()
    {
        // y = 1 iff x > 0.5, with a margin
        var values = Enumerable.Range(0, 60).Select(i => i < 30 ? i / 100.0 : 0.7 + i / 100.0).ToArray();
        var labels = values.Select(v => v > 0.5).ToArray();
        var x = new Matrix(values.Length, 1);

        for (int i = 0; i < values.Length; i++)
            x[i, 0] = values[i];

        var result = ModelEvaluation.CrossValidate(() => new LogisticRegression(new LogisticRegressionOptions() { RegularizationMethod = RegularizationMethods.L1, RegularizationStrength = 0.01 }), x, labels, 5, seed: 1);

        Assert.Equal(5, result.Folds.Count);
        Assert.Equal(1.0, result.MeanAccuracy, 12);
        Assert.Equal(60, result.Pooled.Count);
    }

    [Fact]
    public void SelectRegularizationStrength_OneStandardErrorIsNotSmallerThanBest()
    {
        var training = SyntheticDataFactory.CreateTrainingData(new() { LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.DistanceToNext, LRSimplificationFeatures.DistanceToPrevious });
        var x = training.ToMatrix(out var labels);

        var selection = ModelEvaluation.SelectRegularizationStrength(
            lambda => new LogisticRegression(new LogisticRegressionOptions() { RegularizationMethod = RegularizationMethods.L1, RegularizationStrength = lambda }),
            new[] { 0.1, 1, 100 }, x, labels, 3, seed: 5);

        Assert.Equal(3, selection.All.Count);
        Assert.True(selection.OneStandardError >= selection.Best);
        Assert.True(selection.All.Single(a => a.Lambda == 100).Result.MeanAccuracy < selection.All.Single(a => a.Lambda == selection.Best).Result.MeanAccuracy);
    }

    [Fact]
    public void FindLabelConflicts_CountsVectorsWithBothLabels()
    {
        var features = new List<IReadOnlyList<double>>
        {
            new[] { 1.0, 2.0 },
            new[] { 1.0, 2.0 },
            new[] { 1.0, 2.0 },
            new[] { 3.0, 4.0 },
            new[] { 3.0, 4.0 },
            new[] { 5.0, 6.0 },
        };
        var labels = new[] { true, true, false, true, true, false };

        var conflicts = ModelEvaluation.FindLabelConflicts(features, labels);

        Assert.Equal(6, conflicts.Rows);
        Assert.Equal(3, conflicts.DistinctVectors);
        Assert.Equal(1, conflicts.ConflictingVectors);
        Assert.Equal(3, conflicts.RowsInConflict);
        Assert.Equal(1, conflicts.UnavoidableErrors);
        Assert.Equal(0.5, conflicts.ConflictRate, 12);
    }

    [Fact]
    public void SyntheticTrainingSet_HasNoLabelConflicts()
    {
        var training = SyntheticDataFactory.CreateTrainingData(new() { LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.CosineOfAngle, LRSimplificationFeatures.DistanceToNext, LRSimplificationFeatures.DistanceToPrevious, LRSimplificationFeatures.SquareCosineOfAngle, LRSimplificationFeatures.VerticalDistance });
        training.ToMatrix(out var labels);

        var conflicts = ModelEvaluation.FindLabelConflicts(training.Records.Select(r => (IReadOnlyList<double>)r.FeatureValues).ToList(), labels);

        Assert.Equal(4182, conflicts.Rows);
        Assert.Equal(0, conflicts.ConflictingVectors);
    }
}

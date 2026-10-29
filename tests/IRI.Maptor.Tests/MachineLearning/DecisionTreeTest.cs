using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

public class DecisionTreeTest
{
    private static Matrix ToMatrix(double[][] rows)
    {
        var result = new Matrix(rows.Length, rows[0].Length);

        for (int i = 0; i < rows.Length; i++)
            for (int j = 0; j < rows[0].Length; j++)
                result[i, j] = rows[i][j];

        return result;
    }

    private static DecisionTree Fit(double[][] x, double[] y, DecisionTreeOptions options = null)
    {
        var tree = new DecisionTree(options ?? new DecisionTreeOptions());

        tree.Fit(ToMatrix(x), y);

        return tree;
    }

    [Fact]
    public void Predict_ReturnsNull_BeforeFit_OrForNullInput()
    {
        var tree = new DecisionTree(new DecisionTreeOptions());

        Assert.Null(tree.Predict(new List<double> { 1 }));

        tree.Fit(ToMatrix(new[] { new[] { 0.0 }, new[] { 1.0 } }), new[] { 0.0, 1.0 });

        Assert.Null(tree.Predict(null));
    }

    [Fact]
    public void OneDimensionalThresholdRule_IsRecoveredExactly()
    {
        // label = x > 2.5; the split must fall midway between 2 and 3
        var x = Enumerable.Range(0, 6).Select(i => new[] { (double)i }).ToArray();
        var y = x.Select(r => r[0] > 2.5 ? 1.0 : 0.0).ToArray();

        var tree = Fit(x, y);

        Assert.Equal(1, tree.Depth);
        Assert.Equal(2, tree.LeafCount);
        Assert.Equal(0, tree.Root.FeatureIndex);
        Assert.Equal(2.5, tree.Root.Threshold);

        Assert.Equal(0.0, tree.Predict(new List<double> { 2.4 }));
        Assert.Equal(1.0, tree.Predict(new List<double> { 2.6 }));
    }

    [Fact]
    public void Xor_NeedsDepthTwo_AndIsFitPerfectly()
    {
        // XOR is not separable by one axis-parallel split; two levels solve it
        var x = new[] { new[] { 0.0, 0.0 }, new[] { 0.0, 1.0 }, new[] { 1.0, 0.0 }, new[] { 1.0, 1.0 } };
        var y = new[] { 0.0, 1.0, 1.0, 0.0 };

        var depth1 = Fit(x, y, new DecisionTreeOptions() { MaxDepth = 1 });
        var depth2 = Fit(x, y, new DecisionTreeOptions() { MaxDepth = 2 });

        // no first split lowers the Gini impurity of a balanced XOR, so depth 1 cannot do better than the prior
        foreach (var row in x)
            Assert.Equal(0.5, depth1.Predict(row.ToList()));

        Assert.Equal(2, depth2.Depth);
        Assert.Equal(4, depth2.LeafCount);

        for (int i = 0; i < x.Length; i++)
            Assert.Equal(y[i], depth2.Predict(x[i].ToList()));
    }

    [Fact]
    public void MaxDepthZero_PredictsTheClassPrior()
    {
        var x = Enumerable.Range(0, 10).Select(i => new[] { (double)i }).ToArray();
        var y = Enumerable.Range(0, 10).Select(i => i < 3 ? 1.0 : 0.0).ToArray();

        var tree = Fit(x, y, new DecisionTreeOptions() { MaxDepth = 0 });

        Assert.True(tree.Root.IsLeaf);
        Assert.Equal(0.3, tree.Predict(new List<double> { 100 }).Value, 12);
    }

    [Fact]
    public void ConstantFeature_IsNeverSplit()
    {
        var x = Enumerable.Range(0, 8).Select(_ => new[] { 1.0 }).ToArray();
        var y = new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 };

        var tree = Fit(x, y);

        Assert.True(tree.Root.IsLeaf);
        Assert.Equal(0.5, tree.Predict(new List<double> { 1 }));
    }

    [Fact]
    public void MinSamplesPerLeaf_BlocksSmallChildren()
    {
        // one outlier at x = 10 would be isolated by an unconstrained tree
        var x = new[] { 0.0, 1, 2, 3, 4, 10 }.Select(v => new[] { v }).ToArray();
        var y = new[] { 0.0, 0, 0, 0, 0, 1 };

        Assert.Equal(1.0, Fit(x, y).Predict(new List<double> { 10 }));

        // with two rows per leaf the outlier has to share its leaf with x = 4
        var constrained = Fit(x, y, new DecisionTreeOptions() { MinSamplesPerLeaf = 2 });

        Assert.Equal(0.5, constrained.Predict(new List<double> { 10 }));
        Assert.True(constrained.Root.IsLeaf || constrained.Root.Right.SampleCount >= 2);
    }

    [Fact]
    public void Entropy_AndGini_AgreeOnASeparableProblem()
    {
        var x = Enumerable.Range(0, 20).Select(i => new[] { i * 0.5, Math.Sin(i) }).ToArray();
        var y = x.Select(r => r[0] > 4.75 ? 1.0 : 0.0).ToArray();

        var gini = Fit(x, y, new DecisionTreeOptions() { SplitCriterion = SplitCriterion.Gini });
        var entropy = Fit(x, y, new DecisionTreeOptions() { SplitCriterion = SplitCriterion.Entropy });

        Assert.Equal(gini.Root.FeatureIndex, entropy.Root.FeatureIndex);
        Assert.Equal(gini.Root.Threshold, entropy.Root.Threshold);
        Assert.Equal(1, gini.Depth);
    }

    [Fact]
    public void Fit_RejectsInvalidOptions_AndMismatchedInput()
    {
        var x = ToMatrix(new[] { new[] { 0.0 }, new[] { 1.0 } });

        Assert.Throws<ArgumentException>(() => new DecisionTree(new DecisionTreeOptions()).Fit(x, new[] { 0.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecisionTree(new DecisionTreeOptions() { MaxDepth = -1 }).Fit(x, new[] { 0.0, 1.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecisionTree(new DecisionTreeOptions() { MinSamplesPerLeaf = 0 }).Fit(x, new[] { 0.0, 1.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecisionTree(new DecisionTreeOptions() { MinSamplesToSplit = 1 }).Fit(x, new[] { 0.0, 1.0 }));
    }

    [Fact]
    public void ToText_ListsEveryNode_WithFeatureNames()
    {
        var x = Enumerable.Range(0, 6).Select(i => new[] { (double)i, 0.0 }).ToArray();
        var y = x.Select(r => r[0] > 2.5 ? 1.0 : 0.0).ToArray();

        var text = Fit(x, y).ToText(new[] { "base", "perp" });

        Assert.Contains("base <= 2.5", text);
        Assert.Contains("base > 2.5", text);
        Assert.Contains("-> 0 (p = 0, n = 3)", text);
        Assert.Contains("-> 1 (p = 1, n = 3)", text);
    }

    // ---------------------------------------------------------------------------------------------
    // the LRS use case: a shallow tree on the synthetic triads of the LAS paper

    private static readonly List<LRSimplificationFeatures> Lrs7Features = new()
    {
        LRSimplificationFeatures.Area,
        LRSimplificationFeatures.BaseLength,
        LRSimplificationFeatures.CosineOfAngle,
        LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious,
        LRSimplificationFeatures.SquareCosineOfAngle,
        LRSimplificationFeatures.VerticalDistance,
    };

    private static LRSimplificationTrainingData<Point> CreateTrainingData(List<LRSimplificationFeatures> features)
    {
        var records = new List<LRSimplificationParameters<Point>>();

        foreach (var item in SyntheticDataFactory.CreateAll())
        {
            var original = Geometry<Point>.FromWkt(item.OriginalLineString, 0);
            var simplified = Geometry<Point>.FromWkt(item.SimplifiedLineString, 0);

            records.AddRange(LRSimplificationTrainingData<Point>.Create(original.Points!, simplified.Points!, false, features).Records);
        }

        return new LRSimplificationTrainingData<Point>(records);
    }

    private static Point Identity(Point p) => p;

    private static double TrainingAccuracy(LogisticSimplification<Point> model)
    {
        int correct = 0;

        foreach (var item in SyntheticDataFactory.CreateAll())
        {
            var points = Geometry<Point>.FromWkt(item.OriginalLineString, 0).Points!;

            var retained = model.SimplifyByLogisticRegression(points, Identity).Count == 3;

            if (retained == (item.Note == "RETAINED"))
                correct++;
        }

        return correct / 4182.0;
    }

    [Fact]
    public void SyntheticTriads_Depth5Tree_BeatsTheLogisticRegression()
    {
        var data = CreateTrainingData(Lrs7Features);

        var tree = LogisticSimplification<Point>.Create(data, "tree-5", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 5 }));
        var regression = LogisticSimplification<Point>.Create(data, "LRSv7");

        var treeAccuracy = TrainingAccuracy(tree);
        var regressionAccuracy = TrainingAccuracy(regression);

        Assert.True(treeAccuracy > 0.97, $"tree training accuracy {treeAccuracy:P2}");
        Assert.True(treeAccuracy > regressionAccuracy, $"tree {treeAccuracy:P2} vs logistic regression {regressionAccuracy:P2}");
    }

    [Fact]
    public void SyntheticTriads_Depth3Tree_IsAbove95Percent_WithAtMost8Leaves()
    {
        // eight rules already match the published seven-coefficient logistic regression (about 95.5 %)
        var model = LogisticSimplification<Point>.Create(CreateTrainingData(Lrs7Features), "tree-3", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 3 }));

        var accuracy = TrainingAccuracy(model);

        Assert.True(accuracy > 0.95, $"training accuracy {accuracy:P2}");

        // small enough to print in a paper figure
        Assert.True(((DecisionTree)model.Classifier).LeafCount <= 8);
    }

    [Fact]
    public void SyntheticTriads_Depth5Tree_OnThePixelFeatures_MatchesTheSevenFeatureTree()
    {
        // the label rules are written in (vertical distance, overhang, base length); three features suffice
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.VerticalDistance, LRSimplificationFeatures.Overhang, LRSimplificationFeatures.BaseLength };

        var model = LogisticSimplification<Point>.Create(CreateTrainingData(features), "tree-3f", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 5 }));

        var accuracy = TrainingAccuracy(model);

        Assert.True(accuracy > 0.98, $"training accuracy {accuracy:P2}");
    }

    [Fact]
    public void HandRule_AsAFittedTree_RunsThroughTheTraversal()
    {
        // remove <=> VerticalDistance <= 0.5 px and Overhang <= 2.5 px: the rule-based baseline of the revision
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.VerticalDistance, LRSimplificationFeatures.Overhang, LRSimplificationFeatures.BaseLength };

        var root = new DecisionTreeNode()
        {
            FeatureIndex = 0,
            Threshold = 0.5,
            Left = new DecisionTreeNode()
            {
                FeatureIndex = 1,
                Threshold = 2.5,
                Left = new DecisionTreeNode() { Probability = 0 },
                Right = new DecisionTreeNode() { Probability = 1 },
            },
            Right = new DecisionTreeNode() { Probability = 1 },
        };

        var model = LogisticSimplification<Point>.FromFitted(new DecisionTree(root, 3), features, "rule");

        var accuracy = TrainingAccuracy(model);

        // the two-number rule of the feature assessment: about 92 % of the synthetic labels
        Assert.InRange(accuracy, 0.90, 0.94);

        Assert.Null(LogisticSimplification<Point>.FromFitted(null, features, "x"));
    }

    [Fact]
    public void DecisionThreshold_MovesTheRetainCut()
    {
        // a single leaf that says "retain" with probability 0.6
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.VerticalDistance };

        var tree = new DecisionTree(new DecisionTreeNode() { Probability = 0.6, SampleCount = 10 }, 1);

        var corner = new List<Point> { new(0, 0), new(10, 5), new(20, 0) };

        var lenient = LogisticSimplification<Point>.FromFitted(tree, features, "p50");
        Assert.Equal(0.5, lenient.DecisionThreshold);
        Assert.Equal(3, lenient.SimplifyByLogisticRegression(corner, Identity).Count);

        var strict = LogisticSimplification<Point>.FromFitted(tree, features, "p70");
        strict.DecisionThreshold = 0.7;
        Assert.Equal(2, strict.SimplifyByLogisticRegression(corner, Identity).Count);
    }

    [Fact]
    public void TreeModel_RemovesCollinearMiddlePoints_AndKeepsSharpCorners()
    {
        var model = LogisticSimplification<Point>.Create(CreateTrainingData(Lrs7Features), "tree-5", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 5 }));

        var straight = Enumerable.Range(0, 20).Select(i => new Point(i, 0)).ToList();

        Assert.Equal(new List<Point> { new(0, 0), new(19, 0) }, model.SimplifyByLogisticRegression(straight, Identity));

        var corner = new List<Point> { new(0, 0), new(10, 0), new(10, 10) };

        Assert.Equal(3, model.SimplifyByLogisticRegression(corner, Identity).Count);
    }
}

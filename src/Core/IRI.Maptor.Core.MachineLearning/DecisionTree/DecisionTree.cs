using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.MachineLearning;

// Breiman, Friedman, Olshen and Stone 1984 (CART)
// A binary classification tree with axis-parallel splits, grown greedily: at every node the feature and
// threshold that minimize the weighted impurity of the two children are chosen by exhaustive search over
// the midpoints between consecutive distinct feature values. Growth stops at MaxDepth, at a pure node, or
// when no split satisfies the minimum-size and minimum-decrease options. No pruning; the depth is the
// regularization knob. Fitting is O(p * n log n) per level, prediction is O(depth).
public class DecisionTree : IBinaryClassifier
{
    private readonly DecisionTreeOptions _options;

    public DecisionTreeNode Root { get; private set; }

    public int NumberOfFeatures { get; private set; }

    public DecisionTree(DecisionTreeOptions options)
    {
        _options = options ?? new DecisionTreeOptions();
    }

    /// <summary>
    /// Creates an already fitted tree.
    /// </summary>
    public DecisionTree(DecisionTreeNode root, int numberOfFeatures)
    {
        Root = root;

        NumberOfFeatures = numberOfFeatures;

        _options = new DecisionTreeOptions();
    }

    public void Fit(Matrix xValues, double[] yValues)
    {
        // x: n*p
        // y: n*1

        if (xValues == null || yValues == null)
            throw new ArgumentNullException(xValues == null ? nameof(xValues) : nameof(yValues));

        if (xValues.NumberOfRows != yValues.Length)
            throw new ArgumentException("xValues and yValues must have the same number of rows");

        if (xValues.NumberOfRows == 0)
            throw new ArgumentException("at least one training row is required", nameof(xValues));

        if (_options.MaxDepth < 0)
            throw new ArgumentOutOfRangeException(nameof(DecisionTreeOptions.MaxDepth), _options.MaxDepth, "must not be negative");

        if (_options.MinSamplesPerLeaf < 1)
            throw new ArgumentOutOfRangeException(nameof(DecisionTreeOptions.MinSamplesPerLeaf), _options.MinSamplesPerLeaf, "must be at least 1");

        if (_options.MinSamplesToSplit < 2)
            throw new ArgumentOutOfRangeException(nameof(DecisionTreeOptions.MinSamplesToSplit), _options.MinSamplesToSplit, "must be at least 2");

        if (!(_options.MinImpurityDecrease >= 0))
            throw new ArgumentOutOfRangeException(nameof(DecisionTreeOptions.MinImpurityDecrease), _options.MinImpurityDecrease, "must not be negative");

        NumberOfFeatures = xValues.NumberOfColumns;

        var rows = Enumerable.Range(0, xValues.NumberOfRows).Select(i => xValues.GetRow(i)).ToArray();

        var labels = yValues.Select(y => y > 0.5).ToArray();

        Root = Build(rows, labels, Enumerable.Range(0, rows.Length).ToArray(), 0);
    }

    private DecisionTreeNode Build(double[][] rows, bool[] labels, int[] indices, int depth)
    {
        var positives = indices.Count(i => labels[i]);

        var node = new DecisionTreeNode()
        {
            SampleCount = indices.Length,
            Probability = (double)positives / indices.Length,
        };

        // stopping rules: depth, size, purity
        if (depth >= _options.MaxDepth || indices.Length < _options.MinSamplesToSplit || positives == 0 || positives == indices.Length)
            return node;

        var parentImpurity = Impurity(node.Probability);

        if (!FindBestSplit(rows, labels, indices, positives, out var featureIndex, out var threshold, out var childrenImpurity))
            return node;

        // a decrease equal to the minimum is accepted (as in scikit-learn), so with the default 0 a
        // zero-gain split such as the root of XOR is still made and the children can separate the classes
        if (parentImpurity - childrenImpurity < _options.MinImpurityDecrease)
            return node;

        node.FeatureIndex = featureIndex;
        node.Threshold = threshold;

        var leftIndices = indices.Where(i => rows[i][featureIndex] <= threshold).ToArray();
        var rightIndices = indices.Where(i => rows[i][featureIndex] > threshold).ToArray();

        node.Left = Build(rows, labels, leftIndices, depth + 1);
        node.Right = Build(rows, labels, rightIndices, depth + 1);

        return node;
    }

    // exhaustive search over all features and all midpoints between consecutive distinct values; the
    // rows are sorted once per feature and the class counts are swept, so every candidate costs O(1).
    // Ties keep the first candidate found (lowest feature index, then lowest threshold): deterministic.
    private bool FindBestSplit(double[][] rows, bool[] labels, int[] indices, int positives, out int bestFeature, out double bestThreshold, out double bestImpurity)
    {
        bestFeature = -1;
        bestThreshold = double.NaN;
        bestImpurity = double.PositiveInfinity;

        var n = indices.Length;

        var minLeaf = _options.MinSamplesPerLeaf;

        for (int j = 0; j < NumberOfFeatures; j++)
        {
            var feature = j;

            var sorted = indices.OrderBy(i => rows[i][feature]).ToArray();

            int leftCount = 0, leftPositives = 0;

            for (int k = 0; k < n - 1; k++)
            {
                leftCount++;

                if (labels[sorted[k]])
                    leftPositives++;

                var current = rows[sorted[k]][feature];
                var next = rows[sorted[k + 1]][feature];

                // a split can only fall between two different values
                if (!(next > current))
                    continue;

                var rightCount = n - leftCount;

                if (leftCount < minLeaf || rightCount < minLeaf)
                    continue;

                var rightPositives = positives - leftPositives;

                var impurity = (leftCount * Impurity((double)leftPositives / leftCount) + rightCount * Impurity((double)rightPositives / rightCount)) / n;

                if (impurity < bestImpurity)
                {
                    bestImpurity = impurity;
                    bestFeature = feature;
                    bestThreshold = current + (next - current) / 2;
                }
            }
        }

        return bestFeature >= 0;
    }

    private double Impurity(double p)
    {
        if (p <= 0 || p >= 1)
            return 0;

        switch (_options.SplitCriterion)
        {
            case SplitCriterion.Entropy:
                return -(p * Math.Log(p, 2) + (1 - p) * Math.Log(1 - p, 2));

            case SplitCriterion.Gini:
            default:
                return 2 * p * (1 - p);
        }
    }

    /// <summary>
    /// Fraction of label-1 training rows in the leaf the row falls into.
    /// </summary>
    public double? Predict(List<double> xValues)
    {
        if (Root == null || xValues == null)
            return null;

        if (xValues.Count != NumberOfFeatures)
            throw new ArgumentException($"expected {NumberOfFeatures} feature values, got {xValues.Count}", nameof(xValues));

        var node = Root;

        while (!node.IsLeaf)
        {
            node = xValues[node.FeatureIndex] <= node.Threshold ? node.Left : node.Right;
        }

        return node.Probability;
    }

    public int Depth => Root == null ? 0 : DepthOf(Root);

    private static int DepthOf(DecisionTreeNode node) => node.IsLeaf ? 0 : 1 + Math.Max(DepthOf(node.Left), DepthOf(node.Right));

    public int LeafCount => Root == null ? 0 : LeavesOf(Root);

    private static int LeavesOf(DecisionTreeNode node) => node.IsLeaf ? 1 : LeavesOf(node.Left) + LeavesOf(node.Right);

    /// <summary>
    /// The tree as indented rules, one line per node, e.g. <c>VerticalDistance &lt;= 0.75</c>; a leaf line
    /// reads <c>-&gt; 1 (p = 0.98, n = 1200)</c>. Without <paramref name="featureNames"/> the features are
    /// written as <c>x0, x1, ...</c>.
    /// </summary>
    public string ToText(IList<string> featureNames = null)
    {
        if (Root == null)
            return string.Empty;

        var sb = new StringBuilder();

        Write(Root, 0, featureNames, sb);

        return sb.ToString();
    }

    private static void Write(DecisionTreeNode node, int indent, IList<string> featureNames, StringBuilder sb)
    {
        var pad = new string(' ', 4 * indent);

        if (node.IsLeaf)
        {
            sb.AppendLine($"{pad}-> {(node.Probability > 0.5 ? 1 : 0)} (p = {node.Probability:0.###}, n = {node.SampleCount})");
            return;
        }

        var name = featureNames != null && node.FeatureIndex < featureNames.Count ? featureNames[node.FeatureIndex] : $"x{node.FeatureIndex}";

        sb.AppendLine($"{pad}{name} <= {node.Threshold:0.###}");
        Write(node.Left, indent + 1, featureNames, sb);

        sb.AppendLine($"{pad}{name} > {node.Threshold:0.###}");
        Write(node.Right, indent + 1, featureNames, sb);
    }
}

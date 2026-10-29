namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// One node of a <see cref="DecisionTree"/>. An internal node sends a row to <see cref="Left"/> when
/// <c>row[FeatureIndex] &lt;= Threshold</c> and to <see cref="Right"/> otherwise; a leaf has no children.
/// Every node also carries the training statistics of the rows that reached it, so a tree can be cut at
/// any depth and still predict.
/// </summary>
public class DecisionTreeNode
{
    /// <summary>Index of the feature the node tests; -1 for a leaf.</summary>
    public int FeatureIndex { get; set; } = -1;

    public double Threshold { get; set; }

    public DecisionTreeNode Left { get; set; }

    public DecisionTreeNode Right { get; set; }

    /// <summary>Number of training rows that reached this node.</summary>
    public int SampleCount { get; set; }

    /// <summary>Fraction of those rows with label 1: the prediction of the node when used as a leaf.</summary>
    public double Probability { get; set; }

    public bool IsLeaf => Left == null || Right == null;
}

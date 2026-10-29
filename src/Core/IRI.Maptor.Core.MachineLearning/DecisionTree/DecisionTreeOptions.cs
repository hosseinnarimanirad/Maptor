namespace IRI.Maptor.Core.MachineLearning;

public class DecisionTreeOptions
{
    public SplitCriterion SplitCriterion { get; set; } = SplitCriterion.Gini;

    /// <summary>
    /// Maximum number of splits on the path from the root to a leaf. 0 gives a single leaf that predicts the
    /// class prior. Must not be negative.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// A split is only accepted when both children hold at least this many training rows. Must be at least 1.
    /// </summary>
    public int MinSamplesPerLeaf { get; set; } = 1;

    /// <summary>
    /// A node with fewer training rows than this is not split. Must be at least 2.
    /// </summary>
    public int MinSamplesToSplit { get; set; } = 2;

    /// <summary>
    /// A split is only accepted when it lowers the (row-weighted) impurity of the node by more than this.
    /// Must not be negative.
    /// </summary>
    public double MinImpurityDecrease { get; set; } = 0;
}

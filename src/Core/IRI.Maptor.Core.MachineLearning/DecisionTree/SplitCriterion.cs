namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// Impurity measure a <see cref="DecisionTree"/> minimizes when it chooses a split.
/// </summary>
public enum SplitCriterion
{
    /// <summary>Gini impurity, 2p(1-p) for two classes (CART).</summary>
    Gini = 0,

    /// <summary>Shannon entropy in bits (ID3/C4.5 style information gain).</summary>
    Entropy = 1
}

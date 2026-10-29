using System.Collections.Generic;

using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// A supervised binary classifier: fitted on rows of features with 0/1 labels, it returns the probability
/// of label 1 for a new row. <see cref="LogisticRegression"/> and <see cref="DecisionTree"/> implement it so
/// that <see cref="LogisticSimplification{T}"/> can use either as the retain/remove decision.
/// </summary>
public interface IBinaryClassifier
{
    /// <param name="xValues">One row per observation, one column per feature.</param>
    /// <param name="yValues">Label of every row, 0 or 1.</param>
    void Fit(Matrix xValues, double[] yValues);

    /// <returns>Probability of label 1, or <c>null</c> when the model is not fitted or the input is null.</returns>
    double? Predict(List<double> xValues);
}

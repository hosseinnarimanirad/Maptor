using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// Counts of a binary confusion matrix and the measures derived from them. Label 1 is the positive class
/// (for the LRS models: the middle point is retained).
/// </summary>
public class BinaryClassificationMetrics
{
    public int TruePositive { get; }

    public int FalsePositive { get; }

    public int FalseNegative { get; }

    public int TrueNegative { get; }

    public int Count => TruePositive + FalsePositive + FalseNegative + TrueNegative;

    public double Accuracy => Count == 0 ? double.NaN : (TruePositive + TrueNegative) / (double)Count;

    public double Precision => TruePositive + FalsePositive == 0 ? double.NaN : TruePositive / (double)(TruePositive + FalsePositive);

    public double Recall => TruePositive + FalseNegative == 0 ? double.NaN : TruePositive / (double)(TruePositive + FalseNegative);

    public double F1 => 2 * TruePositive + FalsePositive + FalseNegative == 0 ? double.NaN : 2.0 * TruePositive / (2 * TruePositive + FalsePositive + FalseNegative);

    public BinaryClassificationMetrics(int truePositive, int falsePositive, int falseNegative, int trueNegative)
    {
        TruePositive = truePositive;
        FalsePositive = falsePositive;
        FalseNegative = falseNegative;
        TrueNegative = trueNegative;
    }

    /// <param name="labels">Observed labels, 0 or 1.</param>
    /// <param name="predicted">Predicted labels.</param>
    public static BinaryClassificationMetrics FromLabels(IReadOnlyList<bool> labels, IReadOnlyList<bool> predicted)
    {
        if (labels.Count != predicted.Count)
            throw new ArgumentException("labels and predictions must have the same length");

        int tp = 0, fp = 0, fn = 0, tn = 0;

        for (int i = 0; i < labels.Count; i++)
        {
            if (predicted[i])
            {
                if (labels[i]) tp++; else fp++;
            }
            else
            {
                if (labels[i]) fn++; else tn++;
            }
        }

        return new BinaryClassificationMetrics(tp, fp, fn, tn);
    }

    /// <summary>Pools the counts of several evaluations (e.g. the folds of a cross-validation).</summary>
    public static BinaryClassificationMetrics Sum(IEnumerable<BinaryClassificationMetrics> metrics)
    {
        var list = metrics.ToList();

        return new BinaryClassificationMetrics(list.Sum(m => m.TruePositive), list.Sum(m => m.FalsePositive), list.Sum(m => m.FalseNegative), list.Sum(m => m.TrueNegative));
    }

    public override string ToString() => $"accuracy {Accuracy:0.0000}, precision {Precision:0.0000}, recall {Recall:0.0000}, F1 {F1:0.0000} (TP {TruePositive}, FP {FalsePositive}, FN {FalseNegative}, TN {TrueNegative})";
}

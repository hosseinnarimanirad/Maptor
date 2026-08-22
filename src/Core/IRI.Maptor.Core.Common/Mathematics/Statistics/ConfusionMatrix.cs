using IRI.Maptor.Core.Common.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Mathematics;

public class ConfusionMatrix
{
    // True Positive (TP): The predicted class was positive and the actual class was positive.
    public int TruePositive { get; set; }

    // False Positive (FP): The model predicted positive but the actual class was negative.
    public int FalsePositive { get; set; }

    // False Negative (FN): The predicted class was negative but the actual class was positive.
    public int FalseNegative { get; set; }

    // True Negative (TN): The predicted class was negative and the actual class was negative.
    public int TrueNegative { get; set; }


    public ConfusionMatrix(int truePositive, int falsePositive, int falseNegative, int trueNegative)
    {
        TruePositive = truePositive;
        FalsePositive = falsePositive;
        FalseNegative = falseNegative;
        TrueNegative = trueNegative;
    }

    // matrix are zero-one matrix
    // zero means negative and one means positive
    public ConfusionMatrix(Matrix actual, Matrix predicted, Func<double, bool> IsPositive, Func<double, bool> IsNegative)
    {
        if (!Matrix.AreTheSameSize(actual, predicted))
            throw new NotImplementedException();

        for (int i = 0; i < actual.NumberOfRows; i++)
        {
            for (int j = 0; j < actual.NumberOfColumns; j++)
            {
                double actualValue = actual[i, j];

                double predictedValue = predicted[i, j];

                if (IsPositive(actualValue) && IsPositive(predictedValue))
                {
                    TruePositive++;
                }
                else if (IsPositive(actualValue) && IsNegative(predictedValue))
                {
                    FalseNegative++;
                }
                else if (IsNegative(actualValue) && IsPositive(predictedValue))
                {
                    FalsePositive++;
                }
                else if (IsNegative(actualValue) && IsNegative(predictedValue))
                {
                    TrueNegative++;
                }
            }
        }
    }



    // Total predictions
    public int Total => TruePositive + FalsePositive + FalseNegative + TrueNegative;


    #region Basic Metrics

    // Accuracy: (TP + TN) / (TP + TN + FP + FN)
    public double Accuracy => (double)(TruePositive + TrueNegative) / Total;

    // Precision (Positive Predictive Value): TP / (TP + FP)
    public double Precision => TruePositive + FalsePositive == 0 ? 0 : (double)TruePositive / (TruePositive + FalsePositive);

    // Recall (Sensitivity or True Positive Rate): TP / (TP + FN)
    public double Recall => TruePositive + FalseNegative == 0 ? 0 : (double)TruePositive / (TruePositive + FalseNegative);

    // F1 Score: 2 * (Precision * Recall) / (Precision + Recall)
    public double F1Score => Precision + Recall == 0 ? 0 : 2 * (Precision * Recall) / (Precision + Recall);

    // Specificity (True Negative Rate): TN / (TN + FP)
    public double Specificity => TrueNegative + FalsePositive == 0 ? 0 : (double)TrueNegative / (TrueNegative + FalsePositive);

    // Error Rate: (FP + FN) / (TP + TN + FP + FN)
    public double ErrorRate => (double)(FalsePositive + FalseNegative) / Total;

    #endregion


    #region Advanced Metrics

    // False Positive Rate (FPR): FP / (FP + TN)
    public double FalsePositiveRate => FalsePositive + TrueNegative == 0 ? 0 : (double)FalsePositive / (FalsePositive + TrueNegative);

    // False Negative Rate (FNR): FN / (FN + TP)
    public double FalseNegativeRate => FalseNegative + TruePositive == 0 ? 0 : (double)FalseNegative / (FalseNegative + TruePositive);

    // Negative Predictive Value (NPV): TN / (TN + FN)
    public double NegativePredictiveValue => TrueNegative + FalseNegative == 0 ? 0 : (double)TrueNegative / (TrueNegative + FalseNegative);

    // False Discovery Rate (FDR): FP / (FP + TP)
    public double FalseDiscoveryRate => FalsePositive + TruePositive == 0 ? 0 : (double)FalsePositive / (FalsePositive + TruePositive);

    // False Omission Rate (FOR): FN / (FN + TN)
    public double FalseOmissionRate => FalseNegative + TrueNegative == 0 ? 0 : (double)FalseNegative / (FalseNegative + TrueNegative);

    // Threat Score (TSS or Critical Success Index): TP / (TP + FN + FP)
    public double ThreatScore => TruePositive + FalseNegative + FalsePositive == 0 ? 0 : (double)TruePositive / (TruePositive + FalseNegative + FalsePositive);

    // Balanced Accuracy: (Sensitivity + Specificity) / 2
    public double BalancedAccuracy => (Recall + Specificity) / 2;

    // Matthews Correlation Coefficient (MCC)
    public double MatthewsCorrelationCoefficient
    {
        get
        {
            double numerator = (TruePositive * TrueNegative) - (FalsePositive * FalseNegative);
            double denominator = Math.Sqrt((TruePositive + FalsePositive) * (TruePositive + FalseNegative) * (TrueNegative + FalsePositive) * (TrueNegative + FalseNegative));
            return denominator == 0 ? 0 : numerator / denominator;
        }
    }
    #endregion

    public string AsTsv()
    {
        var values = new List<string>()
        {
            TruePositive.ToString(),
            FalsePositive.ToString(),
            FalseNegative.ToString(),
            TrueNegative.ToString(),
            Accuracy.ToString("N4"),
            Precision.ToString("N4"),
            Recall.ToString("N4"),
            F1Score.ToString("N4"),
            Specificity.ToString("N4"),
            ErrorRate.ToString("N4")
        };

        return string.Join("\t", values);
    }

    public static string GetTsvHeader()
    {
        var headers = new List<string>()
        {
            {nameof(TruePositive)},
            {nameof(FalsePositive)},
            {nameof(FalseNegative)},
            {nameof(TrueNegative)},
            {nameof(Accuracy)},
            {nameof(Precision)},
            {nameof(Recall)},
            {nameof(F1Score)},
            {nameof(Specificity)},
            {nameof(ErrorRate)}
        };

        return string.Join("\t", headers);
    }


    // 1. Perfect Confusion Matrix: All predictions are correct.
    public static ConfusionMatrix CreatePerfectMatrix(int tp, int tn) => new ConfusionMatrix(tp, 0, 0, tn);

    // 2. Zero Confusion Matrix (Worst Case): All predictions are incorrect.
    public static ConfusionMatrix CreateZeroMatrix(int fp, int fn) => new ConfusionMatrix(0, fp, fn, 0);

    // 3. Identity Confusion Matrix: Perfect balance between TP and TN.
    public static ConfusionMatrix CreateIdentityMatrix(int tp_tn) => new ConfusionMatrix(tp_tn, 0, 0, tp_tn);
 
    // 4. Diagonal Dominant Confusion Matrix: Mostly correct predictions with small errors.
    // 5. Off-Diagonal Confusion Matrix: Mostly incorrect predictions.
    // 6. Skewed Confusion Matrix: Majority of predictions are in one class.
    // 7. Symmetric Confusion Matrix: Equal errors and correct predictions.
    public static ConfusionMatrix CreateSymmetricMatrix(int tp_tn, int fp_fn)
    {
        return new ConfusionMatrix(tp_tn, fp_fn, fp_fn, tp_tn);
    }
}

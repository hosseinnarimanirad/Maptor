using System;
using System.Linq;
using System.Collections.Generic;

namespace IRI.Maptor.Core.MachineLearning;

public class WilcoxonSignedRankResult
{
    /// <summary>Number of pairs with a non-zero difference (zero differences are discarded, Wilcoxon's rule).</summary>
    public int N { get; set; }

    public int Zeros { get; set; }

    /// <summary>Sum of the ranks of the positive differences (first sample larger).</summary>
    public double WPlus { get; set; }

    public double WMinus { get; set; }

    /// <summary>min(W+, W−), the two-sided test statistic.</summary>
    public double Statistic => Math.Min(WPlus, WMinus);

    /// <summary>Normal approximation with tie and continuity corrections; negative when the first sample tends to be smaller.</summary>
    public double Z { get; set; }

    /// <summary>Two-sided p-value of the normal approximation. NaN when fewer than one non-zero pair exists.</summary>
    public double PValue { get; set; }

    public double MedianDifference { get; set; }
}

/// <summary>
/// Paired non-parametric tests used to compare simplification methods feature by feature.
/// </summary>
public static class HypothesisTests
{
    /// <summary>
    /// Wilcoxon signed-rank test of the paired differences <c>first[i] − second[i]</c> (Wilcoxon 1945), with
    /// average ranks for tied |differences|, zero differences discarded, and the two-sided p-value from the
    /// normal approximation with tie correction and continuity correction. This matches
    /// <c>scipy.stats.wilcoxon(x, y, zero_method="wilcox", correction=True, method="approx")</c>; the
    /// approximation is customary from about 10 non-zero pairs upwards.
    /// </summary>
    public static WilcoxonSignedRankResult WilcoxonSignedRank(IList<double> first, IList<double> second)
    {
        if (first == null || second == null)
            throw new ArgumentNullException(first == null ? nameof(first) : nameof(second));

        if (first.Count != second.Count)
            throw new ArgumentException("the samples must be paired: same length");

        var differences = new List<double>();

        int zeros = 0;

        for (int i = 0; i < first.Count; i++)
        {
            var d = first[i] - second[i];

            if (double.IsNaN(d))
                continue;

            if (d == 0)
                zeros++;
            else
                differences.Add(d);
        }

        var result = new WilcoxonSignedRankResult()
        {
            N = differences.Count,
            Zeros = zeros,
            MedianDifference = Median(first.Zip(second, (a, b) => a - b).Where(d => !double.IsNaN(d)).ToList()),
            PValue = double.NaN,
            Z = double.NaN,
        };

        var n = differences.Count;

        if (n == 0)
            return result;

        var ranks = AverageRanks(differences.Select(Math.Abs).ToArray(), out var tieCorrection);

        double wPlus = 0, wMinus = 0;

        for (int i = 0; i < n; i++)
        {
            if (differences[i] > 0)
                wPlus += ranks[i];
            else
                wMinus += ranks[i];
        }

        result.WPlus = wPlus;
        result.WMinus = wMinus;

        var mean = n * (n + 1) / 4.0;

        var variance = (n * (n + 1.0) * (2 * n + 1.0) - tieCorrection / 2) / 24;

        if (variance <= 0)
            return result;

        var statistic = Math.Min(wPlus, wMinus);

        // continuity correction towards the mean, as scipy applies it for the two-sided test
        var correction = statistic == mean ? 0 : 0.5 * Math.Sign(statistic - mean);

        var z = (statistic - mean - correction) / Math.Sqrt(variance);

        result.Z = z;

        result.PValue = Math.Min(1, 2 * StandardNormalSurvival(Math.Abs(z)));

        return result;
    }

    // ranks 1..n with tied values sharing their average rank; tieCorrection = sum over ties of (t^3 - t)
    private static double[] AverageRanks(double[] values, out double tieCorrection)
    {
        var n = values.Length;

        var order = Enumerable.Range(0, n).OrderBy(i => values[i]).ToArray();

        var ranks = new double[n];

        tieCorrection = 0;

        int start = 0;

        while (start < n)
        {
            int end = start;

            while (end + 1 < n && values[order[end + 1]] == values[order[start]])
                end++;

            var count = end - start + 1;

            var rank = (start + 1 + end + 1) / 2.0;

            for (int k = start; k <= end; k++)
                ranks[order[k]] = rank;

            if (count > 1)
                tieCorrection += (double)count * count * count - count;

            start = end + 1;
        }

        return ranks;
    }

    public static double Median(IList<double> values)
    {
        if (values == null || values.Count == 0)
            return double.NaN;

        var sorted = values.OrderBy(v => v).ToArray();

        var middle = sorted.Length / 2;

        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    /// <summary>Upper tail of the standard normal distribution, P(Z &gt; z).</summary>
    public static double StandardNormalSurvival(double z) => 0.5 * Erfc(z / Math.Sqrt(2));

    // complementary error function; Numerical Recipes erfcc (Chebyshev fit), relative error below 1.2e-7 everywhere
    public static double Erfc(double x)
    {
        var z = Math.Abs(x);

        var t = 1.0 / (1.0 + 0.5 * z);

        var result = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                     t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                     t * (-0.82215223 + t * 0.17087277)))))))));

        return x >= 0 ? result : 2.0 - result;
    }
}

using System;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// The bins of a histogram over numbers from <c>min</c> to <c>max</c>: equal-width bins whose width is a "nice"
/// number (1, 2 or 5 times a power of ten) and whose first edge is a multiple of it, so the edges read as round
/// values; about Sturges' number of bins (log2(n) + 1, within <see cref="MinBins"/>..<see cref="MaxBins"/>).
/// Shared by the client's in-memory statistics and the server's SQL ones, so both bin the same way.
/// </summary>
public static class HistogramLayout
{
    public const int MinBins = 5;

    public const int MaxBins = 20;

    /// <summary>
    /// The first edge, the width and the number of bins for <paramref name="count"/> values (at least one).
    /// All values equal: one bin centred on them.
    /// </summary>
    public static (double Start, double BinWidth, int BinCount) Calculate(int count, double min, double max)
    {
        if (min == max)
        {
            var width = min == 0 ? 1 : NiceNumber(Math.Abs(min) / 10);

            return (min - width / 2, width, 1);
        }

        var target = Math.Max(MinBins, Math.Min(MaxBins, CeilingLog2(count) + 1));

        var binWidth = NiceNumber((max - min) / target);
        var start = Math.Floor(min / binWidth) * binWidth;
        var binCount = (int)Math.Ceiling((max - start) / binWidth);

        // max on an edge: its bin would be the empty one after the last, so it is folded into the last
        binCount = Math.Max(1, Math.Min(MaxBins * 2, binCount));

        return (start, binWidth, binCount);
    }

    /// <summary>The bin of <paramref name="value"/>; values outside the range go to the first or the last bin.</summary>
    public static int BinOf(double value, double start, double binWidth, int binCount)
        => Math.Max(0, Math.Min(binCount - 1, (int)Math.Floor((value - start) / binWidth)));

    /// <summary>The smallest of 1, 2, 5 × 10^k not below <paramref name="value"/>.</summary>
    public static double NiceNumber(double value)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            return 1;

        var exponent = Math.Floor(Math.Log10(value));
        var power = Math.Pow(10, exponent);
        var fraction = value / power;

        var nice = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;

        return nice * power;
    }

    // ceil(log2(n)) computed exactly: a floating-point log can land just above an integer for a power of two
    private static int CeilingLog2(int n)
    {
        var result = 0;

        for (long power = 1; power < n; power *= 2)
            result++;

        return result;
    }
}

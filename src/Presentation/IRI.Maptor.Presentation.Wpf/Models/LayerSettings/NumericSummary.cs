using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

/// <summary>
/// Equal-width bins over a set of numbers, laid out by <see cref="HistogramLayout"/> (shared with the server's
/// statistics): the bin width is a "nice" number (1, 2 or 5 times a power of ten) and the first edge a multiple
/// of it, so the edges read as round values. A value on the last edge goes into the last bin.
/// </summary>
public class Histogram
{
    public const int MinBins = HistogramLayout.MinBins;

    public const int MaxBins = HistogramLayout.MaxBins;

    /// <summary>Lower edge of the first bin.</summary>
    public double Start { get; init; }

    public double BinWidth { get; init; }

    public IReadOnlyList<int> Counts { get; init; } = [];

    public int BinCount => Counts.Count;

    public double GetLowerEdge(int bin) => Start + bin * BinWidth;

    public double GetUpperEdge(int bin) => Start + (bin + 1) * BinWidth;

    /// <summary>
    /// Null for no values. All values equal: one bin around them. Otherwise about Sturges' number of bins
    /// (log2(n) + 1, within <see cref="MinBins"/>..<see cref="MaxBins"/>), adjusted to nice edges.
    /// </summary>
    public static Histogram? Calculate(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return null;

        var (start, binWidth, binCount) = HistogramLayout.Calculate(values.Count, values.Min(), values.Max());

        var counts = new int[binCount];

        foreach (var value in values)
            counts[HistogramLayout.BinOf(value, start, binWidth, binCount)]++;

        return new Histogram { Start = start, BinWidth = binWidth, Counts = counts };
    }

    /// <summary>The smallest of 1, 2, 5 × 10^k not below <paramref name="value"/>.</summary>
    public static double NiceNumber(double value) => HistogramLayout.NiceNumber(value);
}

/// <summary>Min, max, sum, mean, median, sample standard deviation and histogram of a set of numbers.</summary>
public class NumericSummary
{
    public int Count { get; init; }

    public double Min { get; init; }

    public double Max { get; init; }

    public double Sum { get; init; }

    public double Mean { get; init; }

    public double Median { get; init; }

    /// <summary>Sample standard deviation; null below two values.</summary>
    public double? StandardDeviation { get; init; }

    public Histogram? Histogram { get; init; }

    /// <summary>Null for no values. Sorts <paramref name="values"/> in place.</summary>
    public static NumericSummary? Calculate(List<double> values)
    {
        if (values.Count == 0)
            return null;

        values.Sort();

        var sum = values.Sum();
        var mean = sum / values.Count;

        var middle = values.Count / 2;
        var median = values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2.0;

        double? standardDeviation = values.Count < 2
            ? null
            : Math.Sqrt(values.Sum(n => (n - mean) * (n - mean)) / (values.Count - 1));

        return new NumericSummary
        {
            Count = values.Count,
            Min = values[0],
            Max = values[^1],
            Sum = sum,
            Mean = mean,
            Median = median,
            StandardDeviation = standardDeviation,
            Histogram = Histogram.Calculate(values),
        };
    }
}

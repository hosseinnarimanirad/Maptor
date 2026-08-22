using IRI.Maptor.Core.Common.Mathematics;
using System;
using System.Linq;

namespace IRI.Maptor.Core.MachineLearning;

public static class GeneralStatistics
{
    public static StatisticsSummary CalculateSummary(double[] values)
    {
        if (values == null || values.Length == 0)
        {
            return new StatisticsSummary();
        }

        var length = values.Length;

        var sortedValues = (double[])values.Clone();

        Array.Sort(sortedValues);

        var result = new StatisticsSummary();

        result.Min = sortedValues.First();

        result.Max = sortedValues.Last();

        result.FirstQuartile = sortedValues[length / 4];

        result.Median = sortedValues[length / 2];

        result.ThirdQuartile = sortedValues[length * 3 / 4];

        result.Mean = IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateMean(values);

        return result;
    }

}

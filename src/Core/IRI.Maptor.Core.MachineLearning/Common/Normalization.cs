using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.MachineLearning;

public static class Normalization
{
    public static Matrix NormalizeColumnsUsingZScore(Matrix values, VarianceCalculationMode mode)
    {
        return NormalizeColumnsUsingZScore(values, mode, out _);
    }

    /// <summary>
    /// Also returns the mean and standard deviation of every original column, computed with
    /// <paramref name="mode"/>, i.e. exactly the values the normalization used.
    /// </summary>
    public static Matrix NormalizeColumnsUsingZScore(Matrix values, VarianceCalculationMode mode, out List<BasicStatisticsInfo> statistics)
    {
        statistics = null;

        if (values == null)
            return null;

        int numberOfColumns = values.NumberOfColumns;

        Matrix result = new Matrix(values.NumberOfRows, values.NumberOfColumns);

        statistics = new List<BasicStatisticsInfo>(numberOfColumns);

        for (int i = 0; i < numberOfColumns; i++)
        {
            var columnValues = values.GetColumn(i);

            var mean = IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateMean(columnValues);

            var std = IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateStandardDeviation(columnValues, mode);

            statistics.Add(new BasicStatisticsInfo() { Mean = mean, StandardDeviation = std });

            result.SetColumn(i, NormalizeUsingZScore(columnValues, mean, std));
        }

        return result;
    }


    // **********************************************************************
    // Z score standardization is one of the most popular 
    // method to normalize data. In this case, we rescale 
    // an original variable to have a mean of zero and 
    // standard deviation of one.
    // **********************************************************************
    public static double[] NormalizeUsingZScore(double[] values, VarianceCalculationMode mode)
    {
        if (values.IsNullOrEmpty())
        {
            return null;
        }

        var mean = IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateMean(values);

        var std = IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateStandardDeviation(values, mode);

        return NormalizeUsingZScore(values, mean, std);
    }

    public static double[] NormalizeUsingZScore(double[] values, double mean, double standardDeviation)
    {
        if (values.IsNullOrEmpty())
        {
            return null;
        }

        double[] result = new double[values.Length];

        // 1399.12.12
        // to prevent divide by zero
        if (standardDeviation == 0)
            standardDeviation = 1;

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (values[i] - mean) / standardDeviation;
        }

        return result;
    }

    public static double NormalizeUsingZScore(double value, double mean, double standardDeviation)
    {
        // 1399.12.12
        // to prevent divide by zero
        if (standardDeviation == 0)
            standardDeviation = 1;

        return (value - mean) / standardDeviation;
    }

    // **********************************************************************
    // It is also called 0-1 scaling because the standardized 
    // value using this method lies between 0 and 1.
    // **********************************************************************
    public static double[] NormalizeUsingMinMax(double[] values)
    {
        if (values.IsNullOrEmpty())
        {
            return null;
        }

        double[] result = new double[values.Length];

        var min = Statistics.GetMin(values);

        var max = Statistics.GetMax(values);

        var range = max - min;

        // to prevent divide by zero
        if (range == 0)
            range = 1;

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (values[i] - min) / range;
        }

        return result;
    }


    // **********************************************************************
    // In this method, we divide each value by the standard deviation. 
    // The idea is to have equal variance, but different means and ranges.
    // **********************************************************************
    public static double[] NormalizeUsingStandardDeviation(double[] values, VarianceCalculationMode mode)
    {
        if (values.IsNullOrEmpty())
        {
            return null;
        }

        double[] result = new double[values.Length];

        var std = Statistics.CalculateStandardDeviation(values, mode);

        // to prevent divide by zero
        if (std == 0)
            std = 1;

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (values[i]) / std;
        }

        return result;
    }


    // **********************************************************************
    // In this method, we divide each value by the standard deviation. 
    // The idea is to have equal variance, but different means and ranges.
    // **********************************************************************
    public static double[] NormalizeUsingRange(double[] values)
    {
        if (values.IsNullOrEmpty())
        {
            return null;
        }

        double[] result = new double[values.Length];

        var min = Statistics.GetMin(values);

        var max = Statistics.GetMax(values);

        var range = max - min;

        // to prevent divide by zero
        if (range == 0)
            range = 1;

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (values[i]) / range;
        }

        return result;
    }
}

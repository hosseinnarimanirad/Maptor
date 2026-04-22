using System;
using System.Collections.Generic;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Exceptions;

namespace IRI.Maptor.Sta.Mathematics;

public static class Statistics
{
    #region Maximum

    public static double GetMax(Matrix values)
    {
        int width = values.NumberOfColumns;

        int height = values.NumberOfRows;

        double result = double.NegativeInfinity;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (values[j, i] > result)
                {
                    result = values[j, i];
                }
            }
        }

        return result;
    }

    public static double GetMax(double[] values)
    {

        if (values.IsNullOrEmpty())
        {
            throw new MaptorZeroSizeArrayException();
        }
        
        double resultVlaue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            resultVlaue = Math.Max(resultVlaue, values[i]);
        }

        return resultVlaue;

    }

    public static int GetMax(int[] values)
    {

        //if (values.Length < 0)
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        int resultVlaue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            resultVlaue = Math.Max(resultVlaue, values[i]);
        }

        return resultVlaue;

    }

    public static TValue GetMax<TObject, TValue>(IEnumerable<TObject> array, Func<TObject, TValue> mapFunction) where TValue : IComparable<TValue>
    {
        IEnumerator<TObject> enumerator = array.GetEnumerator();

        enumerator.MoveNext();

        TValue result = mapFunction(enumerator.Current);

        foreach (TObject item in array)
        {
            TValue temp = mapFunction(item);

            if (result.CompareTo(temp) < 0)
            {
                result = temp;
            }
        }

        return result;
    }

    #endregion

    #region Minimum

    public static double GetMin(Matrix values)
    {
        int width = values.NumberOfColumns;

        int height = values.NumberOfRows;

        double result = double.PositiveInfinity;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (values[j, i] < result)
                {
                    result = values[j, i];
                }
            }
        }

        return result;
    }

    public static double GetMin(double[] values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double resultVlaue = values[0];

        for (int i = 1; i < values.Length; i++)
        {

            if (resultVlaue > values[i])
                resultVlaue = values[i];

        }

        return resultVlaue;

    }

    public static double GetMin(List<double> values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double resultVlaue = values[0];

        for (int i = 1; i < values.Count; i++)
        {
            if (resultVlaue > values[i])
                resultVlaue = values[i];
        }

        return resultVlaue;
    }

    public static int GetMin(int[] values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();


        int resultVlaue = values[0];

        for (int i = 1; i < values.Length; i++)
        {

            if (resultVlaue > values[i])
                resultVlaue = values[i];

        }

        return resultVlaue;

    }

    public static TValue GetMin<TObject, TValue>(IEnumerable<TObject> array, Func<TObject, TValue> mapFunction) where TValue : IComparable<TValue>
    {
        IEnumerator<TObject> enumerator = array.GetEnumerator();

        enumerator.MoveNext();

        TValue result = mapFunction(enumerator.Current);

        foreach (TObject item in array)
        {
            TValue temp = mapFunction(item);

            if (result.CompareTo(temp) > 0)
            {
                result = temp;
            }
        }

        return result;
    }
    #endregion

    #region Median

    #endregion

    #region Mode

    #endregion

    #region Sum

    public static double CalculateSum(double[] values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();


        double result = 0;

        foreach (double item in values)
        {
            result += item;
        }

        return result;
    }

    private static double CalculateSum(List<double> values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double result = 0;

        foreach (double item in values)
        {
            result += item;
        }

        return result;

    }

    public static double CalculateSum(Matrix values)
    {
        double result = 0;

        for (int i = 0; i < values.NumberOfRows; i++)
        {
            for (int j = 0; j < values.NumberOfColumns; j++)
            {
                result += values[i, j];
            }
        }

        return result;
    }

    #endregion

    #region Mean

    public static double CalculateMean(double[] values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        return Statistics.CalculateSum(values) / values.Length;
    }

    public static double CalculateMean(List<double> values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        return Statistics.CalculateSum(values) / values.Count;
    }

    public static double CalculateMean(Matrix values)
    {
        if (values is null)
        {
            throw new NotImplementedException();
        }

        return Statistics.CalculateSum(values) / (values.NumberOfColumns * values.NumberOfRows);
    }

    #endregion

    #region StandardDeviation & Variance

    public static double CalculateStandardDeviation(double[] values, VarianceCalculationMode mode = VarianceCalculationMode.Sample)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        return Math.Sqrt(Statistics.CalculateVariance(values, mode));
    }

    public static double CalculateStandardDeviation(List<double> values, VarianceCalculationMode mode = VarianceCalculationMode.Sample)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        return Math.Sqrt(Statistics.CalculateVariance(values, mode));
    }

    public static double CalculateStandardDeviation(Matrix values)
    {
        if (values is null)
            throw new MaptorZeroSizeArrayException();

        return Math.Sqrt(Statistics.CalculateVariance(values));
    }

    // ref for sample mode: https://stats.stackexchange.com/a/3934/289542
    public static double CalculateVariance(double[] values, VarianceCalculationMode mode = VarianceCalculationMode.Sample)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double result = 0;

        double mean = Statistics.CalculateMean(values);

        foreach (double item in values)
        {
            result += (item - mean) * (item - mean);
        }

        if (mode == VarianceCalculationMode.Sample)
        {
            return result / (values.Length - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / values.Length;
        }
        else
        {
            throw new NotImplementedException("Statistics > CalculateVariance");
        }

    }

    // ref for sample mode: https://stats.stackexchange.com/a/3934/289542
    public static double CalculateVariance(List<double> values, VarianceCalculationMode mode = VarianceCalculationMode.Sample)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double result = 0;

        double mean = Statistics.CalculateMean(values);

        foreach (double item in values)
        {
            result += (item - mean) * (item - mean);
        }


        if (mode == VarianceCalculationMode.Sample)
        {
            return result / (values.Count - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / values.Count;
        }
        else
        {
            throw new NotImplementedException("Statistics > CalculateVariance");
        }
    }

    // do not consider sample mode
    public static double CalculateVariance(Matrix values)
    {
        if (values is null)
            throw new MaptorZeroSizeArrayException();

        double result = 0;

        double mean = Statistics.CalculateMean(values);

        for (int i = 0; i < values.NumberOfRows; i++)
        {
            for (int j = 0; j < values.NumberOfColumns; j++)
            {
                result += (values[i, j] - mean) * (values[i, j] - mean);
            }
        }

        return result / (values.NumberOfColumns * values.NumberOfRows);
    }

    #endregion

    #region Covariance & Correlation


    public static double CalculateCovariance(double[] firstValues, double[] secondValues)
    {
        if (firstValues.IsNullOrEmpty() || secondValues.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        int length = firstValues.Length;

        if (length != secondValues.Length)
        {
            throw new NotImplementedException();
        }

        double firstMean = Statistics.CalculateMean(firstValues);

        double secondMean = Statistics.CalculateMean(secondValues);

        double result = 0;

        for (int i = 0; i < length; i++)
        {
            result += (firstValues[i] - firstMean) * (secondValues[i] - secondMean);
        }

        return Math.Sqrt(result / length);
    }

    public static double CalculateCovariance(Matrix firstValues, Matrix secondValues)
    {
        if (firstValues is null || secondValues is null)
            throw new MaptorZeroSizeArrayException();

        if (!Matrix.AreTheSameSize(firstValues, secondValues))
        {
            throw new NotImplementedException();
        }

        double result = 0;

        double firstMean = Statistics.CalculateMean(firstValues);

        double secondMean = Statistics.CalculateMean(secondValues);

        for (int i = 0; i < firstValues.NumberOfRows; i++)
        {
            for (int j = 0; j < firstValues.NumberOfColumns; j++)
            {
                result += (firstValues[i, j] - firstMean) * (secondValues[i, j] - secondMean);
            }
        }

        return result / (firstValues.NumberOfColumns * firstValues.NumberOfRows);
    }

    public static Matrix CalculateVarianceCovariance(Matrix[] values)
    {
        int numberOfArrays = values.Length;

        if (numberOfArrays < 0)
        {
            throw new MaptorZeroSizeArrayException();
        }

        for (int i = 0; i < numberOfArrays; i++)
        {
            if (!Matrix.AreTheSameSize(values[0], values[i]))
            {
                throw new NotImplementedException();
            }
        }

        Matrix result = new Matrix(numberOfArrays, numberOfArrays);

        for (int i = 0; i < numberOfArrays; i++)
        {
            for (int j = 0; j < numberOfArrays; j++)
            {
                if (i > j)
                {
                    result[i, j] = result[j, i];
                }
                else if (i == j)
                {
                    result[i, j] = CalculateVariance(values[i]);
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j]);
                }
            }
        }

        return result;
    }

    public static Matrix CalculateVarianceCovariance(double[][] values)
    {
        int numberOfArrays = values.Length;

        if (numberOfArrays < 0)
        {
            throw new MaptorZeroSizeArrayException();
        }

        int arrayLength = values[0].Length;

        if (arrayLength < 0)
        {
            throw new MaptorZeroSizeArrayException();
        }

        foreach (double[] item in values)
        {
            if (item.Length != arrayLength)
            {
                throw new NotImplementedException();
            }
        }

        Matrix result = new Matrix(numberOfArrays, numberOfArrays);

        for (int i = 0; i < numberOfArrays; i++)
        {
            for (int j = 0; j < numberOfArrays; j++)
            {
                if (i > j)
                {
                    result[i, j] = result[j, i];
                }
                else if (i == j)
                {
                    result[i, j] = CalculateVariance(values[i]);
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j]);
                }
            }
        }

        return result;
    }

    public static Matrix CalculateCorrelation(Matrix[] values)
    {
        int numberOfArrays = values.Length;

        if (numberOfArrays < 0)
        {
            throw new MaptorZeroSizeArrayException();
        }

        for (int i = 0; i < numberOfArrays; i++)
        {
            if (!Matrix.AreTheSameSize(values[0], values[i]))
            {
                throw new NotImplementedException();
            }
        }

        double[] variances = new double[numberOfArrays];

        for (int i = 0; i < numberOfArrays; i++)
        {
            variances[i] = Statistics.CalculateVariance(values[i]);
        }

        Matrix result = new Matrix(numberOfArrays, numberOfArrays);

        for (int i = 0; i < numberOfArrays; i++)
        {
            for (int j = 0; j < numberOfArrays; j++)
            {
                if (i > j)
                {
                    result[i, j] = result[j, i];
                }
                else if (i == j)
                {
                    result[i, j] = 1 / variances[i];
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j]) / (variances[i] * variances[j]);
                }
            }
        }

        return result;
    }

    public static Matrix CalculateCorrelation(double[][] values)
    {
        int numberOfArrays = values.Length;

        if (numberOfArrays < 0)
        {
            throw new MaptorZeroSizeArrayException();
        }

        int arrayLength = values[0].Length;

        if (arrayLength < 0)
        {
            throw new MaptorZeroSizeArrayException();
        }

        foreach (double[] item in values)
        {
            if (item.Length != arrayLength)
            {
                throw new NotImplementedException();
            }
        }

        double[] variances = new double[numberOfArrays];

        for (int i = 0; i < numberOfArrays; i++)
        {
            variances[i] = Statistics.CalculateVariance(values[i]);
        }

        Matrix result = new Matrix(numberOfArrays, numberOfArrays);

        for (int i = 0; i < numberOfArrays; i++)
        {
            for (int j = 0; j < numberOfArrays; j++)
            {
                if (i > j)
                {
                    result[i, j] = result[j, i];
                }
                else if (i == j)
                {
                    result[i, j] = 1 / variances[i];
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j]) / (variances[i] * variances[j]);
                }
            }
        }

        return result;
    }

    #endregion

    #region Other

    #endregion
}

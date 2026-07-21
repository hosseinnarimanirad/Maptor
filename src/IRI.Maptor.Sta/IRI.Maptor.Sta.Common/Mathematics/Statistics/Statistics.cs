using System;
using System.Collections.Generic;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Exceptions;

namespace IRI.Maptor.Sta.Mathematics;

public static class Statistics
{
    private static void ValidateNonEmpty(Matrix values, string paramName)
    {
        if (values is null)
            throw new ArgumentNullException(paramName);

        if (values.IsNull() || values.NumberOfColumns == 0 || values.NumberOfRows == 0)
            throw new MaptorZeroSizeArrayException();
    }

    #region Maximum

    public static double GetMax(Matrix values)
    {
        ValidateNonEmpty(values, nameof(values));

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
        
        double resultValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            resultValue = Math.Max(resultValue, values[i]);
        }

        return resultValue;

    }

    public static int GetMax(int[] values)
    {

        //if (values.Length < 0)
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        int resultValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            resultValue = Math.Max(resultValue, values[i]);
        }

        return resultValue;

    }

    public static double GetMax(List<double> values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double resultValue = values[0];

        for (int i = 1; i < values.Count; i++)
        {
            resultValue = Math.Max(resultValue, values[i]);
        }

        return resultValue;
    }

    public static TValue GetMax<TObject, TValue>(IEnumerable<TObject> array, Func<TObject, TValue> mapFunction) where TValue : IComparable<TValue>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        using (IEnumerator<TObject> enumerator = array.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new MaptorZeroSizeArrayException();

            TValue result = mapFunction(enumerator.Current);

            while (enumerator.MoveNext())
            {
                TValue temp = mapFunction(enumerator.Current);

                if (result.CompareTo(temp) < 0)
                {
                    result = temp;
                }
            }

            return result;
        }
    }

    #endregion

    #region Minimum

    public static double GetMin(Matrix values)
    {
        ValidateNonEmpty(values, nameof(values));

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

        double resultValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {

            if (resultValue > values[i])
                resultValue = values[i];

        }

        return resultValue;

    }

    public static double GetMin(List<double> values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        double resultValue = values[0];

        for (int i = 1; i < values.Count; i++)
        {
            if (resultValue > values[i])
                resultValue = values[i];
        }

        return resultValue;
    }

    public static int GetMin(int[] values)
    {
        if (values.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();


        int resultValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {

            if (resultValue > values[i])
                resultValue = values[i];

        }

        return resultValue;

    }

    public static TValue GetMin<TObject, TValue>(IEnumerable<TObject> array, Func<TObject, TValue> mapFunction) where TValue : IComparable<TValue>
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        using (IEnumerator<TObject> enumerator = array.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new MaptorZeroSizeArrayException();

            TValue result = mapFunction(enumerator.Current);

            while (enumerator.MoveNext())
            {
                TValue temp = mapFunction(enumerator.Current);

                if (result.CompareTo(temp) > 0)
                {
                    result = temp;
                }
            }

            return result;
        }
    }
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

    public static double CalculateSum(List<double> values)
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
        ValidateNonEmpty(values, nameof(values));

        double result = 0;

        // column-outer iteration matches the matrix's column-array storage
        for (int j = 0; j < values.NumberOfColumns; j++)
        {
            for (int i = 0; i < values.NumberOfRows; i++)
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
        ValidateNonEmpty(values, nameof(values));

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

    public static double CalculateStandardDeviation(Matrix values, VarianceCalculationMode mode = VarianceCalculationMode.Population)
    {
        return Math.Sqrt(Statistics.CalculateVariance(values, mode));
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
            if (values.Length < 2)
                throw new ArgumentException("Sample variance requires at least two values.", nameof(values));

            return result / (values.Length - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / values.Length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
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
            if (values.Count < 2)
                throw new ArgumentException("Sample variance requires at least two values.", nameof(values));

            return result / (values.Count - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / values.Count;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    // matrix overloads default to Population mode to preserve historical behavior
    public static double CalculateVariance(Matrix values, VarianceCalculationMode mode = VarianceCalculationMode.Population)
    {
        ValidateNonEmpty(values, nameof(values));

        double result = 0;

        double mean = Statistics.CalculateMean(values);

        for (int j = 0; j < values.NumberOfColumns; j++)
        {
            for (int i = 0; i < values.NumberOfRows; i++)
            {
                result += (values[i, j] - mean) * (values[i, j] - mean);
            }
        }

        int count = values.NumberOfColumns * values.NumberOfRows;

        if (mode == VarianceCalculationMode.Sample)
        {
            if (count < 2)
                throw new ArgumentException("Sample variance requires at least two values.", nameof(values));

            return result / (count - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / count;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    #endregion

    #region Covariance & Correlation


    public static double CalculateCovariance(double[] firstValues, double[] secondValues, VarianceCalculationMode mode = VarianceCalculationMode.Sample)
    {
        if (firstValues.IsNullOrEmpty() || secondValues.IsNullOrEmpty())
            throw new MaptorZeroSizeArrayException();

        int length = firstValues.Length;

        if (length != secondValues.Length)
        {
            throw new ArgumentException("Arrays must have the same length.", nameof(secondValues));
        }

        double firstMean = Statistics.CalculateMean(firstValues);

        double secondMean = Statistics.CalculateMean(secondValues);

        double result = 0;

        for (int i = 0; i < length; i++)
        {
            result += (firstValues[i] - firstMean) * (secondValues[i] - secondMean);
        }

        if (mode == VarianceCalculationMode.Sample)
        {
            if (length < 2)
                throw new ArgumentException("Sample covariance requires at least two values.", nameof(firstValues));

            return result / (length - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    // matrix overloads default to Population mode to preserve historical behavior
    public static double CalculateCovariance(Matrix firstValues, Matrix secondValues, VarianceCalculationMode mode = VarianceCalculationMode.Population)
    {
        ValidateNonEmpty(firstValues, nameof(firstValues));

        ValidateNonEmpty(secondValues, nameof(secondValues));

        if (!Matrix.AreTheSameSize(firstValues, secondValues))
        {
            throw new ArgumentException("Matrices must be the same size.", nameof(secondValues));
        }

        double result = 0;

        double firstMean = Statistics.CalculateMean(firstValues);

        double secondMean = Statistics.CalculateMean(secondValues);

        for (int j = 0; j < firstValues.NumberOfColumns; j++)
        {
            for (int i = 0; i < firstValues.NumberOfRows; i++)
            {
                result += (firstValues[i, j] - firstMean) * (secondValues[i, j] - secondMean);
            }
        }

        int count = firstValues.NumberOfColumns * firstValues.NumberOfRows;

        if (mode == VarianceCalculationMode.Sample)
        {
            if (count < 2)
                throw new ArgumentException("Sample covariance requires at least two values.", nameof(firstValues));

            return result / (count - 1);
        }
        else if (mode == VarianceCalculationMode.Population)
        {
            return result / count;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    // matrix overloads default to Population mode to preserve historical behavior
    public static Matrix CalculateVarianceCovariance(Matrix[] values, VarianceCalculationMode mode = VarianceCalculationMode.Population)
    {
        if (values.IsNullOrEmpty())
        {
            throw new MaptorZeroSizeArrayException();
        }

        int numberOfArrays = values.Length;

        for (int i = 0; i < numberOfArrays; i++)
        {
            if (!Matrix.AreTheSameSize(values[0], values[i]))
            {
                throw new ArgumentException("Matrices must be the same size.", nameof(values));
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
                    result[i, j] = CalculateVariance(values[i], mode);
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j], mode);
                }
            }
        }

        return result;
    }

    public static Matrix CalculateVarianceCovariance(double[][] values, VarianceCalculationMode mode = VarianceCalculationMode.Sample)
    {
        if (values.IsNullOrEmpty() || values[0].IsNullOrEmpty())
        {
            throw new MaptorZeroSizeArrayException();
        }

        int numberOfArrays = values.Length;

        int arrayLength = values[0].Length;

        foreach (double[] item in values)
        {
            if (item.Length != arrayLength)
            {
                throw new ArgumentException("Arrays must have the same length.", nameof(values));
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
                    result[i, j] = CalculateVariance(values[i], mode);
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j], mode);
                }
            }
        }

        return result;
    }

    // note: off-diagonal entries are NaN/Infinity when a series has zero variance
    public static Matrix CalculateCorrelation(Matrix[] values)
    {
        if (values.IsNullOrEmpty())
        {
            throw new MaptorZeroSizeArrayException();
        }

        int numberOfArrays = values.Length;

        for (int i = 0; i < numberOfArrays; i++)
        {
            if (!Matrix.AreTheSameSize(values[0], values[i]))
            {
                throw new ArgumentException("Matrices must be the same size.", nameof(values));
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
                    result[i, j] = 1;
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j]) / Math.Sqrt(variances[i] * variances[j]);
                }
            }
        }

        return result;
    }

    // note: off-diagonal entries are NaN/Infinity when a series has zero variance
    public static Matrix CalculateCorrelation(double[][] values)
    {
        if (values.IsNullOrEmpty() || values[0].IsNullOrEmpty())
        {
            throw new MaptorZeroSizeArrayException();
        }

        int numberOfArrays = values.Length;

        int arrayLength = values[0].Length;

        foreach (double[] item in values)
        {
            if (item.Length != arrayLength)
            {
                throw new ArgumentException("Arrays must have the same length.", nameof(values));
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
                    result[i, j] = 1;
                }
                else
                {
                    result[i, j] = CalculateCovariance(values[i], values[j]) / Math.Sqrt(variances[i] * variances[j]);
                }
            }
        }

        return result;
    }

    #endregion
}

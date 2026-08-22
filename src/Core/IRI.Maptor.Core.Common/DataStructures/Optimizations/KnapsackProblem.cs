using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
namespace IRI.Maptor.Core.Common.DataStructures.Optimizations;

// topic: CombinatorialOptimization
public static class KnapsackProblem
{
    static double[,] temp;

    public static double Calculate<T>(List<T> items, double weightLimit, Func<T, double> valueFunc, Func<T, double> weightFunc)
    {
        temp = new double[items.Count + 1, (int)weightLimit + 1];

        for (int i = 1; i < items.Count; i++)
        {
            for (int j = 0; j < (int)weightLimit; j++)
            {
                temp[i, j] = double.NaN;
            }
        }

        return Do(items, weightLimit, valueFunc, weightFunc);
    }

    public static double Do<T>(List<T> items, double weightLimit, Func<T, double> valueFunc, Func<T, double> weightFunc)
    {
        //List<T> result = new List<T>();

        //int currentIndex = items.Count - 1;

        //if (items.Count == 1)
        //{
        //    if (weightFunc(items[0]) < weightLimit)
        //    {
        //        result.Add(items[0]);
        //    }

        //    return result;
        //}

        for (int i = 1; i <= items.Count; i++)
        {
            for (int j = 0; j <= (int)weightLimit; j++)
            {
                if (j - (int)weightFunc(items[i - 1]) < 0)
                {
                    temp[i, j] = temp[i - 1, j];
                }
                else
                {
                    temp[i, j] = Math.Max(temp[i - 1, j], temp[i - 1, j - (int)weightFunc(items[i - 1])] + valueFunc(items[i - 1]));
                }
            }
        }

        return temp[items.Count, (int)(weightLimit)];
        //if (double.IsNaN(temp[items.Count - 2, (int)weightLimit]))
        //{
        //    List<T> first = Do(items.Take(items.Count - 1).ToList(),
        //                                weightLimit,
        //                                valueFunc, weightFunc);

        //    temp[items.Count - 1, (int)weightLimit] = CalculateValue(first, valueFunc);
        //}

        //if ((int)(weightLimit - weightFunc(items[items.Count - 1])) > 0)
        //{
        //    if (double.IsNaN(temp[items.Count - 1, (int)(weightLimit - weightFunc(items[items.Count - 1]))]))
        //    {
        //        List<T> second = Do(items.Take(items.Count - 1).ToList(),
        //                                   weightLimit - weightFunc(items[items.Count - 1]),
        //                                   valueFunc, weightFunc);

        //        temp[items.Count - 1, (int)weightLimit] = CalculateValue(second, valueFunc);
        //    }
        //}

        //if (weightLimit - weightFunc(items[items.Count - 1]) < 0)
        //{
        //    result.AddRange(items.Take(items.Count - 1).ToList());
        //}
        //else if (temp[items.Count - 1, (int)weightLimit] > temp[items.Count - 1, (int)(weightLimit - weightFunc(items[items.Count - 1]))])
        //{
        //    result.AddRange(items.Take(items.Count - 1).ToList());
        //}
        //else
        //{
        //    result.AddRange(items.Take(items.Count - 1).ToList());

        //    result.Add(items[items.Count - 1]);
        //}

        //return result;
    }

    public static double CalculateValue<T>(List<T> items, Func<T, double> valueFunc)
    {
        double result = 0;

        foreach (T item in items)
        {
            result += valueFunc(item);
        }

        return result;
    }
}

public static class KnapsackProblemFast
{
    static double[,] temp;

    public static double Calculate<T>(List<T> items, double weightLimit, Func<T, double> valueFunc, Func<T, double> weightFunc)
    {
        temp = new double[3, (int)weightLimit + 1];

        for (int i = 1; i < 3; i++)
        {
            for (int j = 0; j < (int)weightLimit; j++)
            {
                temp[i, j] = double.NaN;
            }
        }

        return Do(items, weightLimit, valueFunc, weightFunc);
    }

    public static double Do<T>(List<T> items, double weightLimit, Func<T, double> valueFunc, Func<T, double> weightFunc)
    {
        for (int i = 1; i <= items.Count; i++)
        {
            for (int j = 0; j <= (int)weightLimit; j++)
            {
                if (j - (int)weightFunc(items[i - 1]) < 0)
                {
                    temp[1, j] = temp[0, j];
                }
                else
                {
                    temp[1, j] = Math.Max(temp[0, j], temp[0, j - (int)weightFunc(items[i - 1])] + valueFunc(items[i - 1]));
                }
            }

            for (int j = 0; j <= (int)weightLimit; j++)
            {
                temp[0, j] = temp[1, j];
            }
        }

        return temp[1, (int)(weightLimit)];

    }
}

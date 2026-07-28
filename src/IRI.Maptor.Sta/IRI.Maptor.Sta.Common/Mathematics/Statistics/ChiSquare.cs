
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.Mathematics;

public static class ChiSquare
{

    public static double NominalCorrelation<T1, T2>(IEnumerable<(T1 class1, T2 class2)> input)
    {
        if (input == null || input.Any() == false)
        {
            return 0;
        }

        var numberOfTuples = input.Count();

        var firstDistinct = input.Select(i => i.class1).Distinct()?.ToList();

        var secondDistinct = input.Select(i => i.class2).Distinct()?.ToList();

        var firstCounts = input.GroupBy(i => i.class1).ToDictionary(i => i.Key, i => i.Count());

        var secondCounts = input.GroupBy(i => i.class2).ToDictionary(i => i.Key, i => i.Count());

        double[,] expectedFrequencies = new double[firstDistinct.Count, secondDistinct.Count];

        double[,] observedFrequencies = new double[firstDistinct.Count, secondDistinct.Count];

        var degreeOfFreedom = (firstDistinct.Count - 1) * (secondDistinct.Count - 1);

        double result = 0;

        for (int i = 0; i < firstDistinct.Count; i++)
        {
            for (int j = 0; j < secondDistinct.Count; j++)
            {
                var expectedFrequency = (firstCounts[firstDistinct[i]] * secondCounts[secondDistinct[j]]) / numberOfTuples;

                var observedFrequency = input.Count(z => z.class1.Equals(firstDistinct[i]) && z.class2.Equals(secondDistinct[j]));

                expectedFrequencies[i, j] = expectedFrequency; observedFrequencies[i, j] = observedFrequency;

                result += ((expectedFrequency - observedFrequency) * (expectedFrequency - observedFrequency)) / expectedFrequency;
            }
        }

        return result;
    }

    public static double NominalCorrelation<T1, T2>(List<ClassAttributeGroup<T1, T2>> input)
    {
        var classCounts = input.SelectMany(i => i.Classes).GroupBy(i => i.Class).ToDictionary(d => d.Key, d => d.Sum(c => c.Count));

        double result = 0;

        var totalNumber = classCounts.Sum(c => c.Value);

        for (int i = 0; i < input.Count; i++)
        {
            var ithAttributeCount = input[i].Classes.Sum(c => c.Count);

            for (int j = 0; j < input[i].Classes.Count; j++)
            {
                var currentClass = input[i].Classes[j];

                var observedFrequency = currentClass.Count;

                var ithClassCount = classCounts[currentClass.Class];

                var expectedFrequency = (ithAttributeCount * ithClassCount) / (double)totalNumber;

                if (expectedFrequency == 0 && observedFrequency == expectedFrequency)
                {
                    continue;
                }
                else
                {
                    result += ((observedFrequency - expectedFrequency) * (observedFrequency - expectedFrequency)) / expectedFrequency;
                }
            }
        }

        return result;
    }

    public static (double correlation, double df)? NominalCorrelation<T1, T2>(List<T1> firstAttribute, List<T2> secondAttribute)
    {
        if (firstAttribute == null || secondAttribute == null)
        {
            return null;
        }

        if (firstAttribute.Count != secondAttribute.Count)
        {
            throw new NotImplementedException("arrays must be the same size");
        }

        var numberOfTuples = firstAttribute.Count;

        var firstDistinct = firstAttribute.Distinct()?.ToList();

        var secondDistinct = secondAttribute.Distinct()?.ToList();

        var firstCounts = firstDistinct.Select(a => firstAttribute.Count(b => b.Equals(a))).ToList();

        var secondCounts = secondDistinct.Select(a => secondAttribute.Count(b => b.Equals(a))).ToList();

        double[,] expectedFrequencies = new double[firstDistinct.Count, secondDistinct.Count];

        double[,] observedFrequencies = new double[firstDistinct.Count, secondDistinct.Count];

        var zip = firstAttribute.Zip(secondAttribute, (f, s) => (f, s));

        var degreeOfFreedom = (firstDistinct.Count - 1) * (secondDistinct.Count - 1);

        double result = 0;

        for (int i = 0; i < firstDistinct.Count; i++)
        {
            for (int j = 0; j < secondDistinct.Count; j++)
            {
                var expectedFrequency = (firstCounts[i] * secondCounts[j]) / numberOfTuples;

                var observedFrequency = zip.Count(z => z.f.Equals(firstDistinct[i]) && z.s.Equals(secondDistinct[j]));

                expectedFrequencies[i, j] = expectedFrequency; observedFrequencies[i, j] = observedFrequency;

                if (expectedFrequency == 0)
                {
                    continue;
                }

                result += ((expectedFrequency - observedFrequency) * (expectedFrequency - observedFrequency)) / expectedFrequency;
            }
        }

        return (correlation: result, df: degreeOfFreedom);

    }

    //public static double NumericPearsonCoefficient(List<double> firstAttribute, List<double> secondAttribute)
    //{
    //    if (firstAttribute == null || secondAttribute == null)
    //    {
    //        return 0;
    //    }

    //    if (firstAttribute.Count != secondAttribute.Count)
    //    {
    //        throw new NotImplementedException("arrays must be the same size");
    //    }

    //    var numberOfTuples = firstAttribute.Count;

    //    var firstMean = Statistics.CalculateMean(firstAttribute);

    //    var secondMean = Statistics.CalculateMean(secondAttribute);

    //    var firstSd = Statistics.CalculateStandardDeviation(firstAttribute);

    //    var secondSd = Statistics.CalculateStandardDeviation(secondAttribute);

    //    double result = 0;

    //    for (int i = 0; i < numberOfTuples; i++)
    //    {
    //        result += (firstAttribute[i] - firstMean) * (secondAttribute[i] - secondMean);
    //    }

    //    return result / (numberOfTuples * firstSd * secondSd);
    //}

    //public static double GammaLowerRegularized(double a, double x)
    //{

    //}
}

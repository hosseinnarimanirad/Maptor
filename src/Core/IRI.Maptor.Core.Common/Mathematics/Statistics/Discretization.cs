using IRI.Maptor.Core.Common.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Mathematics;

public static class Discretization
{
    public static List<ClassAttributeGroup<T1, T2>> ChiMergeDiscretize<T1, T2>(IEnumerable<(T1 attribute, T2 @class)> input, int maxClasses)
    {
        if (input == null || input.Any() == false)
        {
            throw new NotImplementedException();
        }

        input.GroupBy(i => i.attribute).ToList();

        var intervals = input.GroupBy(i => i.attribute)
                            .OrderBy(i => i.Key)
                            .Select(i => new ClassAttributeGroup<T1, T2>(new List<T1> { i.Key }, i.Select(c => c.@class)?.ToList()))
                            ?.ToList();

        return ChiMergeDiscretize(intervals, maxClasses);
    }

    public static List<ClassAttributeGroup<T1, T2>>? ChiMergeDiscretize<T1, T2>(List<ClassAttributeGroup<T1, T2>>? input, int maxClasses)
    {
        var numberOfTuples = input?.Count ?? 0;

        if (numberOfTuples == 1)
        {
            return input;
        }

        List<double> chiValues = new List<double>();

        for (int i = 0; i < numberOfTuples - 1; i++)
        {
            //calculate ChiSquare for adjacent intervals
            var temporaryInput = new List<ClassAttributeGroup<T1, T2>>() { input![i], input[i + 1] };

            chiValues.Add(ChiSquare.NominalCorrelation(temporaryInput));
        }

        if (numberOfTuples <= maxClasses)
        {
            return input;
        }
        //if (chiValues.All(c => c > threshold))
        //{
        //    return input;
        //}

        List<ClassAttributeGroup<T1, T2>> result = new List<ClassAttributeGroup<T1, T2>>();

        int index = 0;

        var minChiValue = chiValues.Min();

        do
        {
            var lastItem = chiValues.Select((cv, i) => (cv, i)).Skip(index).TakeWhile((item, i) => item.cv <= minChiValue /* < threshold*/);

            int lastIndexToMerge = index;

            if (lastItem?.Any() == true)
            {
                lastIndexToMerge = lastItem.Select(x => x.i).Last() + 1;

                var merged = ClassAttributeGroup<T1, T2>.Merge(input.Skip(index).Take(lastIndexToMerge - index + 1));

                result.Add(merged);

                index = lastIndexToMerge + 1;
            }
            else
            {
                result.Add(input![index]);

                index++;
            }

        } while (index <= chiValues.Count);

        return ChiMergeDiscretize(result.OrderBy(i => i.Key)?.ToList(), maxClasses);
    }
}

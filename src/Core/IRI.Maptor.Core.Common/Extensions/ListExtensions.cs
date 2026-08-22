using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class ListExtensions
{
    public static double? DotProduct(this List<double> first, double[] second)
    {
        if (first == null || second == null)
            return null;

        if (first.Count != second.Length)
            throw new NotImplementedException("ArrayExtensions > Multiply");

        double? result = 0;

        for (int i = 0; i < first.Count; i++)
        {
            result += first[i] * second[i];
        }

        return result;
    }
}

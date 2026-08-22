using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class ArrayExtensions
{
    public static T[] RemoveAt<T>(this T[] source, int index)
    {
        T[] dest = new T[source.Length - 1];
        if (index > 0)
            Array.Copy(source, 0, dest, 0, index);

        if (index < source.Length - 1)
            Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

        return dest;
    }

    public static double? DotProduct(this double[] first, double[] second)
    {
        if (first == null || second == null)
            return null;

        if (first.Length != second.Length)
            throw new NotImplementedException("ArrayExtensions > Multiply");

        double? result = 0;

        for (int i = 0; i < first.Length; i++)
        {
            result += first[i] * second[i];
        }

        return result;
    }
}

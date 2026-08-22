using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class IEnumerableExtension
{
    //public static bool IsNotNullNorEmpty<T>(this IEnumerable<T> enumerable)
    //{
    //    return enumerable?.Any() == true;
    //}

    //public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    //{
    //    return enumerable?.Any() != true;
    //}

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable is null || !enumerable.Any();
    }
}

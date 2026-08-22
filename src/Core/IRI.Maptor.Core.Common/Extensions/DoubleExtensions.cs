using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class DoubleExtensions
{
    public static bool IsNormal(this double value)
    {
        return !(double.IsNaN(value) || double.IsInfinity(value));
    }

    public static double AsDouble(this double? value)
    {
        return value.HasValue ? value.Value : double.NaN;
    }

    public static string ToInvariantString(this double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string AsExactString(this double value)
    {
        //decimal.Parse("6378249.145").ToString("G17")---------"6378249.145"

        //double.Parse("6378249.145").ToString("G17")---------"6378249.1449999996"

        return ((decimal)value).ToString("G17", CultureInfo.InvariantCulture);
    }

    public static bool AreEqual(this double first, double second)
    {
        return Math.Abs(first - second) < 1E-13;
    }
}

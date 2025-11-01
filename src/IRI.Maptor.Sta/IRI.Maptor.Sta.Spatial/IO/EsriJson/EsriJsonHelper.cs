using IRI.Maptor.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Spatial.Primitives.Esri;

public static class EsriJsonHelper
{
    internal static string PointArrayToString(double?[][] pointArray)
    {
        //return $"{pointArray.Select(i => string.Join(", ", string.Join(" ", i)))}";
        return $"({String.Join(",", pointArray.Select(i => string.Join(" ", i.ToStringOrNull())))})";
    }

    internal static string ToStringOrNull(this double? value, bool returnNullString)
    {
        if (!value.HasValue || !value.Value.IsNormal())
        {
            if (returnNullString)
            {
                return "NULL";
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            return value.Value.ToString();
        }
    }

    internal static string ToStringOrNull(this double?[] point)
    {
        if (!(point?.Length > 0))
        {
            return null;
        }

        var xValue = point[0].ToStringOrNull(false);

        var yValue = point[1].ToStringOrNull(false);

        if (string.IsNullOrEmpty(xValue) || string.IsNullOrEmpty(yValue))
        {
            return null;
        }

        if (point.Length == 2)
        {
            return $"{xValue} {yValue}";
        }
        else if (point.Length == 3)
        {
            return $"{xValue} {yValue} {point[2].ToStringOrNull(false)}";
        }
        else if (point.Length == 4)
        {
            var mValue = point[2].ToStringOrNull(false);
            var zValue = point[3].ToStringOrNull(mValue.Length > 0);

            return $"{xValue} {yValue} {zValue} {mValue}";
        }
        else
        {
            throw new NotImplementedException();
        }
    }


}

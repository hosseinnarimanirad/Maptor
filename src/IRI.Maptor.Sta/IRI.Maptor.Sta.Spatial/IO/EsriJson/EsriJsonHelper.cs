using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.IO.OgcSFA;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Spatial.IO.EsriJson;

public static class EsriJsonHelper
{
    internal static string PointArrayToString(double?[][] pointArray, bool isRing/*, bool shouldBeClockwiseRing*/)
    { 
        //return $"{pointArray.Select(i => string.Join(", ", string.Join(" ", i)))}";
        return $"({string.Join(",", pointArray.Select(i => string.Join(" ", i.ToStringOrNull())))})";
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

    internal static string? ToStringOrNull(this double?[] point)
    {
        if (point is null || point.Length < 0)
            return null;

        var xValue = point[0].ToStringOrNull(false);

        var yValue = point[1].ToStringOrNull(false);

        if (string.IsNullOrEmpty(xValue) || string.IsNullOrEmpty(yValue))
            return null;

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

    internal static string PointToWkt(EsriJsonGeometry geometry)
    {
        var xValue = geometry.X.ToStringOrNull(false);

        var yValue = geometry.Y.ToStringOrNull(false);

        if (string.IsNullOrEmpty(xValue) || string.IsNullOrEmpty(yValue))
            return WktConstants.EmptyPoint;

        var mValue = geometry.M.ToStringOrNull(false);

        var zValue = geometry.Z.ToStringOrNull(mValue.Length > 0);

        if (string.IsNullOrEmpty(zValue) && string.IsNullOrEmpty(mValue))
        {
            return FormattableString.Invariant($"POINT({xValue.ToString(CultureInfo.InvariantCulture)} {yValue})");
        }
        else
        {
            return FormattableString.Invariant($"POINT({xValue} {yValue} {zValue} {mValue})");
        }
    }

    internal static string MultiPointToWkt(EsriJsonGeometry geometry)
    {
        if (geometry is null || geometry.Points.IsNullOrEmpty())
            return WktConstants.EmptyMultiPoint;

        return FormattableString.Invariant($"MULTIPOINT{EsriJsonHelper.PointArrayToString(geometry.Points, isRing: false)}");
    }

    internal static string PolylineToWkt(EsriJsonGeometry geometry)
    {
        if (geometry is null || geometry.Paths == null || geometry.Paths.Length == 0)
            return WktConstants.EmptyLineString;

        var paths = geometry.Paths;

        var validPaths = paths.Where(path => path != null && path.Length >= 2).ToArray();

        if (validPaths.Length == 0)
            return WktConstants.EmptyLineString;

        if (validPaths.Length == 1)
        {
            return FormattableString.Invariant($"LINESTRING{PointArrayToString(geometry.Paths[0], isRing: false)}");
        }
        else
        {
            return FormattableString.Invariant($"MULTILINESTRING({string.Join(", ", validPaths.Select(l => PointArrayToString(l, isRing: false)))})");
        }

    }

    internal static string PolygonToWkt(EsriJsonGeometry geometry)
    {
        if (geometry is null || geometry.Rings == null || geometry.Rings.Length == 0)
            return WktConstants.EmptyPolygon;

        var rings = geometry.Rings;

        if (rings.Length == 1)
        {
            return FormattableString.Invariant($"POLYGON({PointArrayToString(geometry.Rings[0], isRing: true)})");
        }
        else
        {
            return FormattableString.Invariant($"MULTIPOLYGON({string.Join(", ", geometry.Rings.Select(i => $"({PointArrayToString(i, isRing: true)})"))})");
        }
    }
}

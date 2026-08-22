using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.IO.OgcSFA;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.IO.EsriJson;

public static class EsriJsonHelper
{
    internal static string PointArrayToString(double?[][] pointArray, bool isRing, bool shouldBeClockwiseRing)
    {
        if (pointArray is null)
            return string.Empty;

        var validIndices = pointArray
            .Select((p, idx) => new { Point = p, Index = idx })
            .Where(x => x.Point != null && x.Point.Length >= 2 && x.Point[0].HasValue && x.Point[1].HasValue)
            .Select(x => x.Index)
            .ToList();

        if (validIndices.Count == 0)
            return string.Empty;

        // If orientation correction is needed, we will build a new ordered list of points
        List<double?[]> orderedPoints;

        if (isRing && validIndices.Count >= 3)
        {
            // Extract X,Y for orientation check (only from valid points)
            var pointsForOrientation = validIndices
                .Select(i => new IRI.Maptor.Core.Common.Primitives.Point(pointArray[i][0]!.Value, pointArray[i][1]!.Value))
                .ToList();

            bool isClockwise = SpatialUtility.IsClockwise(pointsForOrientation);

            if ((shouldBeClockwiseRing && !isClockwise) ||
                (!shouldBeClockwiseRing && isClockwise))
            {
                // Reverse the order of valid points (preserving all coordinates)
                validIndices.Reverse();
            }
        }

        // Build the final list of points in the required order (including Z/M)
        orderedPoints = validIndices.Select(i => pointArray[i]).ToList();

        return $"({string.Join(",", orderedPoints.Select(i => string.Join(" ", i.ToStringOrNull())))})";
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

        return FormattableString.Invariant($"MULTIPOINT{EsriJsonHelper.PointArrayToString(geometry.Points, isRing: false, false)}");
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
            return FormattableString.Invariant($"LINESTRING{PointArrayToString(geometry.Paths[0], isRing: false, false)}");
        }
        else
        {
            return FormattableString.Invariant($"MULTILINESTRING({string.Join(", ", validPaths.Select(l => PointArrayToString(l, isRing: false, false)))})");
        }

    }

    internal static string PolygonToWkt(EsriJsonGeometry geometry)
    {
        if (geometry is null || geometry.Rings == null || geometry.Rings.Length == 0)
            return WktConstants.EmptyPolygon;

        var rings = geometry.Rings;

        if (rings.Length == 1)
        {
            return FormattableString.Invariant(
                $"POLYGON({PointArrayToString(geometry.Rings[0], isRing: true, false)})");
        }
        else
        {
            return FormattableString.Invariant(
                $"MULTIPOLYGON({string.Join(", ", geometry.Rings.Select((i, index) => $"({PointArrayToString(i, isRing: true, index != 0)})"))})");
        }
    }
}

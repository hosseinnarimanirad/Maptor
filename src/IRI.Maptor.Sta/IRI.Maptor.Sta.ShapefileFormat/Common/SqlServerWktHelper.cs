using System.Globalization;
using System.Text;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Sta.ShapefileFormat;

internal static class SqlServerWktHelper
{
    internal static string SinglePointElementToWkt(EsriPoint point)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17}", point.X, point.Y);
    }

    internal static string PointMElementToWkt(EsriPoint point, double measure)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} NULL {2:G17}", point.X, point.Y, measure);
    }

    internal static string PointZElementToWkt(EsriPoint point, double zValue, double measure)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17} {3}", point.X, point.Y, zValue, measure == EsriConstants.NoDataValue ? "NULL" : measure.ToString("G17"));
    }

    internal static string PointGroupElementToWkt(EsriPoint[] points)
    {
        StringBuilder result = new StringBuilder("(");

        int numberOfPoints = points.Length;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            result.Append(string.Format("{0},", SinglePointElementToWkt(points[i])));
        }

        result.Append(string.Format("{0})", SinglePointElementToWkt(points[numberOfPoints - 1])));

        return result.ToString();
    }

    internal static string PointMGroupElementToWkt(EsriPoint[] points, double[] measures)
    {
        StringBuilder result = new StringBuilder("(");

        int numberOfPoints = points.Length;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            result.Append(string.Format("{0},", PointMElementToWkt(points[i], measures[i])));
        }

        result.Append(string.Format("{0})", PointMElementToWkt(points[numberOfPoints - 1], measures[numberOfPoints - 1])));

        return result.ToString();
    }

    internal static string PointZGroupElementToWkt(EsriPoint[] points, double[] zValues, double[] measures)
    {
        StringBuilder result = new StringBuilder("(");

        int numberOfPoints = points.Length;

        for (int i = 0; i < numberOfPoints - 1; i++)
        {
            result.Append(string.Format("{0},", PointZElementToWkt(points[i], zValues[i], measures[i])));
        }

        result.Append(string.Format("{0})", PointZElementToWkt(points[numberOfPoints - 1], zValues[numberOfPoints - 1], measures[numberOfPoints - 1])));

        return result.ToString();
    }
}

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.ShapefileFormat.EsriType;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat;

static class ShapeHelper
{
    internal static List<Point> GetPoints(EsriPointCollection shape, int startIndex)
    {
        int partNumber = Array.IndexOf(shape.Parts, startIndex);

        int lastIndex = (partNumber == shape.NumberOfParts - 1) ? shape.NumberOfPoints - 1 : shape.Parts[partNumber + 1] - 1;

        //do not add last point for polygon
        if (shape is EsriPolygon || shape is EsriPolygonM || shape is EsriPolygonZ)
        {
            lastIndex = lastIndex - 1;
        }

        var count = lastIndex - startIndex + 1;

        //Point[] result = new Point[lastIndex - startIndex + 1];
        List<Point> result = new List<Point>(count);

        for (int i = 0; i < count; i++)
        {
            //result[i] = new Point(shape.Points[startIndex + i].X, shape.Points[startIndex + i].Y);
            result.Add(new Point(shape.Points[startIndex + i].X, shape.Points[startIndex + i].Y));
        }

        return result;
    }

    internal static EsriPoint[] GetEsriPoints(EsriPointCollection shape, int startIndex)
    {
        int partNumber = Array.IndexOf(shape.Parts, startIndex);

        //int startIndex = shape.Parts[partNumber];

        int lastIndex = (partNumber == shape.NumberOfParts - 1) ? shape.NumberOfPoints - 1 : shape.Parts[partNumber + 1] - 1;

        EsriPoint[] result = new EsriPoint[lastIndex - startIndex + 1];

        Array.ConstrainedCopy(shape.Points, startIndex, result, 0, result.Length);

        return result;
    }

    internal static double[] GetMeasures(EsriPointMCollection shape, int startIndex)
    {
        int partNumber = Array.IndexOf(shape.Parts, startIndex);

        //int startIndex = shape.Parts[partNumber];

        int lastIndex = (partNumber == shape.NumberOfParts - 1) ? shape.NumberOfPoints - 1 : shape.Parts[partNumber + 1] - 1;

        double[] result = new double[lastIndex - startIndex + 1];

        Array.ConstrainedCopy(shape.Measures, startIndex, result, 0, result.Length);

        return result;
    }

    internal static double[] GetZValues(EsriPointZCollection shape, int startIndex)
    {
        int partNumber = Array.IndexOf(shape.Parts, startIndex);

        //int startIndex = shape.Parts[partNumber];

        int lastIndex = (partNumber == shape.NumberOfParts - 1) ? shape.NumberOfPoints - 1 : shape.Parts[partNumber + 1] - 1;

        double[] result = new double[lastIndex - startIndex + 1];

        Array.ConstrainedCopy(shape.ZValues, startIndex, result, 0, result.Length);

        return result;
    }
}

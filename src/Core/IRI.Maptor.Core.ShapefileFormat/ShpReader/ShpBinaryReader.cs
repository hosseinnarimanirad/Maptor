using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.ShapefileFormat.EsriType;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.ShapefileFormat.ShpReader;

internal static class ShpBinaryReader
{
    internal static BoundingBox ReadBoundingBox(BinaryReader reader)
    {
        double xMin = reader.ReadDouble();

        double yMin = reader.ReadDouble();

        double xMax = reader.ReadDouble();

        double yMax = reader.ReadDouble();

        return new BoundingBox(xMin, yMin, xMax, yMax);
    }

    internal static BoundingBox ReadBoundingBox(byte[] byteArray, int offset)
    {
        double xMin = BitConverter.ToDouble(byteArray, ShapeConstants.DoubleSize * 0 + offset);

        double yMin = BitConverter.ToDouble(byteArray, ShapeConstants.DoubleSize * 1 + offset);

        double xMax = BitConverter.ToDouble(byteArray, ShapeConstants.DoubleSize * 2 + offset);

        double yMax = BitConverter.ToDouble(byteArray, ShapeConstants.DoubleSize * 3 + offset);

        return new BoundingBox(xMin, yMin, xMax, yMax);
    }

    internal static EsriPoint[] ReadPoints(BinaryReader reader, int numberOfPoints, int srid)
    {
        EsriPoint[] result = new EsriPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            double tempX = reader.ReadDouble();

            double tempY = reader.ReadDouble();

            result[i] = new EsriPoint(tempX, tempY, srid);
        }

        return result;
    }

    internal static EsriPoint[] ReadPoints(byte[] byteArray, int offset, int numberOfPoints, int srid)
    {
        EsriPoint[] result = new EsriPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            double tempX = BitConverter.ToDouble(byteArray, 2 * i * ShapeConstants.DoubleSize + offset);

            double tempY = BitConverter.ToDouble(byteArray, (2 * i + 1) * ShapeConstants.DoubleSize + offset);

            result[i] = new EsriPoint(tempX, tempY, srid);
        }

        return result;
    }

    internal static void ReadValues(BinaryReader reader, int numberOfPoints, out double min, out double max, out double[] values)
    {
        min = reader.ReadDouble();

        max = reader.ReadDouble();

        values = new double[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            values[i] = reader.ReadDouble();
        }
    }

    internal static void ReadValues(byte[] byteArray, int offset, int numberOfPoints, out double min, out double max, out double[] values)
    {
        min = BitConverter.ToDouble(byteArray, 0 * ShapeConstants.DoubleSize + offset);

        max = BitConverter.ToDouble(byteArray, 1 * ShapeConstants.DoubleSize + offset);

        values = new double[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            values[i] = BitConverter.ToDouble(byteArray, (i + 2) * ShapeConstants.DoubleSize + offset);
        }
    }

}

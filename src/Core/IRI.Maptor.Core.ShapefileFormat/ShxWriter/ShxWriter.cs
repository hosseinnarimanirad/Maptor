
using IRI.Maptor.Core.ShapefileFormat.EsriType;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Writer;

internal static class ShxWriter
{

    public static void Write(string fileName, IEsriShapeCollection shapes, EsriShapeType shapeType, bool overwrite = false)
    {
        System.IO.MemoryStream shxWriter = new System.IO.MemoryStream();

        int offset = 50;

        foreach (EsriShapeBase item in shapes)
        {
            shxWriter.Write(WriteHeaderToByte(offset, item.ContentLength), 0, 2 * ShapeConstants.IntegerSize);

            offset += item.ContentLength + 4;
        }

        System.IO.MemoryStream writer = new System.IO.MemoryStream();

        int fileLength = (int)shxWriter.Length / 2 + 50;

        writer.Write(WriteMainHeader(shapes, fileLength, shapeType), 0, 100);

        writer.Write(shxWriter.ToArray(), 0, (int)shxWriter.Length);

        //var mode = overwrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew;
        var mode = Shapefile.GetMode(fileName, overwrite);

        System.IO.FileStream stream = new System.IO.FileStream(fileName, mode);

        writer.WriteTo(stream);

        shxWriter.Close();

        writer.Close();

        stream.Close();
    }

    internal static byte[] WriteMainHeader(IEsriShapeCollection shapes, int fileLength, EsriShapeType shapeType)
    {
        return ShpWriter.WriteMainHeader(shapes, fileLength, shapeType);
        //System.IO.MemoryStream result = new System.IO.MemoryStream();

        //result.Write(IRI.Maptor.Infrastructure.IO.Binary.Int32ToBigEndianOrderedBytes(ShapeConstants.FileCode), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        //result.Write(IRI.Maptor.Infrastructure.IO.Binary.Int32ToBigEndianOrderedBytes(fileLength), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(ShapeConstants.Version), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes((int)shapeType), 0, ShapeConstants.IntegerSize);

        //result.Write(System.BitConverter.GetBytes(MapStatistics.GetMinX(shapes)), 0, ShapeConstants.DoubleSize);

        //result.Write(System.BitConverter.GetBytes(MapStatistics.GetMinY(shapes)), 0, ShapeConstants.DoubleSize);

        //result.Write(System.BitConverter.GetBytes(MapStatistics.GetMaxX(shapes)), 0, ShapeConstants.DoubleSize);

        //result.Write(System.BitConverter.GetBytes(MapStatistics.GetMaxY(shapes)), 0, ShapeConstants.DoubleSize);


        //double tempValue = MapStatistics.GetMinZ(shapes);

        //if (double.IsNaN(tempValue)) { tempValue = 0; }

        //result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);


        //tempValue = MapStatistics.GetMaxZ(shapes);

        //if (double.IsNaN(tempValue)) { tempValue = 0; }

        //result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);


        //tempValue = MapStatistics.GetMinM(shapes);

        //if (double.IsNaN(tempValue)) { tempValue = 0; }

        //result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);


        //tempValue = MapStatistics.GetMaxM(shapes);

        //if (double.IsNaN(tempValue)) { tempValue = 0; }

        //result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);

        //return result.ToArray();
    }

    internal static byte[] WriteHeaderToByte(int offset, int contentLength)
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(IRI.Maptor.Core.Common.Helpers.StreamHelper.Int32ToBigEndianOrderedBytes(offset), 0, ShapeConstants.IntegerSize);

        result.Write(IRI.Maptor.Core.Common.Helpers.StreamHelper.Int32ToBigEndianOrderedBytes(contentLength), 0, ShapeConstants.IntegerSize);

        return result.ToArray();
    }

    //internal static byte[] WriteBoundingBoxToByte(IShape shape)
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    result.Write(System.BitConverter.GetBytes(shape.MinX), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(shape.MinY), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(shape.MaxX), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(shape.MaxY), 0, ShapeConstants.DoubleSize);

    //    return result.ToArray();
    //}

    //internal static byte[] WritePointsToByte(Point[] values)
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    foreach (Point item in values)
    //    {
    //        result.Write(System.BitConverter.GetBytes(item.X), 0, ShapeConstants.DoubleSize);

    //        result.Write(System.BitConverter.GetBytes(item.Y), 0, ShapeConstants.DoubleSize);
    //    }

    //    return result.ToArray();
    //}

    //internal static byte[] WriteAdditionalData(double minValue, double maxValue, double[] values)
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    result.Write(System.BitConverter.GetBytes(minValue), 0, ShapeConstants.DoubleSize);

    //    result.Write(System.BitConverter.GetBytes(maxValue), 0, ShapeConstants.DoubleSize);

    //    foreach (double item in values)
    //    {
    //        result.Write(System.BitConverter.GetBytes(item), 0, ShapeConstants.DoubleSize);
    //    }

    //    return result.ToArray();
    //}

}

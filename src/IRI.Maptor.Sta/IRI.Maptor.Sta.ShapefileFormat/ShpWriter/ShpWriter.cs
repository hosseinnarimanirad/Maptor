// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;

using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Sta.ShapefileFormat.Writer;

public static class ShpWriter
{
   
    internal static byte[] WriteMainHeader(IEsriShapeCollection shapes, int fileLength, EsriShapeType shapeType)
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.Int32ToBigEndianOrderedBytes(ShapeConstants.FileCode), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(0), 0, ShapeConstants.IntegerSize);

        result.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.Int32ToBigEndianOrderedBytes(fileLength), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(ShapeConstants.Version), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes((int)shapeType), 0, ShapeConstants.IntegerSize);

        result.Write(System.BitConverter.GetBytes(shapes.MainHeader.XMin), 0, ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(shapes.MainHeader.YMin), 0, ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(shapes.MainHeader.XMax), 0, ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(shapes.MainHeader.YMax), 0, ShapeConstants.DoubleSize);


        double tempValue = shapes.MainHeader.ZMin;

        if (double.IsNaN(tempValue)) { tempValue = 0; }

        result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);


        tempValue = shapes.MainHeader.ZMax;

        if (double.IsNaN(tempValue)) { tempValue = 0; }

        result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);


        tempValue = shapes.MainHeader.MMin;

        if (double.IsNaN(tempValue)) { tempValue = 0; }

        result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);


        tempValue = shapes.MainHeader.MMax;

        if (double.IsNaN(tempValue)) { tempValue = 0; }

        result.Write(System.BitConverter.GetBytes(tempValue), 0, ShapeConstants.DoubleSize);

        return result.ToArray();
    }

    internal static byte[] WriteHeaderToByte(int recordNumber, EsriShapeBase shape)
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.Int32ToBigEndianOrderedBytes(recordNumber), 0, ShapeConstants.IntegerSize);

        result.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.Int32ToBigEndianOrderedBytes(shape.ContentLength), 0, ShapeConstants.IntegerSize);

        return result.ToArray();
    }

    internal static byte[] WriteBoundingBoxToByte(EsriPointCollection shape)
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(System.BitConverter.GetBytes(shape.MinimumBoundingBox.XMin), 0, ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(shape.MinimumBoundingBox.YMin), 0, ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(shape.MinimumBoundingBox.XMax), 0, ShapeConstants.DoubleSize);

        result.Write(System.BitConverter.GetBytes(shape.MinimumBoundingBox.YMax), 0, ShapeConstants.DoubleSize);

        return result.ToArray();
    }

    //internal static byte[] WritePointsToByte(EsriPoint[] values)
    //{
    //    System.IO.MemoryStream result = new System.IO.MemoryStream();

    //    foreach (EsriPoint item in values)
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

    internal static byte[] CheckNoDataAndGetByteValue(double value)
    {
        return BitConverter.GetBytes(double.IsNaN(value) ? EsriConstants.NoDataValue : value);
    }

    internal static byte[] WritePointsToByte(EsriPoint[] values)
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        foreach (EsriPoint item in values)
        {
            result.Write(CheckNoDataAndGetByteValue(item.X), 0, ShapeConstants.DoubleSize);

            result.Write(CheckNoDataAndGetByteValue(item.Y), 0, ShapeConstants.DoubleSize);
        }

        return result.ToArray();
    }

    internal static byte[] WriteAdditionalData(double minValue, double maxValue, double[] values)
    {
        System.IO.MemoryStream result = new System.IO.MemoryStream();

        result.Write(CheckNoDataAndGetByteValue(minValue), 0, ShapeConstants.DoubleSize);

        result.Write(CheckNoDataAndGetByteValue(maxValue), 0, ShapeConstants.DoubleSize);

        foreach (double item in values)
        {
            result.Write(CheckNoDataAndGetByteValue(item), 0, ShapeConstants.DoubleSize);
        }

        return result.ToArray();
    }
}

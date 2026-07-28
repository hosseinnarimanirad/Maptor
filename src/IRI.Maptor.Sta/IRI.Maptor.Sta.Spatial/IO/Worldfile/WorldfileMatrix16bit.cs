using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.IO;

public class WorldfileMatrix16bit : Worldfile
{
    Int16[,] _values;

    public int ImageHeight => _values?.GetLength(0) ?? 0;

    public int ImageWidth => _values?.GetLength(1) ?? 0;

    public WorldfileMatrix16bit()
    {

    }

    public WorldfileMatrix16bit(Matrix data, double xPixelSize, double yPixelSize, Point centerOfUpperLeftPixel)
        : this(data, xPixelSize, yPixelSize, 0, 0, centerOfUpperLeftPixel)
    {

    }

    public WorldfileMatrix16bit(Matrix data, double xPixelSize, double yPixelSize, double xRotation, double yRotation, Point centerOfUpperLeftPixel)
        : base(xPixelSize: xPixelSize, yPixelSize: yPixelSize, xRotation: xRotation, yRotation: yRotation, centerOfUpperLeftPixel: centerOfUpperLeftPixel)
    {
        _values = new short[data.NumberOfRows, data.NumberOfColumns];

        for (int i = 0; i < data.NumberOfRows; i++)
        {
            for (int j = 0; j < data.NumberOfColumns; j++)
            {
                _values[i, j] = (short)data[i, j];
            }
        }
    }

    public Int16 this[int rowNumber, int columnNumber]
    {
        get => this._values[rowNumber, columnNumber];
        set => this._values[rowNumber, columnNumber] = value;
    }


    public void WriteToBinarySimple(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            // Write properties
            writer.Write(XPixelSize);
            writer.Write(YPixelSize);
            writer.Write(ImageWidth);
            writer.Write(ImageHeight);
            writer.Write(CenterOfUpperLeftPixel.X);
            writer.Write(CenterOfUpperLeftPixel.Y);

            // Write array dimensions and data
            //if (_values == null)
            //{
            //writer.Write(0);
            //writer.Write(0);
            //}
            //else
            if (_values != null)
            {
                //int rows = _values.GetLength(0);
                //int cols = _values.GetLength(1);

                //writer.Write(rows);
                //writer.Write(cols);

                for (int i = 0; i < ImageHeight; i++)
                {
                    for (int j = 0; j < ImageWidth; j++)
                    {
                        writer.Write(_values[i, j]);
                    }
                }
            }
        }
    }

    public static WorldfileMatrix16bit ReadFromBinarySimple(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(stream))
        {
            var dem = new WorldfileMatrix16bit();

            // Read properties
            dem.XPixelSize = reader.ReadDouble();
            dem.YPixelSize = reader.ReadDouble();

            var imageWidth = reader.ReadInt32();
            var imageHeight = reader.ReadInt32();

            double centerX = reader.ReadDouble();
            double centerY = reader.ReadDouble();
            dem.CenterOfUpperLeftPixel = new Point((int)centerX, (int)centerY);

            //// Read array dimensions
            //int rows = reader.ReadInt32();
            //int cols = reader.ReadInt32();

            //if (rows != imageHeight || cols != imageWidth)
            //    throw new NotImplementedException();

            if (imageWidth > 0 && imageHeight > 0)
            {
                dem._values = new Int16[imageHeight, imageWidth];

                for (int i = 0; i < imageHeight; i++)
                {
                    for (int j = 0; j < imageWidth; j++)
                    {
                        dem._values[i, j] = reader.ReadInt16();
                    }
                }
            }
            else
            {
                dem._values = new short[0, 0];
            }

            return dem;
        }
    }

    public Point ToImageCoordinate(Point groundCoordinate) => ToImageCoordinate(groundCoordinate, ImageWidth, ImageHeight);

    public Point ToGroundCoordinate(Point imageCoordinate) => ToGroundCoordinate(imageCoordinate, ImageWidth, ImageHeight);

    public Int16 GetValue(Point groundCoordinate)
    {
        if (_values.Length == 0)
            return 0;

        var point = ToImageCoordinate(groundCoordinate);

        int x = (int)point.X;
        int y = (int)point.Y;

        if (y < 0 || y >= ImageHeight ||
            x < 0 || x >= ImageWidth)
        {
            return 0; 
        }

        return _values[y, x];
    }

}

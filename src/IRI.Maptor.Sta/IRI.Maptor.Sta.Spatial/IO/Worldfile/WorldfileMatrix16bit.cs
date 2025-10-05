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

    public int Height => _values?.GetLength(0) ?? 0;

    public int Width => _values?.GetLength(1) ?? 0;

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

    public Int16 this[int rowNumber, int columNumber]
    {
        get => this._values[rowNumber, columNumber];
        set => this._values[rowNumber, columNumber] = value;
    }


    public void WriteToBinarySimple(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            // Write properties
            writer.Write(XPixelSize);
            writer.Write(YPixelSize);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(CenterOfUpperLeftPixel.X);
            writer.Write(CenterOfUpperLeftPixel.Y);

            // Write array dimensions and data
            if (_values == null)
            {
                writer.Write(0);
                writer.Write(0);
            }
            else
            {
                int rows = _values.GetLength(0);
                int cols = _values.GetLength(1);

                writer.Write(rows);
                writer.Write(cols);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
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
            
            double centerX = reader.ReadDouble();
            double centerY = reader.ReadDouble();
            dem.CenterOfUpperLeftPixel = new Point((int)centerX, (int)centerY);

            // Read array dimensions
            int rows = reader.ReadInt32();
            int cols = reader.ReadInt32();

            if (rows > 0 && cols > 0)
            {
                dem._values = new Int16[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
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

}

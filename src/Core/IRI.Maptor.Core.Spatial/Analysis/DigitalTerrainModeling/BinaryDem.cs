//using IRI.Maptor.Core.Common.Primitives;
//using IRI.Maptor.Core.Spatial.IO;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Core.Spatial.DigitalTerrainModeling;

//public class BinaryDem
//{
//    public double XPixelSize { get; set; }

//    public double YPixelSize { get; set; }

//    // in pixels
//    public double Width { get; set; }

//    // in pixels
//    public double Height { get; set; }

//    public Point CenterOfUpperLeftPixel { get; set; }

//    public Int16[,] Values { get; set; }

//    public BinaryDem()
//    {
//    }

//    public BinaryDem(WorldfileMatrix matrix)
//    {
//        XPixelSize = matrix.XPixelSize;
//        YPixelSize = matrix.YPixelSize;
//        Width = matrix.Width;
//        Height = matrix.Height;

//        CenterOfUpperLeftPixel = matrix.CenterOfUpperLeftPixel;

//        Values = new short[matrix.Height, matrix.Width];

//        for (int i = 0; i < matrix.Height; i++)
//        {
//            for (int j = 0; j < matrix.Width; j++)
//            {
//                Values[i, j] = (short)matrix[i, j];
//            }
//        }
//    }
//}

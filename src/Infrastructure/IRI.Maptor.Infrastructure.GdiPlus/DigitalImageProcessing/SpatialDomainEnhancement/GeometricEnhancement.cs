// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public static class GeometricEnhancement
{
    public static Matrix HalveTheSize(Matrix original)
    {
        int width = (int)Math.Floor(original.NumberOfColumns / 2d); //d:double, m:decimal

        int height = (int)Math.Floor(original.NumberOfRows / 2d);

        Matrix result = new Matrix(height, width);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                result[i, j] = (original[2 * i, 2 * j]); // +
                //original[2 * i, 2 * j + 1] +
                //original[2 * i + 1, 2 * j] +
                //original[2 * i + 1, 2 * j + 1]) / 4;
            }
        }

        return result;

    }

    public static Matrix DoubleTheSize(Matrix original)
    {
        int width = original.NumberOfColumns;

        int height = original.NumberOfRows;

        Matrix result = new Matrix(height * 2, width * 2);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                result[2 * i + 0, 2 * j + 0] = (original[i, j]);

                result[2 * i + 1, 2 * j + 0] = ((original[i, j] + original[i + 1, j]) / 2);

                result[2 * i + 0, 2 * j + 1] = ((original[i, j] + original[i, j + 1]) / 2);

                result[2 * i + 1, 2 * j + 1] = ((original[i, j] + original[i + 1, j] + original[i, j + 1] + original[i + 1, j + 1]) / 4);
            }
        }

        return result;
    }

    public static Matrix GetImagePortion(Matrix original, int row, int column, int quasiWindowsSize, double inclinationAngle)
    {
        Matrix result = new Matrix(quasiWindowsSize + quasiWindowsSize, quasiWindowsSize + quasiWindowsSize);

        for (int i = row - quasiWindowsSize; i < row + quasiWindowsSize; i++)
        {
            for (int j = column - quasiWindowsSize; j < column + quasiWindowsSize; j++)
            {
                double rotatedColumn = (j - column) * Math.Cos(inclinationAngle) - (i - row) * Math.Sin(inclinationAngle);

                double rotatedRow = (j - column) * Math.Sin(inclinationAngle) + (i - row) * Math.Cos(inclinationAngle);

                result[i - (row - quasiWindowsSize), j - (column - quasiWindowsSize)] = original[(int)(row + rotatedRow), (int)(column + rotatedColumn)];
            }
        }

        return result;
    }

    //public static 
}

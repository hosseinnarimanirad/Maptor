// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Mathematics;
using System.Drawing;
using System.Drawing.Imaging;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public static class Conversion
{
    public static (Bitmap image, double percent) CalculateDifBitmaps(Bitmap originalImage, Bitmap secondaryImage, bool ignoreWhitePixels)
    {
        if (originalImage.Width != secondaryImage.Width || originalImage.Height != secondaryImage.Height)
            throw new Exception("Sizes must be equal.");

        //Bitmap result = new Bitmap(originalImage.Width, originalImage.Height);

        double differentPixels = 0.0;
        double equalNonWhitePixels = 0.0;

        var originalMatrix = ColorImageToByteArgb(originalImage);
        var secondaryMatrix = ColorImageToByteArgb(secondaryImage);

        Matrix result = new Matrix(originalImage.Height, originalImage.Width);

        for (int i = 0; i < originalImage.Width; i++)
        {
            for (int j = 0; j < originalImage.Height; j++)
            {
                var pixel1 = originalMatrix.Alpha[j, i];
                var pixel2 = secondaryMatrix.Alpha[j, i];

                if (pixel1 != pixel2)
                {
                    //result.SetPixel(i, j, Color.Black);
                    result[j, i] = 255;

                    differentPixels++;
                }
                //equal pixels
                else if (pixel1 != 0 && pixel2 != 0)
                {
                    equalNonWhitePixels++;
                }
            }
        }

        double totalPixels = ignoreWhitePixels ? (differentPixels + equalNonWhitePixels) : (originalImage.Height * originalImage.Width);

        //1399.06.15
        //returns different percent
        return (MatrixToGrayscaleImage(result), differentPixels / totalPixels * 100.0);
    }


    public static Matrix GrayscaleImageToMatrixSlow(Bitmap image)
    {
        //if (image.PixelFormat != PixelFormat.Format24bppRgb)
        //{
        //    throw new NotImplementedException();
        //}

        int width = image.Width;

        int height = image.Height;

        Matrix result = new Matrix(height, width);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                System.Drawing.Color c = image.GetPixel(j, i);

                result[i, j] = (c.R + c.G + c.B) / 3;
            }
        }

        return result;
    }

    public static Bitmap MatrixToGrayscaleImageSlow(Matrix values)
    {

        int width = values.NumberOfColumns;

        int height = values.NumberOfRows;

        Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        double max = IRI.Maptor.Core.Common.Mathematics.Statistics.GetMax(values);

        double min = IRI.Maptor.Core.Common.Mathematics.Statistics.GetMin(values);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int tempValue = (int)((values[i, j] - min) * 255 / (max - min));

                result.SetPixel(j, i, Color.FromArgb(tempValue, tempValue, tempValue));
            }
        }

        return result;
    }

    public static RgbValues ColorImageToRgbSlow(Bitmap image)
    {

        int width = image.Width;

        int height = image.Height;

        Matrix blue = new Matrix(height, width);

        Matrix green = new Matrix(height, width);

        Matrix red = new Matrix(height, width);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                System.Drawing.Color color = image.GetPixel(j, i);

                blue[i, j] = color.B;

                green[i, j] = color.G;

                red[i, j] = color.R;
            }
        }

        return new RgbValues(red, green, blue);
    }



    public static Matrix GrayscaleImageToMatrix(Bitmap image)
    {
        if (image.PixelFormat != PixelFormat.Format8bppIndexed)
        {
            throw new NotImplementedException();
        }

        int width = image.Width;

        int height = image.Height;

        System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.ReadOnly,
                                                                PixelFormat.Format8bppIndexed);

        Matrix result = new Matrix(height, width);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    result[i, j] = (byte)p[0];

                    p++;
                }
            }
        }

        image.UnlockBits(data);

        return result;

    }

    public static Bitmap MatrixToGrayscaleImage(Matrix values)
    {

        int width = values.NumberOfColumns;

        int height = values.NumberOfRows;

        Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.WriteOnly,
                                                                PixelFormat.Format24bppRgb);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    p[0] = (byte)values[i, j];

                    p[1] = p[0];

                    p[2] = p[0];

                    p += 3;
                }
            }
        }

        result.UnlockBits(data);

        return result;
    }

    public static RgbValues ColorImageToRgb(Bitmap image)
    {

        if (image.PixelFormat != PixelFormat.Format24bppRgb)
        {
            throw new NotImplementedException();
        }

        int width = image.Width;

        int height = image.Height;

        System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.ReadOnly,
                                                                PixelFormat.Format24bppRgb);

        Matrix blue = new Matrix(height, width);

        Matrix green = new Matrix(height, width);

        Matrix red = new Matrix(height, width);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    blue[i, j] = (byte)p[0];

                    green[i, j] = (byte)p[1];

                    red[i, j] = (byte)p[2];

                    p += 3;
                }
            }
        }

        image.UnlockBits(data);

        return new RgbValues(red, green, blue);
    }

    public static ArgbValues ColorImageToArgb(Bitmap image)
    {
        if (image.PixelFormat != PixelFormat.Format32bppArgb)
        {
            throw new NotImplementedException();
        }

        int width = image.Width;

        int height = image.Height;

        System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.ReadOnly,
                                                                PixelFormat.Format32bppArgb);


        Matrix blue = new Matrix(height, width);

        Matrix green = new Matrix(height, width);

        Matrix red = new Matrix(height, width);

        Matrix alpha = new Matrix(height, width);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {

                    blue[i, j] = (byte)p[0];

                    green[i, j] = (byte)p[1];

                    red[i, j] = (byte)p[2];

                    alpha[i, j] = (byte)p[3];

                    p += 4;
                }
            }
        }

        image.UnlockBits(data);

        return new ArgbValues(alpha, red, green, blue);
    }

    public static Bitmap RgbToColorImage(RgbValues values)
    {

        int width = values.Width;

        int height = values.Height;

        Bitmap result = new Bitmap(width, height);

        System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.WriteOnly,
                                                                PixelFormat.Format24bppRgb);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    p[0] = (byte)values.Blue[i, j];

                    p[1] = (byte)values.Green[i, j];

                    p[2] = (byte)values.Red[i, j];

                    p += 3;
                }
            }
        }

        result.UnlockBits(data);

        return result;
    }

    public static Bitmap ArgbToColorImage(ArgbValues values)
    {

        int width = values.Width;

        int height = values.Height;

        Bitmap result = new Bitmap(width, height);

        System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.WriteOnly,
                                                                PixelFormat.Format32bppArgb);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    p[0] = (byte)values.Blue[i, j];

                    p[3] = (byte)values.Green[i, j];

                    p[2] = (byte)values.Red[i, j];

                    p[1] = (byte)values.Alpha[i, j];

                    p += 4;
                }
            }
        }

        result.UnlockBits(data);

        return result;
    }

    //
    public static ByteRgbValues ColorImageToByteRgb(Bitmap image)
    {

        if (image.PixelFormat != PixelFormat.Format24bppRgb)
        {
            throw new NotImplementedException();
        }

        int width = image.Width;

        int height = image.Height;

        System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.ReadOnly,
                                                                PixelFormat.Format24bppRgb);

        ImageMatrix blue = new ImageMatrix(height, width);

        ImageMatrix green = new ImageMatrix(height, width);

        ImageMatrix red = new ImageMatrix(height, width);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    blue[i, j] = (byte)p[0];

                    green[i, j] = (byte)p[1];

                    red[i, j] = (byte)p[2];

                    p += 3;
                }
            }
        }

        image.UnlockBits(data);

        return new ByteRgbValues(red, green, blue);
    }

    public static ByteArgbValues ColorImageToByteArgb(Bitmap image)
    {
        if (image.PixelFormat != PixelFormat.Format32bppArgb)
        {
            throw new NotImplementedException();
        }

        int width = image.Width;

        int height = image.Height;

        System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.ReadOnly,
                                                                PixelFormat.Format32bppArgb);


        ImageMatrix blue = new ImageMatrix(height, width);

        ImageMatrix green = new ImageMatrix(height, width);

        ImageMatrix red = new ImageMatrix(height, width);

        ImageMatrix alpha = new ImageMatrix(height, width);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {

                    blue[i, j] = (byte)p[0];

                    green[i, j] = (byte)p[1];

                    red[i, j] = (byte)p[2];

                    alpha[i, j] = (byte)p[3];

                    p += 4;
                }
            }
        }

        image.UnlockBits(data);

        return new ByteArgbValues(alpha, red, green, blue);
    }

    public static Bitmap ByteRgbToColorImage(ByteRgbValues values)
    {

        int width = values.Width;

        int height = values.Height;

        Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.WriteOnly,
                                                                PixelFormat.Format24bppRgb);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    p[0] = values.Blue[i, j];

                    p[1] = values.Green[i, j];

                    p[2] = values.Red[i, j];

                    p += 3;
                }
            }
        }

        result.UnlockBits(data);

        return result;
    }

    public static Bitmap ByteArgbToColorImage(ByteArgbValues values)
    {

        int width = values.Width;

        int height = values.Height;

        Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, width, height),
                                                                ImageLockMode.WriteOnly,
                                                                PixelFormat.Format32bppArgb);

        unsafe
        {
            byte* p = (byte*)(void*)data.Scan0.ToPointer();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    p[0] = (byte)values.Blue[i, j];

                    p[3] = (byte)values.Green[i, j];

                    p[2] = (byte)values.Red[i, j];

                    p[1] = (byte)values.Alpha[i, j];

                    p += 4;
                }
            }
        }

        result.UnlockBits(data);

        return result;
    }

    public static ByteRgbValues ColorImageToByteRgbSlow(Bitmap image)
    {

        if (image.PixelFormat != PixelFormat.Format24bppRgb)
        {
            throw new NotImplementedException();
        }

        int width = image.Width;

        int height = image.Height;

        ImageMatrix blue = new ImageMatrix(height, width);

        ImageMatrix green = new ImageMatrix(height, width);

        ImageMatrix red = new ImageMatrix(height, width);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                System.Drawing.Color c = image.GetPixel(i, j);

                blue[i, j] = (byte)c.B;

                green[i, j] = (byte)c.G;

                red[i, j] = (byte)c.R;
            }
        }

        return new ByteRgbValues(red, green, blue);
    }

}
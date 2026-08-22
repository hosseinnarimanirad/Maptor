// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Drawing;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public static class RadiometricEnhancement
{
    private const int defaultRadiometricResolution = (int)256;

    public static int[] CalculateHistogram(Matrix image)
    {
        int[] result = new int[defaultRadiometricResolution];

        for (int row = 0; row < image.NumberOfRows; row++)
        {
            for (int column = 0; column < image.NumberOfColumns; column++)
            {
                result[(int)image[row, column]]++;
            }
        }

        return result;
    }

    public static int[] CalculateHistogram(ImageMatrix image)
    {
        int[] histogram = new int[defaultRadiometricResolution];

        for (int row = 0; row < image.NumberOfRows; row++)
        {
            for (int column = 0; column < image.NumberOfColumns; column++)
            {
                histogram[image[row, column]]++;
            }
        }

        return histogram;
    }

    public static int[] CalculateCumulativeHistogram(Matrix image)
    {
        int[] histogram = CalculateHistogram(image);

        return CalculateCumulativeHistogram(histogram);
    }

    public static int[] CalculateCumulativeHistogram(ImageMatrix image)
    {
        int[] histogram = CalculateHistogram(image);

        return CalculateCumulativeHistogram(histogram);
    }

    public static int[] CalculateCumulativeHistogram(int[] histogram)
    {
        int histogramLength = histogram.Length;

        int[] cumulativeHistogram = new int[histogramLength];

        cumulativeHistogram[0] = histogram[0];

        for (int i = 1; i < histogramLength; i++)
        {
            cumulativeHistogram[i] = cumulativeHistogram[i - 1] + histogram[i];
        }

        return cumulativeHistogram;
    }

    public static double[] CalculateEqualizedCumulativeHistogram(Matrix image)
    {
        int[] histogram = CalculateHistogram(image);

        return CalculateEqualizedCumulativeHistogram(histogram);
    }

    public static double[] CalculateEqualizedCumulativeHistogram(ImageMatrix image)
    {
        int[] histogram = CalculateHistogram(image);

        return CalculateEqualizedCumulativeHistogram(histogram);
    }

    public static double[] CalculateEqualizedCumulativeHistogram(int[] histogram)
    {
        int numberOfGrayValueLevels = histogram.Length;

        int[] cumulativeHistogram = CalculateCumulativeHistogram(histogram);

        //scaleFactor = (L-1)/N; L:gray value levels; N: number of pixels
        double scaleFactor = (numberOfGrayValueLevels - 1.0) / (double)cumulativeHistogram[numberOfGrayValueLevels - 1];

        double[] result = new double[numberOfGrayValueLevels];

        for (int i = 0; i < numberOfGrayValueLevels; i++)
        {
            result[i] = scaleFactor * cumulativeHistogram[i];
        }

        return result;
    }

    public static Dictionary<int, int> CalculateHistogramMatchingTransform(int[] sourceHistogram, int[] referenceHistogram)
    {
        int numberOfGrayValueLevels = sourceHistogram.Length;

        if (numberOfGrayValueLevels != referenceHistogram.Length)
        {
            throw new NotImplementedException();
        }

        double[] sourceEqualizedCumulativeHistogram = CalculateEqualizedCumulativeHistogram(sourceHistogram);

        double[] referenceEqualizedCumulativeHistogram = CalculateEqualizedCumulativeHistogram(referenceHistogram);

        Dictionary<int, int> result = new Dictionary<int, int>();

        int index = 0;

        for (int i = 0; i < numberOfGrayValueLevels; i++)
        {
            double firstValue = sourceEqualizedCumulativeHistogram[i];

            double value = Math.Abs(referenceEqualizedCumulativeHistogram[index] - firstValue);

            for (int j = index; j < numberOfGrayValueLevels; j++)
            {
                if (value > Math.Abs(referenceEqualizedCumulativeHistogram[j] - firstValue))
                {
                    index = j;

                    value = Math.Abs(referenceEqualizedCumulativeHistogram[j] - firstValue);
                }
            }

            result.Add(i, index);
        }

        return result;
    }

    public static Dictionary<int, int> CalculateHistogramEqualizationTransform(int[] histogram)
    {
        int numberOfGrayValueLevels = histogram.Length;

        int[] cumulativeHistogram = CalculateCumulativeHistogram(histogram);

        //scaleFactor = (L-1)/N; L:gray value levels; N: number of pixels
        double scaleFactor = (numberOfGrayValueLevels - 1.0) / (double)cumulativeHistogram[numberOfGrayValueLevels - 1];

        Dictionary<int, int> result = new Dictionary<int, int>();

        for (int i = 0; i < numberOfGrayValueLevels; i++)
        {
            result.Add(i, (int)Math.Round(scaleFactor * cumulativeHistogram[i]));
        }

        return result;
    }

    public static ImageMatrix MatchHistogram(ImageMatrix sourceImage, ImageMatrix referenceImage)
    {
        //if (!AreTheSameSize(sourceImage, referenceImage))
        //{
        //    throw new NotImplementedException();
        //}

        int[] sourceHistogram = CalculateHistogram(sourceImage);

        int[] referenceHistogram = CalculateHistogram(referenceImage);

        Dictionary<int, int> transformation = CalculateHistogramMatchingTransform(sourceHistogram, referenceHistogram);

        ImageMatrix result = new ImageMatrix(sourceImage.NumberOfRows, sourceImage.NumberOfColumns);

        for (int i = 0; i < sourceImage.NumberOfRows; i++)
        {
            for (int j = 0; j < sourceImage.NumberOfColumns; j++)
            {
                result[i, j] = (byte)transformation[sourceImage[i, j]];
            }
        }

        return result;
    }

    public static Bitmap MatchHistogram(Bitmap sourceImage, Bitmap referenceImage)
    {
        ByteRgbValues sourceRgb = Conversion.ColorImageToByteRgb(sourceImage);

        ByteRgbValues referenceRgb = Conversion.ColorImageToByteRgb(referenceImage);

        ImageMatrix sourceRed = sourceRgb.Red; ImageMatrix referenceRed = referenceRgb.Red;

        ImageMatrix sourceGreen = sourceRgb.Green; ImageMatrix referenceGreen = referenceRgb.Green;

        ImageMatrix sourceBlue = sourceRgb.Blue; ImageMatrix referenceBlue = referenceRgb.Blue;

        ImageMatrix resultRed = MatchHistogram(sourceRed, referenceRed);

        ImageMatrix resultGreen = MatchHistogram(sourceGreen, referenceGreen);

        ImageMatrix resultBlue = MatchHistogram(sourceBlue, referenceBlue);

        ByteRgbValues newValues = new ByteRgbValues(resultRed, resultGreen, resultBlue);

        return Conversion.ByteRgbToColorImage(newValues);
    }

}

// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Drawing;
using System.Drawing.Imaging;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public struct RgbValues
{
    private Matrix m_RedValues, m_GreenValues, m_BlueValues;

    private int[] m_RedHistogram, m_GreenHistogram, m_BlueHistogram;

    private int height, width;

    public RgbValues(Matrix red, Matrix green, Matrix blue)
    {
        if (!(Matrix.AreTheSameSize(red, green) && Matrix.AreTheSameSize(red, blue)))
        {
            throw new NotImplementedException();
        }

        this.height = red.NumberOfRows;

        this.width = red.NumberOfColumns;

        this.m_RedValues = red;

        this.m_GreenValues = green;

        this.m_BlueValues = blue;

        this.m_RedHistogram = RadiometricEnhancement.CalculateHistogram(red);

        this.m_GreenHistogram = RadiometricEnhancement.CalculateHistogram(green);

        this.m_BlueHistogram = RadiometricEnhancement.CalculateHistogram(blue);
    }

    public int Height
    {
        get { return this.height; }
    }

    public int Width
    {
        get { return this.width; }
    }

    public Matrix Red
    {
        get { return this.m_RedValues; }

        set
        {
            if (value.NumberOfColumns != this.width || value.NumberOfRows != this.height)
            {
                throw new NotImplementedException();
            }

            this.m_RedValues = value;
        }
    }

    public Matrix Green
    {
        get { return this.m_GreenValues; }

        set
        {
            if (value.NumberOfColumns != this.width || value.NumberOfRows != this.height)
            {
                throw new NotImplementedException();
            }

            this.m_GreenValues = value;
        }
    }

    public Matrix Blue
    {
        get { return this.m_BlueValues; }

        set
        {
            if (value.NumberOfColumns != this.width || value.NumberOfRows != this.height)
            {
                throw new NotImplementedException();
            }

            this.m_BlueValues = value;
        }
    }

    public int[] RedHistogram
    {
        get { return this.m_RedHistogram; }
    }

    public int[] GreenHistogram
    {
        get { return this.m_GreenHistogram; }
    }

    public int[] BlueHistogram
    {
        get { return this.m_BlueHistogram; }
    }

}

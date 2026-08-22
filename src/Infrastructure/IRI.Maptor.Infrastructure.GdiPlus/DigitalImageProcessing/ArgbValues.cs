// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Mathematics;
using System;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public struct ArgbValues
{
    private Matrix m_RedValues, m_GreenValues, m_BlueValues, m_AlphaValues;

    private int height, width;

    private int[] m_RedHistogram, m_GreenHistogram, m_BlueHistogram, m_AlphaHistogram;

    public ArgbValues(Matrix alpha, Matrix red, Matrix green, Matrix blue)
    {
        if (!(Matrix.AreTheSameSize(red, green) && Matrix.AreTheSameSize(red, blue) && Matrix.AreTheSameSize(red, alpha)))
        {
            throw new NotImplementedException();
        }

        this.height = red.NumberOfRows;

        this.width = red.NumberOfColumns;

        this.m_AlphaValues = alpha;

        this.m_RedValues = red;

        this.m_GreenValues = green;

        this.m_BlueValues = blue;

        this.m_RedHistogram = RadiometricEnhancement.CalculateHistogram(red);

        this.m_GreenHistogram = RadiometricEnhancement.CalculateHistogram(green);

        this.m_BlueHistogram = RadiometricEnhancement.CalculateHistogram(blue);

        this.m_AlphaHistogram = RadiometricEnhancement.CalculateHistogram(alpha);

    }

    public int Height
    {
        get { return this.height; }
    }

    public int Width
    {
        get { return this.width; }
    }

    public Matrix Alpha
    {
        get { return this.m_AlphaValues; }

        set
        {
            if (value.NumberOfColumns != this.width || value.NumberOfRows != this.height)
            {
                throw new NotImplementedException();
            }

            this.m_AlphaValues = value;
        }
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

    public int[] AlphaHistogram
    {
        get { return this.m_AlphaHistogram; }
    }
}

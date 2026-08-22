// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Common.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing.ImageMatching;


public class QuasiImage
{
    private Matrix m_Values;

    private int m_Scale, m_Blur;

    private double m_Sigma;

    public int Width
    {
        get { return this.m_Values.NumberOfColumns; }
    }

    public int Height
    {
        get { return this.m_Values.NumberOfRows; }
    }

    public int Scale
    {
        get { return this.m_Scale; }

        set { this.m_Scale = value; }
    }

    public int Blur
    {
        get { return this.m_Blur; }

        set { this.m_Blur = value; }
    }

    public double Sigma
    {
        get { return this.m_Sigma; }

        set { this.m_Sigma = value; }
    }

    public double this[int column, int row]
    {
        get { return this.m_Values[row, column]; }

        set { this.m_Values[row, column] = value; }
    }

    public double MaxGrayValue
    {
        get
        {
            return IRI.Maptor.Core.Common.Mathematics.Statistics.GetMax(this.m_Values);
        }
    }

    public double MinGrayValue
    {
        get
        {
            return IRI.Maptor.Core.Common.Mathematics.Statistics.GetMin(this.m_Values);
        }
    }

    public QuasiImage(Matrix values, int scale, int blur, double sigma)
    {
        if (values == null)
        {
            throw new NotImplementedException();
        }

        this.m_Scale = scale;

        this.m_Blur = blur;

        this.m_Sigma = sigma;

        this.m_Values = values;
    }
}

// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Common.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing.ImageMatching;

[Serializable()]
public struct Extremum
{
    
    private double m_ScaleLevel;
    
    private double m_BlurLevel;
    
    private double m_Row;

    private double m_Column;

    private double m_Sigma;

    public Extremum(double scaleLevel, double blurLevel, double column, double row, double sigma)
    {
        this.m_ScaleLevel = scaleLevel;

        this.m_BlurLevel = blurLevel;

        this.m_Row = row;

        this.m_Column = column;

        this.m_Sigma = sigma;
    }

    public double ScaleLevel
    {
        get { return m_ScaleLevel; }
        set { m_ScaleLevel = value; }
    }
   
    public double BlurLevel
    {
        get { return m_BlurLevel; }

        set { m_BlurLevel = value; }
    }
    
    public double Row
    {
        get { return m_Row; }

        set { m_Row = value; }
    }

    public double Column
    {
        get { return m_Column; }

        set { m_Column = value; }
    }

    public double Sigma
    {
        get { return this.m_Sigma; }
        set { this.m_Sigma = value; }
    }

    public override string ToString()
    {
        return string.Format("scale:{0}, blur:{1}, x:{2} y:{3}, sigma:{4}", ScaleLevel, BlurLevel, Column, Row, Sigma);
    }

}

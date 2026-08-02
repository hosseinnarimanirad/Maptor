// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Ket.DigitalImageProcessing.ImageMatching;


public struct Extrema : IEnumerable<Extremum>
{
    private List<Extremum> m_Values;

    public int Count
    {
        get { return m_Values.Count; }
    }

    public Extremum this[int index]
    {
        get { return this.m_Values[index]; }
        set { this.m_Values[index] = value; }
    }

    public void Add(double scaleLevel, double blurLevel, double column, double row, double sigma)
    {
        if (this.m_Values == null)
        {
            this.m_Values = new List<Extremum>();
        }

        Extremum value = new Extremum(scaleLevel, blurLevel, column, row, sigma);

        this.m_Values.Add(value);
    }

    public void Remove(int index)
    {
        this.m_Values.RemoveAt(index);
    }

    #region IEnumerable<Extremum> Members

    public IEnumerator<Extremum> GetEnumerator()
    {
        return this.m_Values.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion
}

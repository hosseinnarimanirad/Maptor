// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;


[Obsolete("Legacy type used only by DelaunayTriangulation_old. Use VoronoiDiagram (Analysis) instead.")]
public class VoronoiPoint
{

    //private double m_X;

    //private double m_Y;

    private int m_TriangleCode;

    public double X { get; set; }

    public double Y { get; set; }

    public int TriangleCode
    {
        get { return m_TriangleCode; }
    }

    public VoronoiPoint(int triangleCode, Point position)
    {
        m_TriangleCode = triangleCode;

        X = position.X;

        Y = position.Y;

        NeighboursCode = new List<int>();
    }

    public List<int> NeighboursCode;

    public override string ToString()
    {
        //StringBuilder temp = new StringBuilder();

        //temp.Append(string.Format("X:{0}, Y:{1}, Neighbours:", X.ToString(), Y.ToString()));

        //foreach (int item in NeighboursCode)
        //{
        //    temp.Append(string.Format(", {0}", item.ToString()));
        //}

        //return temp.ToString();

        return string.Format("X:{0}, Y:{1}, Triangle{2}:", X.ToString(), Y.ToString(), TriangleCode);
    }

    // dar soorate neveshtane hamsayeha dat method ToString()
    // ba tagire hamsayeha hashcode nogata ham avaz mishavad!
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() != typeof(VoronoiPoint))
        {
            return false;
        }

        return ToString().Equals(obj.ToString());
    }
}

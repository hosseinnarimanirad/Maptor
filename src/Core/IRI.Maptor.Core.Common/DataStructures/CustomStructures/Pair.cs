// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.CustomStructures;

public struct Pair<TFirst, TSecond>
{
    private TFirst m_First;

    private TSecond m_Second;

    public TFirst First
    {
        get { return this.m_First; }
        set { this.m_First = value; }
    }

    public TSecond Second
    {
        get { return this.m_Second; }
        set { this.m_Second = value; }
    }

    public Pair(TFirst first, TSecond second)
    {
        this.m_First = first;

        this.m_Second = second;
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}", this.First.ToString(), this.Second.ToString());
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(Pair<TFirst, TSecond>))
        {
            Pair<TFirst, TSecond> temp = (Pair<TFirst, TSecond>)obj;

            return this.First.Equals(temp.First) && this.Second.Equals(temp.Second);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(First, Second);
    }
}

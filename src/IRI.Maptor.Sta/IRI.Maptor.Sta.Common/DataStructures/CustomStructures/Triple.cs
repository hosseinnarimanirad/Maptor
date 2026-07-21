//// besmellahe rahmane rahim
//// Allahomma ajjel le-valiyek al-faraj
 
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.DataStructures.CustomStructures;

public struct Triple<TFirst, TSecond, TThird>
{
    private TFirst m_First;

    private TSecond m_Second;

    private TThird m_Third;

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

    public TThird Third
    {
        get { return this.m_Third; }
        set { this.m_Third = value; }
    }

    public Triple(TFirst first, TSecond second, TThird third)
    {
        this.m_First = first;

        this.m_Second = second;

        this.m_Third = third;
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}, {2}", this.First.ToString(), this.Second.ToString(), this.Third.ToString());
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(Triple<TFirst, TSecond, TThird>))
        {
            Triple<TFirst, TSecond, TThird> temp = (Triple<TFirst, TSecond, TThird>)obj;

            return this.First.Equals(temp.First) && this.Second.Equals(temp.Second) && this.Third.Equals(temp.Third);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(First, Second, Third);
    }
}

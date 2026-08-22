// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.CustomStructures;

public struct IndexValue<T> : IComparable<IndexValue<T>> where T : IComparable<T>
{
    int m_Index;

    T m_Value;

    public int Index
    {
        get { return this.m_Index; }
        set { this.m_Index = value; }
    }

    public T Value
    {
        get { return this.m_Value; }
        set { this.m_Value = value; }
    }

    public void ChangeValue(T newValue)
    {
        this.m_Value = newValue;
    }

    public void ChangeIndex(int index)
    {
        this.m_Index = index;
    }

    public IndexValue(int index, T values)
    {
        this.m_Index = index;

        this.m_Value = values;
    }

    public override string ToString()
    {
        return string.Format("{0}:{1}", Index, Value.ToString()); ;
    }

    //public static bool operator <(IndexValue<T> firstValue, IndexValue<T> secondValue)
    //{
    //    return firstValue.CompareTo(secondValue) < 0;
    //}

    //public static bool operator >(IndexValue<T> firstValue, IndexValue<T> secondValue)
    //{
    //    return firstValue.CompareTo(secondValue) > 0;
    //}

    //public static bool operator ==(IndexValue<T> firstValue, IndexValue<T> secondValue)
    //{
    //    return firstValue.CompareTo(secondValue) == 0;
    //}

    //public static bool operator !=(IndexValue<T> firstValue, IndexValue<T> secondValue)
    //{
    //    return firstValue.CompareTo(secondValue) != 0;
    //}

    //#region IComparable<T> Members

    //public int CompareTo(T other)
    //{
    //    return this.values.CompareTo(other);
    //}

    //#endregion

    #region IComparable<IndexValue<T>> Members

    public int CompareTo(IndexValue<T> other)
    {
        return this.Value.CompareTo(other.Value);
    }

    #endregion
}

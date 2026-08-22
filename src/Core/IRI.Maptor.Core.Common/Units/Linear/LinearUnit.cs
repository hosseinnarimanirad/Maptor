// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Metrics;

public abstract class LinearUnit : IComparable<LinearUnit>
{
    private double m_Value;

    public double Value
    {
        get
        {
            return m_Value;
        }

        set
        {
            m_Value = value;
        }
    }

    public abstract LinearMode Mode { get; }

    protected LinearUnit() { }

    protected LinearUnit(double value)
    {
        this.m_Value = value;
    }

    public abstract LinearUnit Add(LinearUnit value);

    public abstract LinearUnit Subtract(LinearUnit value);

    public abstract LinearUnit Multiply(LinearUnit value);

    public abstract LinearUnit Divide(LinearUnit value);

    public abstract LinearUnit Negate();

    public abstract LinearUnit Clone();

    public abstract LinearUnit ChangeTo<T>()
        where T : LinearUnit;

    public abstract int CompareTo(LinearUnit other);
}
// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Globalization;

namespace IRI.Maptor.Core.Common.Metrics;

public delegate void ValueChangedEventHandler(object sender, EventArgs e);

public abstract class AngularUnit : IComparable<AngularUnit>
{
    protected event ValueChangedEventHandler OnValueChanged;

    private double m_Value;

    private AngleRange m_Range;

    public double Value
    {
        get { return m_Value; }

        set
        {
            this.m_Value = this.Adapter.Adopt(value);

            if (this.OnValueChanged != null)
            {
                this.OnValueChanged.Invoke(null, null);
            }
        }
    }

    public AngleRange Range
    {
        get { return m_Range; }

        set
        {
            this.m_Range = value;

            this.m_Value = this.Adapter.Adopt(this.m_Value);

            if (this.OnValueChanged != null)
            {
                this.OnValueChanged.Invoke(null, null);
            }
        }
    }

    public abstract AngleMode Mode { get; }

    public abstract AngleAdapter Adapter { get; }

    public abstract double Sin { get; }

    public abstract double Cos { get; }

    public abstract double Tan { get; }

    public abstract double Cot { get; }

    public abstract double Sinh { get; }

    public abstract double Cosh { get; }

    public abstract double Tanh { get; }

    protected AngularUnit()
    {

    }

    protected AngularUnit(double value, AngleRange range)
    {
        this.m_Range = range;

        this.m_Value = this.Adapter.Adopt(value);
    }

    public abstract AngularUnit Add(AngularUnit value);

    //public abstract AngularUnit Add(double value);

    public abstract AngularUnit Subtract(AngularUnit value);

    //public abstract AngularUnit Subtract(double value);

    public abstract AngularUnit Multiply(AngularUnit value);

    //public abstract AngularUnit Multiply(double value);

    public abstract AngularUnit Divide(AngularUnit value);

    //public abstract AngularUnit Divide(double value);

    public abstract AngularUnit Negate();

    public abstract AngularUnit Clone();

    public abstract AngularUnit ChangeTo<T>()
        where T : AngularUnit;

    public abstract int CompareTo(AngularUnit other);


}
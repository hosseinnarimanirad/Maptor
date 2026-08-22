// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Globalization;

namespace IRI.Maptor.Core.Common.Metrics;

public class Radian : AngularUnit
{
    #region Fields

    private readonly double[] minValue = new double[] { 0, -Math.PI };

    private readonly double[] maxValue = new double[] { 2 * Math.PI, Math.PI };

    private const double period = 2 * Math.PI;

    #endregion

    #region Properties

    public override AngleMode Mode
    {
        get { return AngleMode.Radian; }
    }

    //public override AngleRange Range
    //{
    //    get { return m_Range; }

    //    set
    //    {
    //        this.m_Range = value;

    //        SetValue(this.m_Value);
    //    }
    //}

    public override double Sin
    {
        get { return Math.Sin(this.Value); }
    }

    public override double Cos
    {
        get { return Math.Cos(this.Value); }
    }

    public override double Tan
    {
        get { return Math.Tan(this.Value); }
    }

    public override double Cot
    {
        get { return 1 / Math.Tan(this.Value); }
    }

    public override double Sinh
    {
        get { return Math.Sinh(this.Value); }
    }

    public override double Cosh
    {
        get { return Math.Cosh(this.Value); }
    }

    public override double Tanh
    {
        get { return Math.Tanh(this.Value); }
    }

    #endregion

    #region Constructors

    public Radian() 
        : base(0, AngleRange.ZeroTo2Pi) { }

    public Radian(double value) 
        : base(value, AngleRange.ZeroTo2Pi) { }

    public Radian(double value, AngleRange range)
        : base(value, range) { }

    #endregion

    #region Methods

    public override AngularUnit Add(AngularUnit value)
    {
        return new Radian(this.Value + value.ChangeTo<Radian>().Value, this.Range);
    }

    public override AngularUnit Subtract(AngularUnit value)
    {
        return new Radian(this.Value - value.ChangeTo<Radian>().Value, this.Range);
    }

    public override AngularUnit Multiply(AngularUnit value)
    {
        return new Radian(this.Value * value.ChangeTo<Radian>().Value, this.Range);
    }

    public override AngularUnit Divide(AngularUnit value)
    {
        return new Radian(this.Value / value.ChangeTo<Radian>().Value, this.Range);
    }

    public override AngularUnit Negate()
    {
        return new Radian(-this.Value, this.Range);
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
                                  "{0} Radian",
                                  this.Value.ToString(CultureInfo.InvariantCulture));
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(Radian))
        {
            return this == (Radian)obj;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    public sealed override AngleAdapter Adapter
    {
        get
        {
            return new AngleAdapter(minValue[(int)this.Range],
                                    maxValue[(int)this.Range],
                                    period);
        }
    }

    public override AngularUnit Clone()
    {
        return new Radian(this.Value, this.Range);
    }

    public override AngularUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Degree))
        {
            return (Degree)this;
        }
        else if (typeof(T) == typeof(Grade))
        {
            return (Grade)this;
        }
        else if (typeof(T) == typeof(Radian))
        {
            return this.Clone();
        }
        else
            throw new NotImplementedException();
    }

    public override int CompareTo(AngularUnit other)
    {
        return this.Value.CompareTo(other.ChangeTo<Radian>().Value);
    }

    #endregion

    #region Operators

    public static bool operator ==(Radian firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) == 0); }

    public static bool operator !=(Radian firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) != 0); }

    public static bool operator <(Radian firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Radian firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Radian firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Radian firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Degree(Radian angle)
    {

        return new Degree(UnitConversion.RadianToDegree(angle.Value), angle.Range);

    }

    public static explicit operator Grade(Radian angle)
    {

        return new Grade(UnitConversion.RadianToGrade(angle.Value), angle.Range);

    }

    #endregion

}

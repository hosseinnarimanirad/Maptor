// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Globalization;

namespace IRI.Maptor.Core.Common.Metrics;

public class Grade : AngularUnit
{
    #region Fields

    private readonly double[] minValue = new double[] { 0, -200 };

    private readonly double[] maxValue = new double[] { 400, 200 };

    private const double period = 400;

    private short m_GradePart;

    private byte m_MinutePart;

    private byte m_SecondPart;

    //private double m_Value;

    //private AngleRange m_Range;

    private int m_Sign;

    #endregion

    #region Properties

    public int Sign
    {
        get { return this.m_Sign; }
    }

    public override AngleMode Mode
    {
        get { return AngleMode.Grade; }
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

    public short GradePart
    {
        get { return m_GradePart; }
    }

    public byte MinutePart
    {
        get { return m_MinutePart; }
    }

    public byte SecondPart
    {
        get { return m_SecondPart; }
    }

    public override double Sin
    {
        get { return Math.Sin(UnitConversion.GradeToRadian(this.Value)); }
    }

    public override double Cos
    {
        get { return Math.Cos(UnitConversion.GradeToRadian(this.Value)); }
    }

    public override double Tan
    {
        get { return Math.Tan(UnitConversion.GradeToRadian(this.Value)); }
    }

    public override double Cot
    {
        get { return 1 / Math.Tan(UnitConversion.GradeToRadian(this.Value)); }
    }

    public override double Sinh
    {
        get { return Math.Sinh(UnitConversion.GradeToRadian(this.Value)); }
    }

    public override double Cosh
    {
        get { return Math.Cosh(UnitConversion.GradeToRadian(this.Value)); }
    }

    public override double Tanh
    {
        get { return Math.Tanh(UnitConversion.GradeToRadian(this.Value)); }
    }

    #endregion

    #region Constructors

    public Grade() 
        : base(0, AngleRange.ZeroTo2Pi) 
    {
        this.OnValueChanged += new ValueChangedEventHandler(Grade_OnValueChanged);
    }

    public Grade(double value)
        : base(value, AngleRange.ZeroTo2Pi) 
    {
        this.OnValueChanged += new ValueChangedEventHandler(Grade_OnValueChanged);
    }

    public Grade(double value, AngleRange range)
        : base(value, range)
    {

        //this.m_Value = this.Adapter.Adopt(value);

        this.m_Sign = Math.Sign(value);

        this.m_GradePart = (short)Math.Floor(m_Sign * this.Value);

        this.m_MinutePart = (byte)Math.Floor((m_Sign * this.Value - this.m_GradePart) * 100);

        this.m_SecondPart = (byte)Math.Round(((m_Sign * this.Value - this.m_GradePart) * 100 - this.m_MinutePart) * 100);

        this.OnValueChanged += new ValueChangedEventHandler(Grade_OnValueChanged);
        //this.m_Range = range;
    }

    public Grade(short gradePart, byte minutePart, byte secondPart, AngleRange range)
        : this(gradePart, minutePart, secondPart, range, true) { }

    public Grade(short gradePart, byte minutePart, byte secondPart, AngleRange range, bool isPositive)
        : base((isPositive ? 1 : -1) * (gradePart + minutePart / 60d + secondPart / 3600d), range)
    {
        if (minutePart >= 100 || secondPart >= 100)
        {
            minutePart += (byte)(secondPart / 100);

            secondPart = (byte)(secondPart % 100);

            gradePart += (short)(minutePart / 100);

            minutePart = (byte)(minutePart % 100);
        }

        this.m_Sign = (isPositive ? 1 : -1);

        //double value = m_Sign * (gradePart + minutePart / 100d + secondPart / 10000d);

        //this.m_Value = this.Adapter.Adopt(value);

        this.m_GradePart = (short)Math.Floor(m_Sign * this.Value);

        this.m_MinutePart = (byte)Math.Floor((m_Sign * this.Value - this.m_GradePart) * 100);

        this.m_SecondPart = (byte)Math.Round(((m_Sign * this.Value - this.m_GradePart) * 100 - this.m_MinutePart) * 100);

        this.OnValueChanged += new ValueChangedEventHandler(Grade_OnValueChanged);
        //this.m_Range = range;
    }

    #endregion

    #region Methods

    public override AngularUnit Add(AngularUnit value)
    {
        return new Grade(this.Value + value.ChangeTo<Grade>().Value, this.Range);
    }

    public override AngularUnit Subtract(AngularUnit value)
    {
        return new Grade(this.Value - value.ChangeTo<Grade>().Value, this.Range);
    }

    public override AngularUnit Multiply(AngularUnit value)
    {
        return new Grade(this.Value * value.ChangeTo<Grade>().Value, this.Range);
    }

    public override AngularUnit Divide(AngularUnit value)
    {
        return new Grade(this.Value / value.ChangeTo<Grade>().Value, this.Range);
    }

    public override AngularUnit Negate()
    {
        return new Grade(-this.Value, this.Range);
    }

    void Grade_OnValueChanged(object sender, EventArgs e)
    {
        this.m_Sign = Math.Sign(this.Value);

        this.m_GradePart = (short)Math.Floor(m_Sign * this.Value);

        this.m_MinutePart = (byte)Math.Floor((m_Sign * this.Value - this.m_GradePart) * 100);

        this.m_SecondPart = (byte)Math.Floor(((m_Sign * this.Value - this.m_GradePart) * 100 - this.m_MinutePart) * 100);
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
                                  "{0} Grade",
                                  this.Value.ToString(CultureInfo.InvariantCulture));
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(Grade))
        {
            return this == (Grade)obj;
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
        return new Grade(this.Value, this.Range);
    }

    public override AngularUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Degree))
        {
            return (Degree)this;
        }
        else if (typeof(T) == typeof(Grade))
        {
            return this.Clone();
        }
        else if (typeof(T) == typeof(Radian))
        {
            return (Radian)this;
        }
        else
            throw new NotImplementedException();
    }

    public override int CompareTo(AngularUnit other)
    {
        return this.Value.CompareTo(other.ChangeTo<Grade>().Value);
    }

    #endregion

    #region Operators

    public static bool operator ==(Grade firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) == 0); }

    public static bool operator !=(Grade firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) != 0); }

    public static bool operator <(Grade firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Grade firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Grade firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Grade firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Degree(Grade angle)
    {

        return new Degree(UnitConversion.GradeToDegree(angle.Value), angle.Range);

    }

    public static explicit operator Radian(Grade angle)
    {

        return new Radian(UnitConversion.GradeToRadian(angle.Value), angle.Range);

    }

    #endregion
}

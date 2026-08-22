// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Globalization;

namespace IRI.Maptor.Core.Common.Metrics;

public class Degree : AngularUnit
{
    #region Fields

    private readonly double[] minValue = new double[] { 0, -180 };

    private readonly double[] maxValue = new double[] { 360, 180 };

    private const double period = 360;

    private short _degreePart;

    private byte _minutePart;

    private double _secondPart;

    private int _sign;

    #endregion

    #region Properties

    public int Sign
    {
        get { return this._sign; }
    }

    public override AngleMode Mode
    {
        get { return AngleMode.Degree; }
    }
     
    public short DegreePart
    {
        get { return _degreePart; }
    }

    public byte MinutePart
    {
        get { return _minutePart; }
    }

    public double SecondPart
    {
        get { return _secondPart; }
    }

    public override double Sin
    {
        get { return Math.Sin(UnitConversion.DegreeToRadian(this.Value)); }
    }

    public override double Cos
    {
        get { return Math.Cos(UnitConversion.DegreeToRadian(this.Value)); }
    }

    public override double Tan
    {
        get { return Math.Tan(UnitConversion.DegreeToRadian(this.Value)); }
    }

    public override double Cot
    {
        get { return 1 / Math.Tan(UnitConversion.DegreeToRadian(this.Value)); }
    }

    public override double Sinh
    {
        get { return Math.Sinh(UnitConversion.DegreeToRadian(this.Value)); }
    }

    public override double Cosh
    {
        get { return Math.Cosh(UnitConversion.DegreeToRadian(this.Value)); }
    }

    public override double Tanh
    {
        get { return Math.Tanh(UnitConversion.DegreeToRadian(this.Value)); }
    }

    #endregion

    #region Constructors

    public Degree()
        : base(0, AngleRange.ZeroTo2Pi)
    {
        this.OnValueChanged += new ValueChangedEventHandler(Degree_OnValueChanged);
    }

    public Degree(double value)
        : base(value, AngleRange.ZeroTo2Pi)
    {
        this.OnValueChanged += new ValueChangedEventHandler(Degree_OnValueChanged);

        this._sign = Math.Sign(value);

        this._degreePart = (short)Math.Floor(_sign * this.Value);

        this._minutePart = (byte)Math.Floor((_sign * this.Value - this._degreePart) * 60);

        this._secondPart = (((_sign * this.Value - this._degreePart) * 60 - this._minutePart) * 60);
    }

    public Degree(double value, AngleRange range)
        : base(value, range)
    {
        //this.m_Value = this.Adapter.Adopt(value);

        this._sign = Math.Sign(value);

        this._degreePart = (short)Math.Floor(_sign * this.Value);

        this._minutePart = (byte)Math.Floor((_sign * this.Value - this._degreePart) * 60);

        this._secondPart = (((_sign * this.Value - this._degreePart) * 60 - this._minutePart) * 60);

        this.OnValueChanged += new ValueChangedEventHandler(Degree_OnValueChanged);
        //this.m_Range = range;
    }

    void Degree_OnValueChanged(object sender, EventArgs e)
    {
        this._sign = Math.Sign(this.Value);

        this._degreePart = (short)Math.Floor(_sign * this.Value);

        this._minutePart = (byte)Math.Floor((_sign * this.Value - this._degreePart) * 60);

        this._secondPart = (((_sign * this.Value - this._degreePart) * 60 - this._minutePart) * 60);
    }

    public Degree(short degreePart, byte minutePart, double secondPart, AngleRange range)
        : this(degreePart, minutePart, secondPart, range, true)
    { }

    public Degree(short degreePart, byte minutePart, double secondPart, AngleRange range, bool isPositive)
        : base((isPositive ? 1 : -1) * (degreePart + minutePart / 60d + secondPart / 3600d), range)
    {
        if (minutePart >= 60 || secondPart >= 60)
        {
            minutePart += (byte)(secondPart / 60);

            secondPart = secondPart % 60;

            degreePart += (short)(minutePart / 60);

            minutePart = (byte)(minutePart % 60);
        }

        this._sign = (isPositive ? 1 : -1);

        //double value = m_Sign * (degreePart + minutePart / 60d + secondPart / 3600d);

        //this.m_Value = this.Adapter.Adopt(value);

        this._degreePart = (short)Math.Floor(_sign * this.Value);

        this._minutePart = (byte)Math.Floor((_sign * this.Value - this._degreePart) * 60);

        this._secondPart = (((_sign * this.Value - this._degreePart) * 60 - this._minutePart) * 60);

        this.OnValueChanged += new ValueChangedEventHandler(Degree_OnValueChanged);
        //this.m_Range = range;
    }

    #endregion

    #region Methods

    public override AngularUnit Add(AngularUnit value)
    {
        return new Degree(this.Value + value.ChangeTo<Degree>().Value, this.Range);
    }

    public override AngularUnit Subtract(AngularUnit value)
    {
        return new Degree(this.Value - value.ChangeTo<Degree>().Value, this.Range);
    }

    public override AngularUnit Multiply(AngularUnit value)
    {
        return new Degree(this.Value * value.ChangeTo<Degree>().Value, this.Range);
    }

    public override AngularUnit Divide(AngularUnit value)
    {
        return new Degree(this.Value / value.ChangeTo<Degree>().Value, this.Range);
    }

    public override AngularUnit Negate()
    {
        return new Degree(-this.Value, this.Range);
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
                                  "{0} Degree",
                                  this.Value.ToString(CultureInfo.InvariantCulture));
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(Degree))
        {
            return this == (Degree)obj;
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
        return new Degree(this.Value, this.Range);
    }

    public override AngularUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Degree))
        {
            return this.Clone();
        }
        else if (typeof(T) == typeof(Grade))
        {
            return (Grade)this;
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
        return this.Value.CompareTo(other.ChangeTo<Degree>().Value);
    }

    #endregion

    #region Operators

    public static bool operator ==(Degree firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) == 0); }

    public static bool operator !=(Degree firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) != 0); }

    public static bool operator <(Degree firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Degree firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Degree firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Degree firstValue, AngularUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Grade(Degree angle)
    {

        return new Grade(UnitConversion.DegreeToGrade(angle.Value), angle.Range);

    }

    public static explicit operator Radian(Degree angle)
    {

        return new Radian(UnitConversion.DegreeToRadian(angle.Value), angle.Range);

    }

    //public static bool operator ==(Degree firstValue, Degree secondValue)
    //{
    //    return (firstValue.Value == secondValue.Value &&
    //            firstValue.Range == secondValue.Range);
    //}

    //public static bool operator !=(Degree firstValue, Degree secondValue)
    //{
    //    return (!(firstValue == secondValue));
    //}

    #endregion


}

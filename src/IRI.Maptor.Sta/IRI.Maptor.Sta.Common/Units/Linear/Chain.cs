// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Metrics;

public class Chain : LinearUnit
{
    #region Fields&Properties

    public override LinearMode Mode
    {
        get { return LinearMode.Chain; }
    }

    #endregion

    #region Constructors

    public Chain() : this(0) { }

    public Chain(double value) : base(value) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (this.ToString() == obj.ToString() && obj.GetType() == typeof(Chain));
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Chain",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Operators

    public static bool operator ==(Chain firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Chain firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Chain firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Chain firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Chain firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Chain firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Meter(Chain distance)
    {
        return new Meter(UnitConversion.ChainToMeter(distance.Value));//, LinearPrefix.Nothing);
    }

    public static explicit operator Mile(Chain distance)
    {
        return new Mile(UnitConversion.ChainToMile(distance.Value));
    }

    public static explicit operator Yard(Chain distance)
    {
        return new Yard(UnitConversion.ChainToYard(distance.Value));
    }

    public static explicit operator Foot(Chain distance)
    {
        return new Foot(UnitConversion.ChainToFoot(distance.Value));
    }

    public static explicit operator Inch(Chain distance)
    {
        return new Inch(UnitConversion.ChainToInch(distance.Value));
    }

    public static explicit operator Rod(Chain distance)
    {
        return new Rod(UnitConversion.ChainToRod(distance.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        return new Chain(this.Value + value.ChangeTo<Chain>().Value);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        return new Chain(this.Value - value.ChangeTo<Chain>().Value);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        return new Chain(this.Value * value.ChangeTo<Chain>().Value);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        return new Chain(this.Value / value.ChangeTo<Chain>().Value);
    }

    public override LinearUnit Negate()
    {
        return new Chain(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Chain(this.Value);
    }

    public override LinearUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Meter))
            return (Meter)this;

        else if (typeof(T) == typeof(Mile))
            return (Mile)this;

        else if (typeof(T) == typeof(Yard))
            return (Yard)this;

        else if (typeof(T) == typeof(Foot))
            return (Foot)this;

        else if (typeof(T) == typeof(Inch))
            return (Inch)this;

        else if (typeof(T) == typeof(Rod))
            return (Rod)this;

        else if (typeof(T) == typeof(Chain))
            return this.Clone();
        else
            return null;
    }

    #endregion

    #region IComparable<ILinearUnit> Members

    public override int CompareTo(LinearUnit other)
    {
        return this.Value.CompareTo(other.ChangeTo<Chain>().Value);
    }

    #endregion
}

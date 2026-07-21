// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Metrics;

public class Foot : LinearUnit
{
    #region Fields&Properties

    public override LinearMode Mode
    {
        get { return LinearMode.Foot; }
    }

    #endregion

    #region Constructors

    public Foot() : this(0) { }

    public Foot(double value) : base(value) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (obj.GetType() == typeof(Foot) && obj.ToString() == this.ToString());
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Foot",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Operators

    public static bool operator ==(Foot firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Foot firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Foot firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Foot firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Foot firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Foot firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Meter(Foot distance)
    {
        return new Meter(UnitConversion.FootToMeter(distance.Value));//, LinearPrefix.Nothing);
    }

    public static explicit operator Mile(Foot distance)
    {
        return new Mile(UnitConversion.FootToMile(distance.Value));
    }

    public static explicit operator Yard(Foot distance)
    {
        return new Yard(UnitConversion.FootToYard(distance.Value));
    }

    public static explicit operator Inch(Foot distance)
    {
        return new Inch(UnitConversion.FootToInch(distance.Value));
    }

    public static explicit operator Rod(Foot distance)
    {
        return new Rod(UnitConversion.FootToRod(distance.Value));
    }

    public static explicit operator Chain(Foot distance)
    {
        return new Chain(UnitConversion.FootToChain(distance.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        return new Foot(this.Value + value.ChangeTo<Foot>().Value);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        return new Foot(this.Value - value.ChangeTo<Foot>().Value);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        return new Foot(this.Value * value.ChangeTo<Foot>().Value);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        return new Foot(this.Value / value.ChangeTo<Foot>().Value);
    }

    public override LinearUnit Negate()
    {
        return new Foot(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Foot(this.Value);
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
            return this.Clone();

        else if (typeof(T) == typeof(Inch))
            return (Inch)this;

        else if (typeof(T) == typeof(Rod))
            return (Rod)this;

        else if (typeof(T) == typeof(Chain))
            return (Chain)this;
        else
            throw new NotImplementedException();
    }

    #endregion

    #region IComparable<ILinearUnit> Members

    public override int CompareTo(LinearUnit other)
    {
        return this.Value.CompareTo(other.ChangeTo<Foot>().Value);
    }

    #endregion
}

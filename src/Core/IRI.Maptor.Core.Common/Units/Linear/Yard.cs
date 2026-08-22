// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Metrics;

public class Yard : LinearUnit
{
    #region Fields&Properties

    public override LinearMode Mode
    {
        get { return LinearMode.Yard; }
    }

    #endregion

    #region Constructors

    public Yard() : this(0) { }

    public Yard(double value) : base(value) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (obj.GetType() == typeof(Yard) && obj.ToString() == this.ToString());
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Yard",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Operators

    public static bool operator ==(Yard firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Yard firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Yard firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Yard firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Yard firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Yard firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Meter(Yard distance)
    {
        return new Meter(UnitConversion.YardToMeter(distance.Value));//, LinearPrefix.Nothing);
    }

    public static explicit operator Mile(Yard distance)
    {
        return new Mile(UnitConversion.YardToMile(distance.Value));
    }

    public static explicit operator Foot(Yard distance)
    {
        return new Foot(UnitConversion.YardToFoot(distance.Value));
    }

    public static explicit operator Inch(Yard distance)
    {
        return new Inch(UnitConversion.YardToInch(distance.Value));
    }

    public static explicit operator Rod(Yard distance)
    {
        return new Rod(UnitConversion.YardToRod(distance.Value));
    }

    public static explicit operator Chain(Yard distance)
    {
        return new Chain(UnitConversion.YardToChain(distance.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        return new Yard(this.Value + value.ChangeTo<Yard>().Value);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        return new Yard(this.Value - value.ChangeTo<Yard>().Value);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        return new Yard(this.Value * value.ChangeTo<Yard>().Value);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        return new Yard(this.Value / value.ChangeTo<Yard>().Value);
    }

    public override LinearUnit Negate()
    {
        return new Yard(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Yard(this.Value);
    }

    public override LinearUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Meter))
            return (Meter)this;

        else if (typeof(T) == typeof(Mile))
            return (Mile)this;

        else if (typeof(T) == typeof(Yard))
            return this.Clone();

        else if (typeof(T) == typeof(Foot))
            return (Foot)this;

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
        return this.Value.CompareTo(other.ChangeTo<Yard>().Value);
    }

    #endregion

    
}

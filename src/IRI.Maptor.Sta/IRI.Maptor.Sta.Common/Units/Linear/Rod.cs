// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Metrics;

public class Rod : LinearUnit
{
    #region Fields&Properties

    public override LinearMode Mode
    {
        get { return LinearMode.Rod; }
    }

    #endregion

    #region Constructors

    public Rod() : this(0) { }

    public Rod(double value) : base(value) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (obj.GetType() == typeof(Rod) && obj.ToString() == this.ToString());
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Rod",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Operators

    public static bool operator ==(Rod firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Rod firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Rod firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Rod firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Rod firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Rod firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Meter(Rod distance)
    {
        return new Meter(UnitConversion.RodToMeter(distance.Value));//, LinearPrefix.Nothing);
    }

    public static explicit operator Mile(Rod distance)
    {
        return new Mile(UnitConversion.RodToMile(distance.Value));
    }

    public static explicit operator Yard(Rod distance)
    {
        return new Yard(UnitConversion.RodToYard(distance.Value));
    }

    public static explicit operator Foot(Rod distance)
    {
        return new Foot(UnitConversion.RodToFoot(distance.Value));
    }

    public static explicit operator Inch(Rod distance)
    {
        return new Inch(UnitConversion.RodToInch(distance.Value));
    }

    public static explicit operator Chain(Rod distance)
    {
        return new Chain(UnitConversion.RodToChain(distance.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        return new Rod(this.Value + value.ChangeTo<Rod>().Value);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        return new Rod(this.Value - value.ChangeTo<Rod>().Value);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        return new Rod(this.Value * value.ChangeTo<Rod>().Value);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        return new Rod(this.Value / value.ChangeTo<Rod>().Value);
    }

    public override LinearUnit Negate()
    {
        return new Rod(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Rod(this.Value);
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
            return this.Clone();

        else if (typeof(T) == typeof(Chain))
            return (Chain)this;
        else
            throw new NotImplementedException();
    }

    #endregion

    #region IComparable<ILinearUnit> Members

    public override int CompareTo(LinearUnit other)
    {
        return this.Value.CompareTo(other.ChangeTo<Rod>().Value);
    }

    #endregion
}

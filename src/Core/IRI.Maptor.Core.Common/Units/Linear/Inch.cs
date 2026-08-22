// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Metrics;

public class Inch : LinearUnit
{
    #region Fields&Properties

    public override LinearMode Mode
    {
        get { return LinearMode.Inch; }
    }

    #endregion

    #region Constructors

    public Inch() : this(0) { }

    public Inch(double value) : base(value) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (obj.GetType() == typeof(Inch) && obj.ToString() == this.ToString());
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Inch",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Operators

    public static bool operator ==(Inch firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Inch firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Inch firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Inch firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Inch firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Inch firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Meter(Inch distance)
    {
        return new Meter(UnitConversion.InchToMeter(distance.Value));//, LinearPrefix.Nothing);
    }

    public static explicit operator Mile(Inch distance)
    {
        return new Mile(UnitConversion.InchToMile(distance.Value));
    }

    public static explicit operator Yard(Inch distance)
    {
        return new Yard(UnitConversion.InchToYard(distance.Value));
    }

    public static explicit operator Foot(Inch distance)
    {
        return new Foot(UnitConversion.InchToFoot(distance.Value));
    }

    public static explicit operator Rod(Inch distance)
    {
        return new Rod(UnitConversion.InchToRod(distance.Value));
    }

    public static explicit operator Chain(Inch distance)
    {
        return new Chain(UnitConversion.InchToChain(distance.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        return new Inch(this.Value + value.ChangeTo<Inch>().Value);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        return new Inch(this.Value - value.ChangeTo<Inch>().Value);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        return new Inch(this.Value * value.ChangeTo<Inch>().Value);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        return new Inch(this.Value / value.ChangeTo<Inch>().Value);
    }

    public override LinearUnit Negate()
    {
        return new Inch(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Inch(this.Value);
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
            return this.Clone();

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
        return this.Value.CompareTo(other.ChangeTo<Inch>().Value);
    }

    #endregion
}

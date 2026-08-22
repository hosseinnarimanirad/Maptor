// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Metrics;

public class Mile : LinearUnit
{
    #region Fields&Properties

    public override LinearMode Mode
    {
        get { return LinearMode.Mile; }
    }

    #endregion

    #region Constructors

    public Mile() : this(0) { }

    public Mile(double value) : base(value) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (obj.GetType() == typeof(Mile) && obj.ToString() == this.ToString());
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Mile",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Operators

    public static bool operator ==(Mile firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Mile firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Mile firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Mile firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Mile firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Mile firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Meter(Mile distance)
    {
        return new Meter(UnitConversion.MileToMeter(distance.Value));//, LinearPrefix.Nothing);
    }

    public static explicit operator Yard(Mile distance)
    {
        return new Yard(UnitConversion.MileToYard(distance.Value));
    }

    public static explicit operator Foot(Mile distance)
    {
        return new Foot(UnitConversion.MileToFoot(distance.Value));
    }

    public static explicit operator Inch(Mile distance)
    {
        return new Inch(UnitConversion.MileToInch(distance.Value));
    }

    public static explicit operator Rod(Mile distance)
    {
        return new Rod(UnitConversion.MileToRod(distance.Value));
    }

    public static explicit operator Chain(Mile distance)
    {
        return new Chain(UnitConversion.MileToChain(distance.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        return new Mile(this.Value + value.ChangeTo<Mile>().Value);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        return new Mile(this.Value - value.ChangeTo<Mile>().Value);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        return new Mile(this.Value * value.ChangeTo<Mile>().Value);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        return new Mile(this.Value / value.ChangeTo<Mile>().Value);
    }

    public override LinearUnit Negate()
    {
        return new Mile(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Mile(this.Value);
    }

    public override LinearUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Meter))
            return (Meter)this;

        else if (typeof(T) == typeof(Mile))
            return this.Clone();

        else if (typeof(T) == typeof(Yard))
            return (Yard)this;

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
        return this.Value.CompareTo(other.ChangeTo<Mile>().Value);
    }

    #endregion
}

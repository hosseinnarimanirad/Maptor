// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Metrics;

public class Meter : LinearUnit
{
    #region Fields&Properties

    //private LinearPrefix m_Prefex;

    public override LinearMode Mode
    {
        get { return LinearMode.Meter; }
    }

    //public LinearPrefix Prefix
    //{

    //    get { return this.m_Prefex; }

    //    set { ChangePrefix(value); }
    //}

    #endregion

    #region Constructors

    public Meter() : this(0) { }//, LinearPrefix.Nothing) { }

    public Meter(double value) : base(value) { }

    public Meter(double value, LinearPrefix prefix)
        : base(value * Math.Pow(10, (int)(prefix))) { }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        return (obj.GetType() == typeof(Meter) && obj.ToString() == this.ToString());
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0} Meter",
                                this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        //(this.Prefix == LinearPrefix.Nothing ? string.Empty : this.Prefix.ToString()));
    }

    #endregion

    #region Operators

    public static bool operator ==(Meter firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) == 0);
    }

    public static bool operator !=(Meter firstValue, LinearUnit secondValue)
    {
        return (firstValue.CompareTo(secondValue) != 0);
    }

    public static bool operator <(Meter firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) < 0); }

    public static bool operator >(Meter firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) > 0); }

    public static bool operator <=(Meter firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) <= 0); }

    public static bool operator >=(Meter firstValue, LinearUnit secondValue)
    { return (firstValue.CompareTo(secondValue) >= 0); }

    public static explicit operator Mile(Meter distance)
    {
        Meter temp = (Meter)distance.Clone();

        //temp.Prefix = LinearPrefix.Nothing;

        return new Mile(UnitConversion.MeterToMile(temp.Value));
    }

    public static explicit operator Yard(Meter distance)
    {
        Meter temp = (Meter)distance.Clone();

        //temp.Prefix = LinearPrefix.Nothing;

        return new Yard(UnitConversion.MeterToYard(temp.Value));
    }

    public static explicit operator Foot(Meter distance)
    {
        Meter temp = (Meter)distance.Clone();

        //temp.Prefix = LinearPrefix.Nothing;

        return new Foot(UnitConversion.MeterToFoot(temp.Value));
    }

    public static explicit operator Inch(Meter distance)
    {
        Meter temp = (Meter)distance.Clone();

        //temp.Prefix = LinearPrefix.Nothing;

        return new Inch(UnitConversion.MeterToInch(temp.Value));
    }

    public static explicit operator Rod(Meter distance)
    {
        Meter temp = (Meter)distance.Clone();

        //temp.Prefix = LinearPrefix.Nothing;

        return new Rod(UnitConversion.MeterToRod(temp.Value));
    }

    public static explicit operator Chain(Meter distance)
    {
        Meter temp = (Meter)distance.Clone();

        //temp.Prefix = LinearPrefix.Nothing;

        return new Chain(UnitConversion.MeterToChain(temp.Value));
    }

    #endregion

    #region ILinearUnit Members

    public override LinearUnit Add(LinearUnit value)
    {
        Meter tempValue = (Meter)value.ChangeTo<Meter>();

        //tempValue.Prefix = this.Prefix;

        return new Meter(tempValue.Value + this.Value);//, this.Prefix);
    }

    public override LinearUnit Subtract(LinearUnit value)
    {
        Meter tempValue = (Meter)value.ChangeTo<Meter>();

        //tempValue.Prefix = this.Prefix;

        return new Meter(this.Value - tempValue.Value);//, this.Prefix);
    }

    public override LinearUnit Multiply(LinearUnit value)
    {
        Meter tempValue = (Meter)value.ChangeTo<Meter>();

        //tempValue.Prefix = this.Prefix;

        return new Meter(this.Value * tempValue.Value);//, this.Prefix);
    }

    public override LinearUnit Divide(LinearUnit value)
    {
        Meter tempValue = (Meter)value.ChangeTo<Meter>();

        //tempValue.Prefix = this.Prefix;

        return new Meter(this.Value / tempValue.Value);//, this.Prefix);
    }

    public override LinearUnit Negate()
    {
        return new Meter(-this.Value);
    }

    public override LinearUnit Clone()
    {
        return new Meter(this.Value);//, this.Prefix);
    }

    public override LinearUnit ChangeTo<T>()
    {
        if (typeof(T) == typeof(Meter))
            return this.Clone();

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
            return (Chain)this;
        else
            throw new NotImplementedException();
    }

    #endregion

    #region IComparable<ILinearUnit> Members

    public override int CompareTo(LinearUnit other)
    {
        Meter firstValue = this;
        Meter secondValue = (Meter)other.ChangeTo<Meter>();
        //firstValue.Prefix = secondValue.Prefix;

        return firstValue.Value.CompareTo(secondValue.Value);
    }

    #endregion

}

// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Globalization;

namespace IRI.Maptor.Core.Common.Metrics;

public struct AngleAdapter
{
    private readonly double MinValue, MaxValue, Correction;

    public AngleAdapter(double minValue, double maxValue, double correction)
    {
        this.MinValue = minValue;

        this.MaxValue = maxValue;

        this.Correction = correction;
    }

    public double Adopt(double value)
    {
        if (value > MaxValue)
        {
            return value - Math.Ceiling(Math.Abs(value - MaxValue) / Correction) * Correction;
        }
        else if (value < MinValue)
        {
            return value + Math.Ceiling(Math.Abs(value - MinValue) / Correction) * Correction;
        }
        else
            return value;
    }

    public override bool Equals(object obj)
    {
        return this.ToString() == obj.ToString();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "MinValue: {0}, MaxValue: {1}, Correction: {3}",
                                MinValue,
                                MaxValue,
                                Correction);
    }

    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    public static bool operator ==(AngleAdapter firstValue, AngleAdapter secondValue)
    {
        return firstValue.Equals(secondValue);
    }

    public static bool operator !=(AngleAdapter firstValue, AngleAdapter secondValue)
    {
        return !(firstValue == secondValue);
    }

}

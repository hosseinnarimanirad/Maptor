using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Primitives;

public class PointM : Point, IHasM
{
    public double M { get; set; }

    public PointM()
    {
    }

    public PointM(double x, double y, double m) : base(x, y)
    {
        M = m;
    }

    public override byte[]? AsWkb()
    {
        if (IsNaN())
            return null;

        byte[] result = new byte[29];

        result[0] = (byte)WkbByteOrder.WkbNdr;

        Array.Copy(BitConverter.GetBytes((int)WkbGeometryType.PointM), 0, result, 1, BaseConversionHelper.IntegerSize);

        Array.Copy(BitConverter.GetBytes(X), 0, result, 5, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(Y), 0, result, 13, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(M), 0, result, 21, BaseConversionHelper.DoubleSize);

        return result;
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}", X, Y, M);
    }

    public override bool HaveTheSameXY(object obj)
    {
        var point = obj as PointM;

        if (point is null)
            return false;

        return this.X == point.X && this.Y == point.Y;
    }

    public override bool Equals(object obj)
    {
        if (obj?.GetType() == typeof(PointM))
        {
            PointM temp = (PointM)obj;

            return temp.X == this.X && temp.Y == this.Y && temp.M == this.M;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, M);
    }

    public override string AsDelimited(char delimiter, int precision, bool useThousandSeparator)
    {
        var xFormatted = FormatHelper.FormatWithPrecision(X, precision, useThousandSeparator);

        var yFormatted = FormatHelper.FormatWithPrecision(Y, precision, useThousandSeparator);

        var mFormatted = FormatHelper.FormatWithPrecision(M, precision, useThousandSeparator);

        return $"{xFormatted}{delimiter}{yFormatted}{delimiter}{mFormatted}";
    }
}

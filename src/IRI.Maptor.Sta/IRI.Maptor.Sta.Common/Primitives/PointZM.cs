using System;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Common.Primitives;

public class PointZM : IPoint, IHasZ, IHasM
{
    //private double _x;
    //public double X
    //{
    //    get { return _x; }
    //    set { _x = value; }
    //}

    public double X { get; set; }

    //private double _y;

    //public double Y
    //{
    //    get { return _y; }
    //    set { _y = value; }
    //}
    public double Y { get; set; }

    //private double _z;
    //public double Z
    //{
    //    get { return _z; }
    //    set { _z = value; }
    //}

    public double Z { get; set; }

    public double M { get; set; }


    //private PointType _type;

    //public PointType Type => _type;

    //public bool HasM() => Type.HasFlag(PointType.PointM);

    //public bool HasZ() => Type.HasFlag(PointType.PointZ);


    public PointZM()
    {
    }

    public PointZM(double x, double y, double z)
    {
        X = x;

        Y = y;

        Z = z;
    }



    public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", X, Y, Z, M);

    public string AsExactString() => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17}", X, Y, Z);

    public bool AreExactlyTheSame(object obj)
    {
        if (obj.GetType() != typeof(PointZM))
            return false;

        return this.AsExactString() == ((PointZM)obj).AsExactString();
    }

    public static double GetDistance(PointZM first, PointZM second)
    {
        return Math.Sqrt((first.X - second.X) * (first.X - second.X) +
                            (first.Y - second.Y) * (first.Y - second.Y) +
                            (first.Z - second.Z) * (first.Z - second.Z));
    }

    public double DistanceTo(IPoint point)
    {
        if (point is PointZM)
        {
            return GetDistance(this, (PointZM)point);
        }
        else
        {
            return Math.Sqrt((X - point.X) * (X - point.X) +
                                (Y - point.Y) * (Y - point.Y));
        }
    }


    public byte[] AsWkb()
    {
        byte[] result = new byte[29];

        result[0] = (byte)WkbByteOrder.WkbNdr;

        Array.Copy(BitConverter.GetBytes((int)WkbGeometryType.Point), 0, result, 1, BaseConversionHelper.IntegerSize);

        Array.Copy(BitConverter.GetBytes(X), 0, result, 5, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(Y), 0, result, 13, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(Z), 0, result, 21, BaseConversionHelper.DoubleSize);

        return result;
    }

    public byte[] AsByteArray()
    {
        // Option #3
        Span<byte> buffer = stackalloc byte[16];  // Stack-allocated, no heap allocation

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        BitConverter.TryWriteBytes(buffer.Slice(16, 8), Z);

        return buffer.ToArray();  // Only allocates when creating final array
    }

    public bool IsNaN() => double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z);

    #region Static Methods and Operators

    public static PointZM NaN
    {
        get { return new PointZM(double.NaN, double.NaN, double.NaN); }
    }

    public static PointZM operator -(PointZM p1, PointZM p2)
    {
        return new PointZM(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
    }

    public static PointZM operator +(PointZM p1, PointZM p2)
    {
        return new PointZM(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
    }

    public static explicit operator Point(PointZM point)
    {
        return new Point(point.X, point.Y);
    }

    #endregion


}

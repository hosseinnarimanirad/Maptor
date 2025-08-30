using System;
using System.Linq;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Extensions;


namespace IRI.Maptor.Sta.Common.Primitives;

public class Point : IPoint
{
    public double X { get; set; }

    public double Y { get; set; }

    public Point(double x, double y)
    {
        this.X = x;

        this.Y = y;
    }

    public Point()
    {
        this.X = 0;

        this.Y = 0;
    }

    //public static double GetDistance(Point first, Point second)
    //{
    //    return Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));
    //}

    //public static double EuclideanDistance(IPoint first, IPoint second)
    //{
    //    return
    //        Math.Sqrt((first.X - second.X) * (first.X - second.X) +
    //                    (first.Y - second.Y) * (first.Y - second.Y));
    //}

    public static Point FromPolar(double radius, double angleInRadian)
    {
        return new Point(
            radius * Math.Cos(angleInRadian),
            radius * Math.Sin(angleInRadian));
    }

    public double DistanceTo(IPoint point)
    {
        double dx = this.X - point.X;

        double dy = this.Y - point.Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}", this.X, this.Y);
    }

    public bool IsNaN()
    {
        return double.IsNaN(X) || double.IsNaN(Y);
    }

    public string AsExactString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:G17} {1:G17}", this.X, this.Y);
    }

    public bool AreExactlyTheSame(object obj)
    {
        if (obj.GetType() != typeof(Point))
        {
            return false;
        }

        return this.AsExactString() == ((Point)obj).AsExactString();
    }

    public bool AreTheSame(Point point, int precision)
    {
        return Math.Round(this.X, precision).Equals(Math.Round(point.X, precision)) &&
                Math.Round(this.Y, precision).Equals(Math.Round(point.Y, precision));
    }

    //public double DistanceTo(IPoint point)
    //{
    //    return GetDistance(this, new Point(point.X, point.Y));
    //}

    public static Point operator -(Point p1, Point p2)
    {
        return new Point(p2.X - p1.X, p2.Y - p1.Y);
    }

    public static Point operator +(Point p1, Point p2)
    {
        return new Point(p1.X + p2.X, p1.Y + p2.Y);
    }

    public static Point NaN { get; } = new Point(double.NaN, double.NaN);

    public static Point PositiveInfinity { get; } = new Point(double.PositiveInfinity, double.PositiveInfinity);

    public override bool Equals(object obj)
    {
        if (obj?.GetType() == typeof(Point))
        {
            Point temp = (Point)obj;

            return temp.X == this.X && temp.Y == this.Y;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public byte[] AsWkb()
    {
        byte[] result = new byte[21];

        result[0] = (byte)WkbByteOrder.WkbNdr;

        Array.Copy(BitConverter.GetBytes((int)WkbGeometryType.Point), 0, result, 1, BaseConversionHelper.IntegerSize);

        Array.Copy(BitConverter.GetBytes(X), 0, result, 5, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(Y), 0, result, 13, BaseConversionHelper.DoubleSize);

        return result;
    }
     

    public virtual string AsWkt()
    {
        return FormattableString.Invariant($"POINT({X.ToInvariantString()} {Y.ToInvariantString()}");
    }

    public static Point Parse(double[] values, bool isLongitudeFirst)
    {
        if (values == null || values.Count() != 2)
        {
            throw new NotImplementedException();
        }

        if (isLongitudeFirst)
        {
            return new Point(values[0], values[1]);
        }
        else
        {
            return new Point(values[1], values[0]);
        }
    }

    public static T Parse<T>(double[] values, bool isLongitudeFirst) where T : IPoint, new()
    {
        var point = Parse(values, isLongitudeFirst);

        T result = new T()
        {
            X = point.X,
            Y = point.Y
        };

        return result;
    }

    public static Point Create(double x, double y)
    {
        return new Point(x, y);
    }

    //public static bool operator ==(Point first, Point second)
    //{
    //    return first.Equals(second);
    //}

    //public static bool operator !=(Point first, Point second)
    //{
    //    return !first.Equals(second);
    //}


    //public Microsoft.SqlServer.Types.SqlGeometry AsSqlGeometry()
    //{
    //    return Microsoft.SqlServer.Types.SqlGeometry.Parse(
    //        new System.Data.SqlTypes.SqlString(string.Format("POINT({0} {1})", this.X.ToString(), this.Y.ToString())));
    //}


    public byte[] AsByteArray()
    {
        // Option #1
        //byte[] result = new byte[16];

        //Array.Copy(BitConverter.GetBytes(X), 0, result, 0, 8);

        //Array.Copy(BitConverter.GetBytes(Y), 0, result, 8, 8);

        //return result;


        // Option #2
        //byte[] result = new byte[16];

        //BitConverter.TryWriteBytes(result.AsSpan(0, 8), X);

        //BitConverter.TryWriteBytes(result.AsSpan(8, 8), Y);

        //return result;


        // Option #3
        Span<byte> buffer = stackalloc byte[16];  // Stack-allocated, no heap allocation

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        return buffer.ToArray();  // Only allocates when creating final array
    }


}

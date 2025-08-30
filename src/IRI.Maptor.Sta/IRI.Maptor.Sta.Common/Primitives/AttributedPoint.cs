// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;

[Serializable]
public struct AttributedPoint : IPoint
{
    private double _x, _y, _value;

    private readonly int code;

    public double X
    {
        get { return _x; }

        set { _x = value; }
    }

    public double Y
    {
        get { return _y; }

        set { _y = value; }
    }

    public double Value
    {
        get { return _value; }

        set { _value = value; }
    }
     
    public AttributedPoint(double x, double y, double value)
    {
        _x = x;

        _y = y;

        _value = value;         
    }

    public override string ToString()
    {
        return string.Format("Coordinate: X:{0}, Y:{1}; Attribute:{2}", X, Y, Value);
    }
     
    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(AttributedPoint))
        {
            return ((AttributedPoint)obj).X == X &&
                    ((AttributedPoint)obj).Y == Y &&
                    ((AttributedPoint)obj).Value == Value;
        }

        return false;
    }

    public bool AreTheSame(AttributedPoint point, int precision)
    {
        return Math.Round(X, precision).Equals(Math.Round(point.X, precision)) &&
                Math.Round(Y, precision).Equals(Math.Round(point.Y, precision)) &&
                Math.Round(Value, precision).Equals(Math.Round(point.Value, precision));
    }

    public double CalculateDistance(AttributedPoint nextPoint)
    {
        return Math.Sqrt((X - nextPoint.X) * (X - nextPoint.X) + (Y - nextPoint.Y) * (Y - nextPoint.Y));
    }

    public bool AreExactlyTheSame(object obj)
    {
        throw new NotImplementedException();
    }

    public double DistanceTo(IPoint point)
    {
        throw new NotImplementedException();
    }

    public byte[] AsWkb()
    {
        throw new NotImplementedException();
    }


    public byte[] AsByteArray()
    {
        // Option #3
        Span<byte> buffer = stackalloc byte[16];  // Stack-allocated, no heap allocation

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        return buffer.ToArray();  // Only allocates when creating final array
    }

    public bool IsNaN()
    {
        return double.IsNaN(X) || double.IsNaN(Y);
    }
}

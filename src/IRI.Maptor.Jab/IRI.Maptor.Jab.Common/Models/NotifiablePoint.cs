using System;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Models;

public class NotifiablePoint : Notifier, IPoint
{
    private double _x;

    public double X
    {
        get { return _x; }
        set
        {
            _x = value;
            RaisePropertyChanged();

            if (IsRaiseCoordinateChangeEnabled)
            {
                RaiseCoordinateChangedAction?.Invoke(this);
            }

        }
    }


    private double _y;

    public double Y
    {
        get { return _y; }
        set
        {
            _y = value;
            RaisePropertyChanged();

            if (IsRaiseCoordinateChangeEnabled)
            {
                RaiseCoordinateChangedAction?.Invoke(this);
            }

        }
    }

    public NotifiablePoint()
    {

    }

    public NotifiablePoint(double x, double y, Action<NotifiablePoint> changeAction)
    {
        this.X = x;

        this.Y = y;

        this.RaiseCoordinateChangedAction = changeAction;
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

    public Action<NotifiablePoint> RaiseCoordinateChangedAction { get; set; }

    private bool _isRaiseCoordinateChangeEnabled = true;

    public bool IsRaiseCoordinateChangeEnabled
    {
        get { return _isRaiseCoordinateChangeEnabled; }
        set { _isRaiseCoordinateChangeEnabled = value; }
    }


    public bool IsNaN()
    {
        return double.IsNaN(X) || double.IsNaN(Y);
    }

    public byte[] AsByteArray()
    {
        // Option #3
        Span<byte> buffer = stackalloc byte[16];  // Stack-allocated, no heap allocation

        BitConverter.TryWriteBytes(buffer.Slice(0, 8), X);

        BitConverter.TryWriteBytes(buffer.Slice(8, 8), Y);

        return buffer.ToArray();  // Only allocates when creating final array
    }
}

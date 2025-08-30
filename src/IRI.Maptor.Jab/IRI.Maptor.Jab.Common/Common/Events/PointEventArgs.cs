using System;

namespace IRI.Maptor.Jab.Common.Events;

/// <summary>
/// Point is a (long, lat) pair on WGS84
/// </summary>
public class PointEventArgs : EventArgs
{
    public System.Windows.Point Point { get; set; }

    public PointEventArgs(System.Windows.Point point)
    {
        Point = point;
    }
}

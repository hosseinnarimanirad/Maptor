using System;
using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Events;

public class PointArrayEventArgs : EventArgs
{
    public List<Sta.Common.Primitives.Point> Coordinates { get; set; }

    public bool IsClosed { get; set; }

    public PointArrayEventArgs(List<Sta.Common.Primitives.Point> coordinates, bool isClosed)
    {
        Coordinates = coordinates;

        IsClosed = isClosed;
    }
}

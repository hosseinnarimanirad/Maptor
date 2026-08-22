using System;
using System.Collections.Generic;

namespace IRI.Maptor.Presentation.Wpf.Events;

public class PointArrayEventArgs : EventArgs
{
    public List<IRI.Maptor.Core.Common.Primitives.Point> Coordinates { get; set; }

    public bool IsClosed { get; set; }

    public PointArrayEventArgs(List<IRI.Maptor.Core.Common.Primitives.Point> coordinates, bool isClosed)
    {
        Coordinates = coordinates;

        IsClosed = isClosed;
    }
}

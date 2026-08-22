using System;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Events;

public class ZoomToPointEventArgs : EventArgs
{
    public double MapScale { get; set; }

    public Point Center { get; set; }

    public ZoomToPointEventArgs(double mapScale, Point center)
    {
        MapScale = mapScale;

        Center = center;
    }
}

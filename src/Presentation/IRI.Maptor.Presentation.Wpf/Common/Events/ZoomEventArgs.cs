using System;

namespace IRI.Maptor.Presentation.Wpf.Events;

public class ZoomEventArgs : EventArgs
{
    public double ZoomLevel { get; set; }

    //public double MapScale { get; set; }

    public ZoomEventArgs(double zoomLevel, double mapScale)
    {
        ZoomLevel = zoomLevel;

        //MapScale = mapScale;
    }

    public static ZoomEventArgs EmptyArg => new ZoomEventArgs(double.NaN, double.NaN);
}

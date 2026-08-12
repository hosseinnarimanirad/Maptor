using System;

namespace IRI.Maptor.Jab.Wpf.Events;

public class MapActionEventArgs : EventArgs
{
    public MapAction Action { get; set; }

    public MapActionEventArgs(MapAction action)
    {
        Action = action;
    }
}

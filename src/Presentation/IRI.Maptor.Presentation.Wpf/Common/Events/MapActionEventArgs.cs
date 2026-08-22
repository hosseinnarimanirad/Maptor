using System;

namespace IRI.Maptor.Presentation.Wpf.Events;

public class MapActionEventArgs : EventArgs
{
    public MapAction Action { get; set; }

    public MapActionEventArgs(MapAction action)
    {
        Action = action;
    }
}

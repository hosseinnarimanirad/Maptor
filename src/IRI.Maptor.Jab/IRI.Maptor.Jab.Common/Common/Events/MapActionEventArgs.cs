using System;

namespace IRI.Maptor.Jab.Common.Events;

public class MapActionEventArgs : EventArgs
{
    public MapAction Action { get; set; }

    public MapActionEventArgs(MapAction action)
    {
        Action = action;
    }
}

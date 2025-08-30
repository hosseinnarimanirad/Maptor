using System;

namespace IRI.Maptor.Jab.Common.Events;

public class MapStatusEventArgs : EventArgs
{
    public MapStatus Status { get; set; }

    public MapStatusEventArgs(MapStatus status)
    {
        Status = status;
    }
}

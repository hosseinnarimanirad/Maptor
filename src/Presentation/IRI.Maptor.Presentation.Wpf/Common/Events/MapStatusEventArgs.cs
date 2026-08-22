using System;

namespace IRI.Maptor.Presentation.Wpf.Events;

public class MapStatusEventArgs : EventArgs
{
    public MapStatus Status { get; set; }

    public MapStatusEventArgs(MapStatus status)
    {
        Status = status;
    }
}

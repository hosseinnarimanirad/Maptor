using System;
using System.Windows;

namespace IRI.Maptor.Jab.Common.Events;

public class PanEventArgs : EventArgs
{
    public Point Offset { get; set; }

    public PanEventArgs(Point offset)
    {
        Offset = offset;
    }
}

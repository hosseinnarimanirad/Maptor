using System;
using Geometry = IRI.Maptor.Core.Spatial.Primitives.Geometry<IRI.Maptor.Core.Common.Primitives.Point>;


namespace IRI.Maptor.Presentation.Wpf.Events;

public class GeometryEventArgs : EventArgs
{
    public Geometry Geometry { get; set; }

    public GeometryEventArgs(Geometry geometry)
    {
        Geometry = geometry;
    }
}

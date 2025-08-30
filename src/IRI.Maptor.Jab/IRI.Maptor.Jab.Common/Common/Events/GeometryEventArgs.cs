using System;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;


namespace IRI.Maptor.Jab.Common.Events;

public class GeometryEventArgs : EventArgs
{
    public Geometry Geometry { get; set; }

    public GeometryEventArgs(Geometry geometry)
    {
        Geometry = geometry;
    }
}

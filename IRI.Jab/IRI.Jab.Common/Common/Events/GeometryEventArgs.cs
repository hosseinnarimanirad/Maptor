﻿using System;
using Geometry = IRI.Sta.Spatial.Primitives.Geometry<IRI.Sta.Common.Primitives.Point>;


namespace IRI.Jab.Common;

public class GeometryEventArgs : EventArgs
{
    public Geometry Geometry { get; set; }

    public GeometryEventArgs(Geometry geometry)
    {
        this.Geometry = geometry;
    }
}

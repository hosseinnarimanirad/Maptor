﻿using System.Collections.Generic;

using IRI.Sta.Common.Primitives;
using IRI.Sta.Spatial.Primitives;

namespace IRI.Jab.Common.Model;

public class GeometryLabelPairs
{
    public List<Geometry<Point>> Geometries { get; set; }

    public List<string> Labels { get; set; }

    public GeometryLabelPairs(List<Geometry<Point>> geometries, List<string> labels)
    {
        this.Geometries = geometries;

        this.Labels = labels;
    }

}

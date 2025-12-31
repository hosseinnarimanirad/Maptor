using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class PointAddress
{
    public int LocalPointIndex { get; set; }

    public int PartIndex { get; set; }

    public int? PolygonIndex { get; set; }

    public PointAddress(int? polygonIndex, int partIndex, int localPointIndex)
    {
        PolygonIndex = polygonIndex;
        PartIndex = partIndex;
        LocalPointIndex = localPointIndex;
    }
}

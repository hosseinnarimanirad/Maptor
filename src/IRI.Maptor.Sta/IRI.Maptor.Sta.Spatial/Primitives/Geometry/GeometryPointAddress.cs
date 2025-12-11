using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class GeometryPointAddress
{
    public int LocalPointIndex { get; set; }

    public int PartIndex { get; set; }

    public int? PolygonIndex { get; set; }

    public GeometryPointAddress(int? polygonIndex, int partIndex, int localPointIndex)
    {
        PolygonIndex = polygonIndex;
        PartIndex = partIndex;
        LocalPointIndex = localPointIndex;
    }
}

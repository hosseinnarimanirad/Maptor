using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public interface IGeometry
{
    GeometryType Type { get; set; }

    int NumberOfPoints { get; }

    int NumberOfGeometries { get; }

    int TotalNumberOfPoints { get; }

    int Srid { get; set; }

    bool HasZ();

    bool HasM();

    string AsWkt();

    byte[]? AsWkb();
     
}

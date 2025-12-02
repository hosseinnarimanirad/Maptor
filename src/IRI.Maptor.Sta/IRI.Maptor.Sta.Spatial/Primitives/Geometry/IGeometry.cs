using IRI.Maptor.Sta.Common.Enums;
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

    List<Point> GetPoints();

    bool IsLeafGeometry();

    bool HasZ();

    bool HasM();

    string AsWkt();

    byte[]? AsWkb();

    byte[]? AsSqlServerNativeBinary();

    string AsSqlServerWkt();

    CoordinateDimension GetDimension();

    bool IsValid();

    bool IsEmpty();
}

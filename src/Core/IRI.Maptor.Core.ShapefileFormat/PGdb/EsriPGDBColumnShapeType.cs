using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.PersonalGdb;

public enum EsriPGDBColumnShapeType
{
    Null = 0,
    Point = 1,
    Multipoint = 2,
    Polyline = 3,
    Polygon = 4,
    MultiPatch = 9
}

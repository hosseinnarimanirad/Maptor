using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Spatial.IO.OgcSFA;

public static class WktConstants
{
    public const string EmptyPoint = "POINT EMPTY";
    public const string EmptyLineString = "LINESTRING EMPTY";
    public const string EmptyPolygon = "POLYGON EMPTY";

    public const string EmptyMultiPoint = "MULTIPOINT EMPTY";
    public const string EmptyMultiLineString = "MULTILINESTRING EMPTY";
    public const string EmptyMultiPolygon = "MULTIPOLYGON EMPTY";

    public const string EmptyGeometryCollection = "GEOMETRYCOLLECTION EMPTY";
}

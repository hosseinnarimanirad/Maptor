using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public enum CoordinateDisplayMode
{
    UTM = 0,
    WebMercator = 1,
    GeodeticDecimal = 2,
    GeodeticDms = 3,
    Mercator = 4,
    TM = 5,
    CylindricalEqualArea=6
}

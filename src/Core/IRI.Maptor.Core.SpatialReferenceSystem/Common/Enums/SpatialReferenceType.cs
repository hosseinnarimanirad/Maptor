using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public enum SpatialReferenceType
{
    None = 1,

    AlbersEqualAreaConic = 2,
    AzimuthalEquidistant = 3,
    CylindricalEqualArea = 4,
    LambertConformalConic = 5,
    Mercator = 6,
    TransverseMercator = 7,
    UTM = 8,
    WebMercator = 9,

    Geodetic = 100,
}

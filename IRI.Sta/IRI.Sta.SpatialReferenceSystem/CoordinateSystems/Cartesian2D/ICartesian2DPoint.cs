﻿// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Sta.Metrics;
using IRI.Sta.SpatialReferenceSystem;

namespace IRI.Sta.SpatialReferenceSystem;

public interface ICartesian2DPoint
{
    LinearMode LinearMode { get; }

    CoordinateRegion Region { get; }

    LinearUnit X { get; set; }

    LinearUnit Y { get; set; }

    PolarPoint<TRadius, TAngle> ToPolar<TRadius, TAngle>(AngleRange range)
        where TRadius : LinearUnit, new()
        where TAngle : AngularUnit, new();

}

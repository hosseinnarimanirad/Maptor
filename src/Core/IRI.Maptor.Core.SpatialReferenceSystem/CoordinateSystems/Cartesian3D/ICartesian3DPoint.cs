// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface ICartesian3DPoint
{
    LinearMode LinearMode { get; }

    GeodeticPoint<TLinear, TAngular> ToGeodetic<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange longitudinalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    SphericalPoint<TLinear, TAngular> ToSpherical<TLinear, TAngular>(AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    ICartesian3DPoint Negate();

    LinearUnit X { get; set; }

    LinearUnit Y { get; set; }

    LinearUnit Z { get; set; }
}

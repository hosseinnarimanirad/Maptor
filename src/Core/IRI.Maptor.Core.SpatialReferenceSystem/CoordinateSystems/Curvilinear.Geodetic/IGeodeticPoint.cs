// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface IGeodeticPoint
{
    AngleMode AngularMode { get; }

    IEllipsoid Datum { get; }

    LinearUnit Height { get; set; }

    AngularUnit Latitude { get; set; }

    LinearMode LinearMode { get; }

    AngularUnit Longitude { get; set; }

    AngleRange LongitudinalRange { get; set; }

    Cartesian3DPoint<T> ToCartesian<T>() where T : LinearUnit, new();
}

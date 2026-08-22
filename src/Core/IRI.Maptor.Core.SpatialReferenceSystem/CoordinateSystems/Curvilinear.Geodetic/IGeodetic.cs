// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface IGeodetic : IEnumerable<IGeodeticPoint>
{
    string Name { get; }
    int NumberOfPoints { get; }
    AngleMode AngularMode { get; }
    IEllipsoid Datum { get; }
    int Dimension { get; }
    AxisType Handedness { get; }
    LinearMode LinearMode { get; }
    AngleRange LongitudinalRange { get; set; }
    ILinearCollection Height { get; }
    IAngularCollection Latitude { get; }
    IAngularCollection Longitude { get; }
    IGeodeticPoint this[int index] { get; set; }

    IGeodetic RotateAboutX(AngularUnit value, RotateDirection direction);
    IGeodetic RotateAboutY(AngularUnit value, RotateDirection direction);
    IGeodetic RotateAboutZ(AngularUnit value, RotateDirection direction);
    IGeodetic Shift(ISphericalPoint newBase);
    Matrix ToMatrix();
    IGeodetic Clone();

    Cartesian3D<T> GetCartesianForm<T>() where T : LinearUnit, new();

    Geodetic<TNewLinear, TNewAngular> ChangeTo<TNewLinear, TNewAngular>()
        where TNewLinear : LinearUnit, new()
        where TNewAngular : AngularUnit, new();
}

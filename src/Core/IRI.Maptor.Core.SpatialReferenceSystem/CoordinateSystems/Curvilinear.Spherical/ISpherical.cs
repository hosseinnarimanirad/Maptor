// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface ISpherical : IEnumerable<ISphericalPoint>
{
    AngleMode AngularMode { get; }
    AngleRange HorizontalRange { get; set; }
    LinearMode LinearMode { get; }
    string Name { get; }
    int NumberOfPoints { get; }
    int Dimension { get; }
    AxisType Handedness { get; }
    IAngularCollection HorizontalAngle { get; }
    IAngularCollection VerticalAngle { get; }
    ILinearCollection Radius { get; }

    ISphericalPoint this[int index] { get; set; }

    ISpherical RotateAboutX(AngularUnit value, RotateDirection direction);
    ISpherical RotateAboutY(AngularUnit value, RotateDirection direction);
    ISpherical RotateAboutZ(AngularUnit value, RotateDirection direction);
    ISpherical Shift(ISphericalPoint newBase);
    Matrix ToMatrix();
    ISpherical Clone();

    Cartesian3D<T> ToCartesian<T>() where T : LinearUnit, new();

    Spherical<TNewLinear, TNewAngular> ChangeTo<TNewLinear, TNewAngular>()
        where TNewLinear : LinearUnit, new()
        where TNewAngular : AngularUnit, new();

}

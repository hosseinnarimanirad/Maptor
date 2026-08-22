// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface ICartesian3D : IEnumerable<ICartesian3DPoint>
{
    string Name { get; }
    int Dimension { get; }
    int NumberOfPoints { get; }
    AxisType Handedness { get; }
    LinearMode LinearMode { get; }
    ILinearCollection X { get; }
    ILinearCollection Y { get; }
    ILinearCollection Z { get; }

    ICartesian3DPoint this[int index] { get; set; }

    ICartesian3D Transform(Matrix rotation, AxisType newHandedness);
    ICartesian3D Transform(AngularUnit rotationAboutX, AngularUnit rotationAboutY, AngularUnit rotationAboutZ, ICartesian3DPoint translation);
    ICartesian3D Transform(Matrix rotation, ICartesian3DPoint translation);
    ICartesian3D Transform(Matrix rotation, ICartesian3DPoint translation, AxisType newHandedness);
    ICartesian3D Rotate(AngularUnit rotationAboutX, AngularUnit rotationAboutY, AngularUnit rotationAboutZ);
    ICartesian3D Rotate(Matrix rotationMatrix);
    ICartesian3D RotateAboutX(AngularUnit value, RotateDirection direction);
    ICartesian3D RotateAboutY(AngularUnit value, RotateDirection direction);
    ICartesian3D RotateAboutZ(AngularUnit value, RotateDirection direction);
    ICartesian3D Shift(ICartesian3DPoint newBase);
    Matrix ToMatrix();
    ICartesian3D Clone();
    
    Astronomical<TAngular> ToAstronomicForm<TAngular>(AngleRange horizontalRange) where TAngular : AngularUnit, new();

    Ellipsoidal<TLinear, TAngular> ToEllipsoidalForm<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    Geodetic<TLinear, TAngular> ToGeodeticForm<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange longitudinalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    Spherical<TLinear, TAngular> ToSphericalForm<TLinear, TAngular>(AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    Cartesian3D<TNewType> ChangeTo<TNewType>() where TNewType : LinearUnit, new();

}

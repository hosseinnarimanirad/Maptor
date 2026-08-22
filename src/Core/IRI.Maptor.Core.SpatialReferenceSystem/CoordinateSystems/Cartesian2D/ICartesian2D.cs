// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface ICartesian2D : IEnumerable<ICartesian2DPoint>
{
    int Dimension { get; }
    AxisType Handedness { get; }
    LinearMode LinearMode { get; }
    string Name { get; }
    int NumberOfPoints { get; }
    ILinearCollection X { get; }
    ILinearCollection Y { get; }

    ICartesian2DPoint this[int index] { get; set; }

    IEnumerator<ICartesian2DPoint> GetEnumerator();

    ICartesian2D Rotate(AngularUnit rotateAngle, RotateDirection direction);
    ICartesian2D Shift(ICartesian2DPoint newBase);
    ICartesian2D Clone();
    Matrix ToMatrix();

    Polar<TLinear, TAngular> ToPolar<TLinear, TAngular>(AngleRange range)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    Cartesian2D<TNewType> ChangeTo<TNewType>() where TNewType : LinearUnit, new();

}

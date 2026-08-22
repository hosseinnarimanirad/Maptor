// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface IAstronomical : IEnumerable<IAstronomicalPoint>
{
    AxisType Handedness { get; }
    IAngularCollection HorizontalAngle { get; }
    AngleRange HorizontalAngleRange { get; set; }
    string Name { get; }
    int NumberOfPoints { get; }
    AngleMode AngularMode { get; }

    int Dimension { get; }
    IEnumerator<IAstronomicalPoint> GetEnumerator();

    IAstronomical RotateAboutX(AngularUnit value, RotateDirection direction);
    IAstronomical RotateAboutY(AngularUnit value, RotateDirection direction);
    IAstronomical RotateAboutZ(AngularUnit value, RotateDirection direction);
    IAstronomicalPoint this[int index] { get; set; }
    Cartesian3D<T> ToCartesian<T>() where T : LinearUnit, new();
    IAstronomical Clone();
    Matrix ToMatrix();
    IAngularCollection VerticalAngle { get; }
}

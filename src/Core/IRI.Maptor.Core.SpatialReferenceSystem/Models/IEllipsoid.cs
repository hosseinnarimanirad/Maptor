// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Metrics;
using IRI.Maptor.Core.SpatialReferenceSystem.Models;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public interface IEllipsoid
{
    string Name { get; }

    string EsriName { get; set; }

    LinearUnit SemiMajorAxis { get; }

    LinearUnit SemiMinorAxis { get; }

    double FirstEccentricity { get; }

    double SecondEccentricity { get; }

    double Flattening { get; }

    double InverseFlattening { get; }

    int Srid { get; }

    ICartesian3DPoint DatumTranslation { get; }

    OrientationParameter DatumMisalignment { get; }

    LinearUnit CalculateM(AngularUnit Latitude);

    LinearUnit CalculateN(AngularUnit Latitude);

    double CalculateN(double Latitude);

    Ellipsoid<TLinear, TAngular> ChangeTo<TLinear, TAngular>()
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new();

    bool AreTheSame(IEllipsoid other);
}

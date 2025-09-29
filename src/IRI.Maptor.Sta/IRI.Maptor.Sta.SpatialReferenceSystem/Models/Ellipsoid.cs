// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.SpatialReferenceSystem.Models;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public struct Ellipsoid<TLinear, TAngular> : IEllipsoid
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private Cartesian3DPoint<TLinear> _datumTranslation;
     
    private OrientationParameter _datumMisalignment;

    private LinearUnit _semiMajorAxis;

    private LinearUnit _semiMinorAxis;

    private string _name;

    private double _firstEccentricity;

    private double _secondEccentricity;

    private int _srid;

    #endregion

    #region Properties

    public ICartesian3DPoint DatumTranslation => _datumTranslation;

    public OrientationParameter DatumMisalignment => _datumMisalignment;

    public LinearUnit SemiMajorAxis => _semiMajorAxis;

    public LinearUnit SemiMinorAxis => _semiMinorAxis;

    public string Name => _name;

    public string EsriName { get; set; }

    public double FirstEccentricity
    {
        get { return _firstEccentricity; }
    }

    public double SecondEccentricity
    {
        get { return _secondEccentricity; }
    }

    public double Flattening
    {
        get
        {
            return (SemiMajorAxis.Value - SemiMinorAxis.Value) / SemiMajorAxis.Value;
        }
    }

    public double InverseFlattening
    {
        get
        {
            return SemiMajorAxis.Value / (SemiMajorAxis.Value - SemiMinorAxis.Value);
        }
    }

    public int Srid
    {
        get
        {
            return _srid;
        }
    }

    #endregion

    #region Constructors

    public Ellipsoid(
            string name, 
            LinearUnit semiMajorAxis, 
            double inverseFlattening, 
            int srid)
        : this(name,
                semiMajorAxis,
                inverseFlattening,
                new Cartesian3DPoint<TLinear>(new TLinear(), new TLinear(), new TLinear()),
                new OrientationParameter(new Radian(), new Radian(), new Radian()),
                srid)
    { }

    public Ellipsoid(
            string name, 
            LinearUnit semiMajorAxis, 
            LinearUnit semiMinorAxis,
            ICartesian3DPoint datumTranslation, 
            OrientationParameter datumMisalignment, 
            int srid)
        : this(name,
                semiMajorAxis,
                1.0 / semiMajorAxis.Subtract(semiMinorAxis).Divide(semiMajorAxis).Value,
                datumTranslation,
                datumMisalignment,
                srid)
    { }

    public Ellipsoid(
            string name, 
            LinearUnit semiMajorAxis, 
            double inverseFlattening,
            ICartesian3DPoint datumTranslation, 
            OrientationParameter datumMisalignment, 
            int srid)
    {
        _datumTranslation = new Cartesian3DPoint<TLinear>(datumTranslation.X, datumTranslation.Y, datumTranslation.Z);

        _datumMisalignment = new OrientationParameter(datumMisalignment.Omega.ChangeTo<TAngular>(),
                                                            datumMisalignment.Phi.ChangeTo<TAngular>(),
                                                            datumMisalignment.Kappa.ChangeTo<TAngular>());

        _name = name;

        _srid = srid;

        _semiMajorAxis = semiMajorAxis.ChangeTo<TLinear>();

        double tempSemiMajor = _semiMajorAxis.Value;

        if (inverseFlattening == 0)
        {
            _semiMinorAxis = new TLinear() { Value = tempSemiMajor };
        }
        else
        {
            _semiMinorAxis = new TLinear() { Value = tempSemiMajor - tempSemiMajor / inverseFlattening };
        }

        double tempSemiMinor = _semiMinorAxis.Value;

        _firstEccentricity = Math.Sqrt((tempSemiMajor * tempSemiMajor - tempSemiMinor * tempSemiMinor)
                                               /
                                            (tempSemiMajor * tempSemiMajor));

        _secondEccentricity = Math.Sqrt((tempSemiMajor * tempSemiMajor - tempSemiMinor * tempSemiMinor)
                                               /
                                             (tempSemiMinor * tempSemiMinor));

        EsriName = string.Empty;
    }

    #endregion


    #region Methods
     
    public double CalculateN(double Latitude)
    {
        double sin = Math.Sin(Latitude * Math.PI / 180);

        return _semiMajorAxis.Value
                       /
                       Math.Sqrt(1 - FirstEccentricity * FirstEccentricity * sin * sin);
    }

    public LinearUnit CalculateN(AngularUnit Latitude)
    {
        TLinear result = new TLinear();

        result.Value = _semiMajorAxis.Value
                        /
                        Math.Sqrt(1 - FirstEccentricity * FirstEccentricity * Latitude.Sin * Latitude.Sin);

        return result;
    }

    public LinearUnit CalculateM(AngularUnit Latitude)
    {
        TLinear result = new TLinear();

        result.Value = _semiMajorAxis.Value * (1 - FirstEccentricity * FirstEccentricity)
                        /
                        Math.Pow(1 - FirstEccentricity * FirstEccentricity * Latitude.Sin * Latitude.Sin, 3.0 / 2.0);

        return result;
    }

    public bool AreTheSame(IEllipsoid other)
    {
        return
            other.SemiMajorAxis.GetType() == SemiMajorAxis.GetType() &&
            SemiMajorAxis.Value == other.SemiMajorAxis.Value &&
                FirstEccentricity == other.FirstEccentricity;
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(IEllipsoid))
        {
            return this == (IEllipsoid)obj;
        }

        return false;
    }

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => Name;

    public Ellipsoid<TNewLinearType, TNewAngularType> ChangeTo<TNewLinearType, TNewAngularType>()
        where TNewLinearType : LinearUnit, new()
        where TNewAngularType : AngularUnit, new()
    {
        return new Ellipsoid<TNewLinearType, TNewAngularType>(string.Empty,
                                                            SemiMajorAxis,
                                                            SemiMinorAxis,
                                                            DatumTranslation,
                                                            DatumMisalignment,
                                                            Srid);
    }

    public Ellipsoid<TLinear, TAngular> GetGeocentricVersion(int newSrid)
    {
        return new Ellipsoid<TLinear, TAngular>(Name + "_Geocentric", SemiMajorAxis, InverseFlattening, newSrid);
    }

    #endregion


    #region Operators

    public static bool operator ==(Ellipsoid<TLinear, TAngular> firstEllipsoid, IEllipsoid secondEllipsoid)
    {
        return firstEllipsoid.DatumTranslation == secondEllipsoid.DatumTranslation &&
                firstEllipsoid.DatumMisalignment == secondEllipsoid.DatumMisalignment &&
                firstEllipsoid.Name == secondEllipsoid.Name &&
                firstEllipsoid.SemiMajorAxis == secondEllipsoid.SemiMajorAxis &&
                firstEllipsoid.SemiMinorAxis == secondEllipsoid.SemiMinorAxis;
    }

    public static bool operator !=(Ellipsoid<TLinear, TAngular> firstEllipsoid, IEllipsoid secondEllipsoid) => !(firstEllipsoid == secondEllipsoid);

    #endregion
}

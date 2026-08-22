using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Primitives;
using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

public abstract class SrsBase
{
    private string _title;

    public string Title
    {
        get { return _title; }
        set { _title = value; }
    }

    public int Srid
    {
        get { return GetSrid(); }
    }

    protected abstract int GetSrid();

    public string DatumName { get; set; }

    protected Ellipsoid _ellipsoid;

    public Ellipsoid Ellipsoid
    {
        get { return _ellipsoid; }
    }

    public abstract SpatialReferenceType Type { get; }

    public SrsBase()
    {

    }

    //public CoordinateReferenceSystemBase() : this(Ellipsoids.WGS84)
    //{

    //}
    //public CoordinateReferenceSystemBase(Ellipsoid ellipsoid) : this(string.Empty, ellipsoid)
    //{

    //}

    public SrsBase(string title, Ellipsoid ellipsoid)
    {
        this.Title = title;

        this._ellipsoid = ellipsoid;
    }


    public abstract T ToGeodetic<T>(T point) where T : IPoint, new();

    public T ToWgs84Geodetic<T>(T point) where T : IPoint, new()
    {
        return ToGeodetic(point, Ellipsoids.WGS84);
    }

    //must be tested
    public T ToLocalGeodetic<T>(T point) where T : IPoint, new()
    {
        var geocentricGeodeticPoint = ToGeodetic(point);

        return Transformations.ChangeDatum(geocentricGeodeticPoint, this.Ellipsoid.GetGeocentricVersion(0), this.Ellipsoid);
    }

    public T ToGeodetic<T>(T point, Ellipsoid targetEllipsoid) where T : IPoint, new()
    {
        var temp = ToGeodetic(point);

        return ToTargetEllipsoid(temp, targetEllipsoid);
    }


    public abstract T FromGeodetic<T>(T point) where T : IPoint, new();

    public T FromWgs84Geodetic<T>(T point) where T : IPoint, new()
    {
        return FromGeodetic(point, Ellipsoids.WGS84);
    }

    //must be tested
    public T FromLocalGeodetic<T>(T point) where T : IPoint, new()
    {
        var geocentricGeodeticPoint = Transformations.ChangeDatum(point, this.Ellipsoid, this.Ellipsoid.GetGeocentricVersion(0));

        return FromGeodetic(geocentricGeodeticPoint);
    }

    public T FromGeodetic<T>(T point, Ellipsoid sourceEllipsoid) where T : IPoint, new()
    {
        var temp = FromSourceEllipsoid(point, sourceEllipsoid);

        return FromGeodetic(temp);
    }


    protected T ToTargetEllipsoid<T>(T point, Ellipsoid targetEllipsoid) where T : IPoint, new()
    {
        if (targetEllipsoid != this.Ellipsoid ||
            targetEllipsoid.SemiMajorAxis.Value != this.Ellipsoid.SemiMajorAxis.Value ||
            targetEllipsoid.InverseFlattening != this.Ellipsoid.InverseFlattening)
        {
            return Transformations.ChangeDatumSimple(point, this._ellipsoid, targetEllipsoid);
        }
        else
        {
            return point;
        }
    }

    protected T FromSourceEllipsoid<T>(T point, Ellipsoid sourceEllipsoid) where T : IPoint, new()
    {
        if (sourceEllipsoid != this.Ellipsoid ||
            sourceEllipsoid.SemiMajorAxis.Value != this.Ellipsoid.SemiMajorAxis.Value ||
            sourceEllipsoid.InverseFlattening != this.Ellipsoid.InverseFlattening)
        {
            return Transformations.ChangeDatumSimple(point, sourceEllipsoid, this._ellipsoid);
        }
        else
        {
            return point;
        }
    }


    public static bool operator ==(SrsBase? first, SrsBase? second)
    {
        if (first is null && second is null)
            return true;

        if ((first is null) ^ (second is null))
            return false;

        if (first.Srid != second.Srid)
            return false;

        // more checks

        return true;
    }

    public static bool operator !=(SrsBase? first, SrsBase? second) => !(first == second);

    public static SrsBase? Create(int srid) => SridHelper.AsSrsBase(srid);

    public static SrsBase? Create(CoordinateDisplayMode mode, int? utmZone)
    {
        switch (mode)
        {
            case CoordinateDisplayMode.UTM:
                return UTM.CreateForZone(Ellipsoids.WGS84, utmZone!.Value);

            case CoordinateDisplayMode.WebMercator:
                return SrsBases.WebMercator;

            case CoordinateDisplayMode.GeodeticDecimal:
            case CoordinateDisplayMode.GeodeticDms:
                return SrsBases.GeodeticWgs84;

            case CoordinateDisplayMode.Mercator:
            case CoordinateDisplayMode.TM:
            case CoordinateDisplayMode.CylindricalEqualArea:
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

}

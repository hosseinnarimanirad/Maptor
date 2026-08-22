using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

public class Mercator : MapProjectionBase
{
    public override SpatialReferenceType Type
    {
        get
        {
            return SpatialReferenceType.Mercator;
        }
    }

    public Mercator() : this(Ellipsoids.WGS84)
    {
    }

    public Mercator(Ellipsoid ellipsoid, int srid = 0) : base(string.Empty, ellipsoid, srid)
    {
    }

    public override TPoint FromGeodetic<TPoint>(TPoint point)
    {
        return MapProjects.GeodeticToMercator(point, this._ellipsoid);
    }

    public override TPoint ToGeodetic<TPoint>(TPoint point)
    {
        return MapProjects.MercatorToGeodetic(point, this._ellipsoid);
    }

    protected override int GetSrid()
    {
        return _srid;
    }
}

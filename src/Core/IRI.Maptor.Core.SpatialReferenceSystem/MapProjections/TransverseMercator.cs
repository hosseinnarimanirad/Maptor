using IRI.Maptor.Core.Common.Primitives;
using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

public class TransverseMercator : MapProjectionBase
{
    //readonly double _falseEasting, _falseNorthing, _centralMeridian, _scaleFactor = 1.0, _latitude_Of_Origin;

    public override SpatialReferenceType Type
    {
        get
        {
            return SpatialReferenceType.TransverseMercator;
        }
    }

    public TransverseMercator() : this(Ellipsoids.WGS84)
    {
    }

    public TransverseMercator(Ellipsoid ellipsoid) : this(ellipsoid, 0, 0, 0, 0, 1)
    {
    }

    public TransverseMercator(Ellipsoid ellipsoid, double centralMeridian, double latitudeOfOrigin, double falseEasting, double falseNorthing, double scaleFactor, int srid = 0)
        : base(string.Empty, ellipsoid, srid)
    {
        this._falseEasting = falseEasting;

        this._falseNorthing = falseNorthing;

        this._centralMeridian = centralMeridian;

        this._scaleFactor = scaleFactor;

        this._latitudeOfOrigin = latitudeOfOrigin;
    }

    public override TPoint FromGeodetic<TPoint>(TPoint point)
    {
        var tempLongitude = point.X - _centralMeridian;

        var tempLatitude = point.Y - _latitudeOfOrigin;

        var result = MapProjects.GeodeticToTransverseMercator(new Point(tempLongitude, tempLatitude), this._ellipsoid);

        return new TPoint() { X = result.X * _scaleFactor + _falseEasting, Y = result.Y * _scaleFactor + _falseNorthing };
    }

    public override TPoint ToGeodetic<TPoint>(TPoint point)
    {
        //return MapProjects.TransverseMercatorToGeodetic(point, this._ellipsoid);

        double tempX = (point.X - _falseEasting) / _scaleFactor;

        double tempY = (point.Y - _falseNorthing) / _scaleFactor;

        Point result = MapProjects.TransverseMercatorToGeodetic(new Point(tempX, tempY), _ellipsoid);

        return new TPoint()
        {
            X = result.X + _centralMeridian,
            Y = result.Y + _latitudeOfOrigin
        };
    }


    protected override int GetSrid()
    {
        return _srid;
    }
}

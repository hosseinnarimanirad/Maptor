using IRI.Maptor.Core.Common.Metrics;
using IRI.Maptor.Core.Common.Primitives;
using System;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

public class LambertConformalConic2P : MapProjectionBase
{
    //readonly double _latitude0, _latitude1, _latitude2, _longitude0, _falseEasting, _falseNorthing;

    //readonly IEllipsoid _ellipsoid;
    private readonly double n, F, rho0;

    public override SpatialReferenceType Type
    {
        get
        {
            return SpatialReferenceType.LambertConformalConic;
        }
    }

    public LambertConformalConic2P(Ellipsoid<Meter, Degree> ellipsoid,
                                    double standardParallel1,
                                    double standardParallel2,
                                    double centralMeridian,
                                    double latitudeOfOrigin,
                                    double falseEasting = 0,
                                    double falseNorthing = 0,
                                    double scaleFactor = 1.0,
                                    int srid = 0)
    {
        this._srid = srid;

        this._ellipsoid = ellipsoid;

        this._latitudeOfOrigin = latitudeOfOrigin;

        this._standardParallel1 = standardParallel1;

        this._standardParallel2 = standardParallel2;

        this._centralMeridian = centralMeridian;

        this._falseEasting = falseEasting;

        this._falseNorthing = falseNorthing;

        this._scaleFactor = scaleFactor;

        //this.scaleFactor = 1;
        double m1 = MapProjects.CalculateM(ellipsoid.FirstEccentricity, standardParallel1 * Math.PI / 180.0);
        double m2 = MapProjects.CalculateM(ellipsoid.FirstEccentricity, standardParallel2 * Math.PI / 180.0);

        double t0 = GeodeticLatitudeToT(latitudeOfOrigin, ellipsoid.FirstEccentricity);
        double t1 = GeodeticLatitudeToT(standardParallel1, ellipsoid.FirstEccentricity);
        double t2 = GeodeticLatitudeToT(standardParallel2, ellipsoid.FirstEccentricity);

        this.n = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));

        this.F = m1 / (n * Math.Pow(t1, n));

        this.rho0 = this._scaleFactor * ellipsoid.SemiMajorAxis.Value * this.F * Math.Pow(t0, n);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="latitude">Must be in Degree</param>
    /// <param name="firstEccentricity"></param>
    /// <returns></returns>
    protected double GeodeticLatitudeToT(double latitude, double firstEccentricity)
    {
        //Limit the latitude value
        if (Math.Abs(latitude) > MapProjects._MaxConvertibleToIsometricLatitude)
        {
            latitude = MapProjects._MaxConvertibleToIsometricLatitude * (latitude < 0 ? -1 : 1);
        }

        double angleInRadian = latitude * Math.PI / 180;

        double eSin = firstEccentricity * Math.Sin(angleInRadian);

        return Math.Tan(Math.PI / 4.0 - angleInRadian / 2.0) / Math.Pow(((1 - eSin) / (1 + eSin)), firstEccentricity / 2.0);
    }

    public Point LCCToGeodeticIterative(Point lccPoint)
    {
        double x = lccPoint.X - this._falseEasting;
        double y = lccPoint.Y - this._falseNorthing;
        double a = this._ellipsoid.SemiMajorAxis.Value;
        double e = this._ellipsoid.FirstEccentricity;

        double rho = Math.Sqrt(x * x + (rho0 - y) * (rho0 - y));

        double t = Math.Pow(rho / (this._scaleFactor * a * this.F), 1.0 / this.n) * ((this.n > 0) ? 1 : -1);

        double zeta = Math.PI / 2.0 - 2.0 * Math.Atan(t);

        double theta = Math.Atan(x / (rho0 - y));

        double lambda = (theta / this.n) * 180.0 / Math.PI + this._centralMeridian;

        double tempPhi = Math.PI / 2.0 - 2 * Math.Atan(t);

        double phi;

        int counter = 0;

        do
        {
            phi = tempPhi;

            double eSin = e * Math.Sin(phi);

            tempPhi = Math.PI / 2.0 - 2 * Math.Atan(t * Math.Pow((1 - eSin) / (1 + eSin), e / 2.0));

            counter++;

            if (counter > 10)
            {
                throw new NotImplementedException();
            }

        } while ((tempPhi - phi) > 1E-10);

        phi = phi * 180.0 / Math.PI;

        return new Point(lambda, phi);
    }

    public override TPoint ToGeodetic<TPoint>(TPoint lccPoint)
    {
        double x = lccPoint.X - this._falseEasting;
        double y = lccPoint.Y - this._falseNorthing;
        double a = this._ellipsoid.SemiMajorAxis.Value;
        double e = this._ellipsoid.FirstEccentricity;

        double rho = Math.Sqrt(x * x + (rho0 - y) * (rho0 - y)) * ((this.n > 0) ? 1 : -1);

        double t = Math.Pow(rho / (this._scaleFactor * a * this.F), 1.0 / this.n);

        double zeta = Math.PI / 2.0 - 2.0 * Math.Atan(t);

        //Here x is deltaY and (rho0 - y) is deltaX
        double theta = Math.Atan2(x, (rho0 - y));

        double lambda = (theta / this.n) * 180.0 / Math.PI + this._centralMeridian;

        double e2 = e * e;
        double e4 = e2 * e2;
        double e6 = e4 * e2;
        double e8 = e4 * e4;

        double phi = zeta +
                    (e * e / 2.0 + 5 * e4 / 24.0 + e6 / 12.0 + 13 * e8 / 360.0) * Math.Sin(2 * zeta) +
                    (7.0 * e4 / 48.0 + 29.0 * e6 / 240.0 + 811.0 * e8 / 11520.0) * Math.Sin(4 * zeta) +
                    (7.0 * e6 / 120.0 + 81.0 * e8 / 1120) * Math.Sin(6 * zeta) +
                    (4279 * e8 / 161280.0) * Math.Sin(8 * zeta);

        return new TPoint() { X = lambda, Y = phi * 180.0 / Math.PI };
    }

    public override TPoint FromGeodetic<TPoint>(TPoint geodeticPoint)
    {
        double a = this._ellipsoid.SemiMajorAxis.Value;

        double e = this._ellipsoid.FirstEccentricity;

        double rho = this._scaleFactor * a * this.F * Math.Pow(GeodeticLatitudeToT(geodeticPoint.Y, e), this.n);

        double theta = this.n * (geodeticPoint.X - this._centralMeridian) * Math.PI / 180.0;

        double x = rho * Math.Sin(theta) + this._falseEasting;

        double y = rho0 - rho * Math.Cos(theta) + this._falseNorthing;

        return new TPoint() { X = x, Y = y };
    }


    protected override int GetSrid()
    {
        return _srid;
    }
}

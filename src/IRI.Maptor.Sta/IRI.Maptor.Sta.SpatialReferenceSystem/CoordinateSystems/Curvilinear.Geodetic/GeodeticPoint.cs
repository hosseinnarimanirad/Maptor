// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Metrics;
using System.Collections.Generic;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public struct GeodeticPoint<TLinear, TAngular> : IGeodeticPoint
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private IEllipsoid m_Datum;

    private LinearUnit m_Height;

    private AngularUnit m_Latitude;

    private AngularUnit m_Longitude;

    #endregion

    #region Properties

    public IEllipsoid Datum
    {
        get { return this.m_Datum; }
    }

    public LinearUnit Height
    {
        get { return m_Height; }
        set { m_Height = value; }
    }

    public AngularUnit Latitude
    {
        get { return m_Latitude; }
        set { m_Latitude = value; }
    }

    public AngularUnit Longitude
    {
        get { return m_Longitude; }
        set { m_Longitude = value; }
    }

    public LinearMode LinearMode
    {
        get { return Height.Mode; }
    }

    public AngleMode AngularMode
    {
        get { return Latitude.Mode; }
    }

    public AngleRange LongitudinalRange
    {
        get { return Longitude.Range; }
        set { Longitude.Range = value; }
    }

    #endregion

    #region Constructors

    public GeodeticPoint(IEllipsoid ellipsoid, LinearUnit height, AngularUnit longitude, AngularUnit latitude)
    {
        if (latitude.Range != AngleRange.MinusPIToPI)
        {
            latitude.Range = AngleRange.MinusPIToPI;
        }

        this.m_Datum = ellipsoid.ChangeTo<TLinear,TAngular>();

        this.m_Height = height.ChangeTo<TLinear>();

        this.m_Latitude = latitude.ChangeTo<TAngular>();

        this.m_Longitude = longitude.ChangeTo<TAngular>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(ISphericalPoint))
        {
            return this == (IGeodeticPoint)obj;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "Height: {0}, Longitude: {1}, Latitude: {2}, Associated Ellipsoid: {3}",
                                this.Height,
                                this.Longitude,
                                this.Latitude,
                                this.Datum);
    }

    public Cartesian3DPoint<T> ToCartesian<T>() where T : LinearUnit, new()
    {
        T x = new T();

        T y = new T();

        T z = new T();

        double tempSemiMinor = Datum.SemiMinorAxis.ChangeTo<T>().Value;

        double tempSemiMajor = Datum.SemiMajorAxis.ChangeTo<T>().Value;

        //double tempOrigionX = this.AssociatedEllipsoid.Origion.X.ChangeTo<T>().Value;

        //double tempOrigionY = this.AssociatedEllipsoid.Origion.Y.ChangeTo<T>().Value;

        //double tempOrigionZ = this.AssociatedEllipsoid.Origion.Z.ChangeTo<T>().Value;

        //Matrix rotationMatrix = Transformation.CalculateEulerElementMatrix(AssociatedEllipsoid.Omega, AssociatedEllipsoid.Phi, AssociatedEllipsoid.Kappa);

        //Matrix transferMatrix = new Matrix(new double[][] { new double[] { tempOrigionX, tempOrigionY, tempOrigionZ } });

        double tempHeight = this.Height.ChangeTo<T>().Value;

        double tempN = this.Datum.CalculateN(this.Latitude).ChangeTo<T>().Value;

        double longCos = this.Longitude.Cos; double longSin = this.Longitude.Sin;

        double latCos = this.Latitude.Cos; double latSin = this.Latitude.Sin;

        //Matrix tempGeodetic = new Matrix(new double[][]{new double[]{(tempHeight + tempN) * latCos * longCos,
                                                                            //(tempHeight + tempN) * latCos * longSin,
                                                                            //(tempHeight + tempN * tempSemiMinor * tempSemiMinor / (tempSemiMajor * tempSemiMajor)) * latSin}});

        //Matrix tempResult = rotationMatrix * tempGeodetic + transferMatrix;

        //x.Value = tempResult[0, 0];

        //y.Value = tempResult[1, 0];

        //z.Value = tempResult[2, 0];

        x.Value = (tempHeight + tempN) * latCos * longCos;

        y.Value = (tempHeight + tempN) * latCos * longSin;

        z.Value = (tempHeight + tempN * tempSemiMinor * tempSemiMinor / (tempSemiMajor * tempSemiMajor)) * latSin;

        return new Cartesian3DPoint<T>(x, y, z);
    }

    #endregion

    #region Operators

    public static bool operator ==(GeodeticPoint<TLinear, TAngular> firstPoint, IGeodeticPoint secondPoint)
    {
        return (firstPoint.Height == secondPoint.Height &&
                firstPoint.Latitude == secondPoint.Latitude &&
                firstPoint.Longitude == secondPoint.Longitude &&
                firstPoint.Datum == secondPoint.Datum);
    }

    public static bool operator !=(GeodeticPoint<TLinear, TAngular> firstPoint, IGeodeticPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    #endregion
}

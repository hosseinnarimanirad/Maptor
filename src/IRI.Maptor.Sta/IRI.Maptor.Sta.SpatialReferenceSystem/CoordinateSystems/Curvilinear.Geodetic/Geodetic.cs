// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public class Geodetic<TLinear, TAngular> : IGeodetic
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private string m_Name;

    private AxisType m_Handedness;

    private const int m_Dimension = 2;

    private Ellipsoid<TLinear, TAngular> m_Datum;

    private ILinearCollection m_Height;

    private IAngularCollection m_Latitude;

    private IAngularCollection m_Longitude;

    #endregion

    #region Properties

    public string Name
    {
        get { return this.m_Name; }
    }

    public int Dimension
    {
        get { return m_Dimension; }
    }

    public IEllipsoid Datum
    {
        get { return this.m_Datum; }
    }

    public int NumberOfPoints
    {
        get { return this.Height.Length; }
    }

    public AxisType Handedness
    {
        get { return this.m_Handedness; }
    }

    public LinearMode LinearMode
    {
        get { return this.Height.Mode; }
    }

    public AngleMode AngularMode
    {
        get { return this.Latitude.Mode; }
    }

    public AngleRange LongitudinalRange
    {
        get { return this.Latitude.Range; }
        set { this.Latitude.Range = value; }
    }

    public ILinearCollection Height
    {
        get { return this.m_Height; }
    }

    public IAngularCollection Latitude
    {
        get { return this.m_Latitude; }
    }

    public IAngularCollection Longitude
    {
        get { return this.m_Longitude; }
    }

    #endregion

    #region Constructors&Indexers

    public IGeodeticPoint this[int index]
    {
        get { return new GeodeticPoint<TLinear, TAngular>(this.Datum, this.Height[index], this.Longitude[index], this.Latitude[index]); }

        set
        {
            if (this.Datum != value.Datum)
            {
                throw new NotImplementedException();
            }

            this.Height[index] = value.Height;

            this.Longitude[index] = value.Longitude;

            this.Latitude[index] = value.Latitude;
        }
    }

    public Geodetic(ILinearCollection height, IAngularCollection longitude, IAngularCollection latitude, IEllipsoid ellipsoid)
        : this(height, longitude, latitude, ellipsoid, AxisType.RightHanded) { }

    public Geodetic(ILinearCollection height, IAngularCollection longitude, IAngularCollection latitude, IEllipsoid ellipsoid, AxisType handedness)
        : this("Geodetic", height, longitude, latitude, ellipsoid, handedness) { }

    public Geodetic(string name, ILinearCollection height, IAngularCollection longitude, IAngularCollection latitude, IEllipsoid ellipsoid, AxisType handedness)
    {
        if (height.Length != longitude.Length || height.Length != latitude.Length)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_Datum = ellipsoid.ChangeTo<TLinear, TAngular>();

        this.m_Height = (LinearCollection<TLinear>)height.ChangeTo<TLinear>();

        this.m_Longitude = (AngularCollection<TAngular>)longitude.ChangeTo<TAngular>();

        this.m_Latitude = (AngularCollection<TAngular>)latitude.ChangeTo<TAngular>();
    }

    public Geodetic(string name, Matrix values, AxisType handedness, AngleRange longitudinalRange, IEllipsoid ellipsoid)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_Datum = ellipsoid.ChangeTo<TLinear, TAngular>();

        this.m_Height = new LinearCollection<TLinear>(values.GetColumn(0));

        this.m_Longitude = new AngularCollection<TAngular>(values.GetColumn(1), longitudinalRange);

        this.m_Latitude = new AngularCollection<TAngular>(values.GetColumn(2), AngleRange.MinusPIToPI);
    }

    #endregion

    #region Methods

    public IGeodetic RotateAboutX(AngularUnit value, RotateDirection direction)
    {
        ICartesian3D temp = this.GetCartesianForm<TLinear>().RotateAboutX(value, direction);

        return temp.ToGeodeticForm<TLinear, TAngular>(this.Datum, this.LongitudinalRange);
    }

    public IGeodetic RotateAboutY(AngularUnit value, RotateDirection direction)
    {
        ICartesian3D temp = this.GetCartesianForm<TLinear>().RotateAboutY(value, direction);

        return temp.ToGeodeticForm<TLinear, TAngular>(this.Datum, this.LongitudinalRange);
    }

    public IGeodetic RotateAboutZ(AngularUnit value, RotateDirection direction)
    {
        ICartesian3D temp = this.GetCartesianForm<TLinear>().RotateAboutZ(value, direction);

        return temp.ToGeodeticForm<TLinear, TAngular>(this.Datum, this.LongitudinalRange);
    }

    public IGeodetic Shift(ISphericalPoint newBase)
    {
        ICartesian3D temp = this.GetCartesianForm<TLinear>().Shift(newBase.ToCartesian<TLinear>());

        return temp.ToGeodeticForm<TLinear, TAngular>(this.Datum, this.LongitudinalRange);
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.Height.ToArray(),
                                            this.Longitude.ToArray(),
                                            this.Latitude.ToArray()});
    }

    public IGeodetic Clone()
    {
        return new Geodetic<TLinear, TAngular>(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                            "Copy of {0}", this.Name),
                                            this.Height.Clone(),
                                            this.Longitude.Clone(),
                                            this.Latitude.Clone(),
                                            this.Datum,
                                            this.Handedness);
    }

    public Cartesian3D<T> GetCartesianForm<T>()
        where T : LinearUnit, new()
    {
        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> z = new LinearCollection<T>(this.NumberOfPoints);

        double tempE = this.Datum.FirstEccentricity;

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            AngularUnit templatitude = this.Latitude[i];

            AngularUnit templongitude = this.Longitude[i];

            double tempHeight = this.Height[i].ChangeTo<T>().Value;

            double tempN = this.Datum.CalculateN(templatitude).ChangeTo<T>().Value;

            x.SetTheValue(i, (tempHeight + tempN) * templatitude.Cos * templongitude.Cos);

            y.SetTheValue(i, (tempHeight + tempN) * templatitude.Cos * templongitude.Sin);

            z.SetTheValue(i, (tempHeight + tempN * (1 - tempE * tempE)) * templatitude.Sin);
        }

        return new Cartesian3D<T>(x, y, z, this.Handedness);
    }

    
    public Geodetic<TNewLinear, TNewAngular> ChangeTo<TNewLinear, TNewAngular>()
        where TNewLinear : LinearUnit, new()
        where TNewAngular : AngularUnit, new()
    {
        return new Geodetic<TNewLinear, TNewAngular>(this.Height.ChangeTo<TNewLinear>(),
                                                this.Longitude.ChangeTo<TNewAngular>(),
                                                this.Latitude.ChangeTo<TNewAngular>(),
                                                this.Datum.ChangeTo<TNewLinear, TNewAngular>());
    }

    #endregion

    #region IEnumerable<IGeodeticPoint> Members

    public IEnumerator<IGeodeticPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (GeodeticPoint<TLinear, TAngular>)this[i];
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator(); ;
    }

    #endregion
}

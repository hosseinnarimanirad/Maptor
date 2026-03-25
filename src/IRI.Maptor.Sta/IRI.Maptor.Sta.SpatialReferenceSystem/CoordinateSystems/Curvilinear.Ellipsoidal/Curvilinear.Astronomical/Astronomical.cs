// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public class Astronomical<TAngular> : IAstronomical
    where TAngular : AngularUnit, new()
{
    #region Fields

    private string m_Name;

    private AxisType m_Handedness;

    private const int m_Dimension = 3;

    private IAngularCollection m_HorizontalAngle;

    private IAngularCollection m_VerticalAngle;

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

    public int NumberOfPoints
    {
        get { return this.HorizontalAngle.Length; }
    }

    public AxisType Handedness
    {
        get { return this.m_Handedness; }
    }

    public AngleMode AngularMode
    {
        get { return this.HorizontalAngle.Mode; }
    }

    public AngleRange HorizontalAngleRange
    {
        get { return this.HorizontalAngle.Range; }
        set { this.HorizontalAngle.Range = value; }
    }

    public IAngularCollection HorizontalAngle
    {
        get { return this.m_HorizontalAngle; }
    }

    public IAngularCollection VerticalAngle
    {
        get { return this.m_VerticalAngle; }
    }

    #endregion

    #region Constructors&Indexers

    public IAstronomicalPoint this[int index]
    {
        get { return new AstronomicalPoint<TAngular>(this.HorizontalAngle[index], this.VerticalAngle[index]); }

        set
        {
            this.HorizontalAngle[index] = value.HorizontalAngle;

            this.VerticalAngle[index] = value.VerticalAngle;
        }
    }

    public Astronomical(IAngularCollection horizontalAngle, IAngularCollection verticalAngle)
        : this(horizontalAngle, verticalAngle, AxisType.RightHanded) { }

    public Astronomical(IAngularCollection horizontalAngle, IAngularCollection verticalAngle, AxisType handedness)
        : this("Astronomic", horizontalAngle, verticalAngle, handedness) { }

    public Astronomical(string name, IAngularCollection horizontalAngle, IAngularCollection verticalAngle, AxisType handedness)
    {
        if (horizontalAngle.Length != verticalAngle.Length)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_HorizontalAngle = (AngularCollection<TAngular>)horizontalAngle.ChangeTo<TAngular>();

        this.m_VerticalAngle = (AngularCollection<TAngular>)verticalAngle.ChangeTo<TAngular>();
    }

    public Astronomical(string name, Matrix values, AxisType handedness, AngleRange horizontalRange)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_HorizontalAngle = new AngularCollection<TAngular>(values.GetColumn(0), horizontalRange);

        this.m_VerticalAngle = new AngularCollection<TAngular>(values.GetColumn(1), AngleRange.MinusPIToPI);
    }

    #endregion

    #region Methods

    public IAstronomical RotateAboutX(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<Meter> temp = (Cartesian3D<Meter>)this.ToCartesian<Meter>().RotateAboutX(value, direction);

        return temp.ToAstronomicForm<TAngular>(this.HorizontalAngleRange);
    }

    public IAstronomical RotateAboutY(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<Meter> temp = (Cartesian3D<Meter>)this.ToCartesian<Meter>().RotateAboutY(value, direction);

        return temp.ToAstronomicForm<TAngular>(this.HorizontalAngleRange);
    }

    public IAstronomical RotateAboutZ(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<Meter> temp = (Cartesian3D<Meter>)this.ToCartesian<Meter>().RotateAboutZ(value, direction);

        return temp.ToAstronomicForm<TAngular>(this.HorizontalAngleRange);
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.HorizontalAngle.ToArray(),
                                            this.VerticalAngle.ToArray()});
    }

    public IAstronomical Clone()
    {
        return new Astronomical<TAngular>(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                            "Copy of {0}", this.Name),
                                            this.HorizontalAngle.Clone(),
                                            this.VerticalAngle.Clone(),
                                            this.Handedness);
    }

    public Cartesian3D<T> ToCartesian<T>()
        where T : LinearUnit, new()
    {

        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> z = new LinearCollection<T>(this.NumberOfPoints);

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            AngularUnit tempVerticalAngle = this.VerticalAngle[i];

            AngularUnit tempHorizontalAngle = this.HorizontalAngle[i];

            x.SetTheValue(i, tempVerticalAngle.Cos * tempHorizontalAngle.Cos);

            y.SetTheValue(i, tempVerticalAngle.Cos * tempHorizontalAngle.Sin);

            z.SetTheValue(i, tempVerticalAngle.Sin);
        }

        return new Cartesian3D<T>(x, y, z, this.Handedness);
    }

    #endregion

    #region IEnumerable<IAstronomicPoint> Members

    public IEnumerator<IAstronomicalPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (AstronomicalPoint<TAngular>)this[i];
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

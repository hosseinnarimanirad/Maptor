// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public class Spherical<TLinear, TAngular> : ISpherical
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private string m_Name;

    private AxisType m_Handedness;

    private const int m_Dimension = 3;

    private ILinearCollection m_Radius;

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
        get { return this.Radius.Length; }
    }

    public AxisType Handedness
    {
        get { return this.m_Handedness; }
    }

    public LinearMode LinearMode
    {
        get { return this.Radius.Mode; }
    }

    public AngleMode AngularMode
    {
        get { return this.HorizontalAngle.Mode; }
    }

    public AngleRange HorizontalRange
    {
        get { return this.HorizontalAngle.Range; }
        set { this.HorizontalAngle.Range = value; }
    }

    public ILinearCollection Radius
    {
        get { return this.m_Radius; }
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

    public ISphericalPoint this[int index]
    {
        get { return new SphericalPoint<TLinear, TAngular>(this.Radius[index], this.HorizontalAngle[index], this.VerticalAngle[index]); }

        set
        {
            this.Radius[index] = value.Radius;

            this.HorizontalAngle[index] = value.HorizontalAngle;

            this.VerticalAngle[index] = value.VerticalAngle;
        }
    }

    public Spherical(ILinearCollection radius, IAngularCollection horizontalAngle, IAngularCollection verticalAngle)
        : this(radius, horizontalAngle, verticalAngle, AxisType.RightHanded) { }

    public Spherical(ILinearCollection radius, IAngularCollection horizontalAngle, IAngularCollection verticalAngle, AxisType handedness)
        : this("Spherical", radius, horizontalAngle, verticalAngle, handedness) { }

    public Spherical(string name, ILinearCollection radius, IAngularCollection horizontalAngle, IAngularCollection verticalAngle, AxisType handedness)
    {
        if (radius.Length != horizontalAngle.Length || radius.Length != verticalAngle.Length)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_Radius = (LinearCollection<TLinear>)radius.ChangeTo<TLinear>();

        this.m_HorizontalAngle = (AngularCollection<TAngular>)horizontalAngle.ChangeTo<TAngular>();

        this.m_VerticalAngle = (AngularCollection<TAngular>)verticalAngle.ChangeTo<TAngular>();
    }

    public Spherical(string name, Matrix values, AxisType handedness, AngleRange horizontalRange)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_Radius = new LinearCollection<TLinear>(values.GetColumn(0));

        this.m_HorizontalAngle = new AngularCollection<TAngular>(values.GetColumn(1), horizontalRange);

        this.m_VerticalAngle = new AngularCollection<TAngular>(values.GetColumn(2), AngleRange.MinusPIToPI);
    }

    #endregion

    #region Methods

    public ISpherical RotateAboutX(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().RotateAboutX(value, direction);

        return temp.ToSphericalForm<TLinear, TAngular>(this.HorizontalRange);
    }

    public ISpherical RotateAboutY(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().RotateAboutY(value, direction);

        return temp.ToSphericalForm<TLinear, TAngular>(this.HorizontalRange);
    }

    public ISpherical RotateAboutZ(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().RotateAboutZ(value, direction);

        return temp.ToSphericalForm<TLinear, TAngular>(this.HorizontalRange);
    }

    public ISpherical Shift(ISphericalPoint newBase)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().Shift(newBase.ToCartesian<TLinear>());

        return temp.ToSphericalForm<TLinear, TAngular>(this.HorizontalRange);
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.Radius.ToArray(),
                                            this.HorizontalAngle.ToArray(),
                                            this.VerticalAngle.ToArray()});
    }

    public ISpherical Clone()
    {
        return new Spherical<TLinear, TAngular>(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                            "Copy of {0}", this.Name),
                                            this.Radius.Clone(),
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
            double tempRadius = this.Radius[i].ChangeTo<T>().Value;

            AngularUnit tempVerticalAngle = this.VerticalAngle[i];

            AngularUnit tempHorizontalAngle = this.HorizontalAngle[i];

            x.SetTheValue(i, tempRadius * tempVerticalAngle.Cos * tempHorizontalAngle.Cos);

            y.SetTheValue(i, tempRadius * tempVerticalAngle.Cos * tempHorizontalAngle.Sin);

            z.SetTheValue(i, tempRadius * tempVerticalAngle.Sin);
        }

        return new Cartesian3D<T>(x, y, z, this.Handedness);
    }

    public Spherical<TNewLinear, TNewAngular> ChangeTo<TNewLinear, TNewAngular>()
        where TNewLinear : LinearUnit, new()
        where TNewAngular : AngularUnit, new()
    {
        return new Spherical<TNewLinear, TNewAngular>(this.Radius.ChangeTo<TNewLinear>(),
                                                    this.HorizontalAngle.ChangeTo<TNewAngular>(),
                                                    this.VerticalAngle.ChangeTo<TNewAngular>());


    }

    #endregion

    #region IEnumerable<ISphericalPoint> Members

    public IEnumerator<ISphericalPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (SphericalPoint<TLinear, TAngular>)this[i];
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

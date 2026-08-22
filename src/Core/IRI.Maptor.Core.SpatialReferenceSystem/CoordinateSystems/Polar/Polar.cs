// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public class Polar<TLinear, TAngular> : IPolar
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{

    #region Fields

    private string m_Name;

    private AxisType m_Handedness;

    private const int m_Dimension = 2;

    private ILinearCollection m_Radius;

    private IAngularCollection m_Angle;

    #endregion

    #region Properties

    public string Name
    {
        get { return this.m_Name; }
    }

    public int Dimension
    {
        get { return m_Dimension; ; }
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
        get { return this.Angle.Mode; }
    }

    public AngleRange AngularRange
    {
        get { return this.Angle.Range; }
        set { this.Angle.Range = value; }
    }

    public ILinearCollection Radius
    {
        get { return this.m_Radius; }
    }

    public IAngularCollection Angle
    {
        get { return this.m_Angle; }
    }

    #endregion

    #region Constructors&Indexers

    public IPolarPoint this[int index]
    {
        get { return new PolarPoint<TLinear, TAngular>(this.Radius[index], this.Angle[index]); }

        set
        {
            this.Radius[index] = value.Radius;

            this.Angle[index] = value.Angle;
        }
    }

    public Polar(ILinearCollection radius, IAngularCollection angle)
        : this(radius, angle, AxisType.RightHanded) { }

    public Polar(ILinearCollection radius, IAngularCollection angle, AxisType handedness)
        : this("Polar", radius, angle, handedness) { }

    public Polar(string name, ILinearCollection radius, IAngularCollection angle, AxisType handedness)
    {
        if (radius.Length != angle.Length)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_Radius = (LinearCollection<TLinear>)radius.ChangeTo<TLinear>();

        this.m_Angle = (AngularCollection<TAngular>)angle.ChangeTo<TAngular>();
    }

    public Polar(string name, Matrix values, AxisType handedness, AngleRange range)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_Radius = new LinearCollection<TLinear>(values.GetColumn(0));

        this.m_Angle = new AngularCollection<TAngular>(values.GetColumn(1), range);
    }

    #endregion

    #region Methods

    public IPolar Rotate(AngularUnit value, RotateDirection direction)
    {
        double tempValue = (int)direction * (int)Handedness * value.ChangeTo<TAngular>().Value;

        LinearCollection<TLinear> newRadius = (LinearCollection<TLinear>)this.Radius.Clone();

        AngularCollection<TAngular> newAngle = (AngularCollection<TAngular>)this.Angle.AddAllValuesWith(tempValue);

        return new Polar<TLinear, TAngular>(newRadius, newAngle, this.Handedness);
    }

    public IPolar Shift(IPolarPoint newBase)
    {
        Cartesian2D<TLinear> tempValue = (Cartesian2D<TLinear>)this.ToCartesian<TLinear>().Shift(newBase.ToCartesian<TLinear>());

        return tempValue.ToPolar<TLinear, TAngular>(this.AngularRange);
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.Radius.ToArray(), this.Angle.ToArray() });
    }

    public IPolar Clone()
    {
        return new Polar<TLinear, TAngular>(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                            "Copy of {0}", this.Name),
                                            this.Radius.Clone(),
                                            this.Angle.Clone(),
                                            this.Handedness);
    }

    public Cartesian2D<T> ToCartesian<T>()
        where T : LinearUnit, new()
    {

        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            double tempRadius = this.Radius[i].ChangeTo<T>().Value;

            x.SetTheValue(i, tempRadius * this.Angle[i].Cos);

            y.SetTheValue(i, tempRadius * this.Angle[i].Sin);
        }

        return new Cartesian2D<T>(x, y, this.Handedness);
    }

    public Polar<TNewLinear, TNewAngular> ChangeTo<TNewLinear, TNewAngular>()
        where TNewLinear : LinearUnit, new()
        where TNewAngular : AngularUnit, new()
    {
        return new Polar<TNewLinear, TNewAngular>(this.Radius.ChangeTo<TNewLinear>(),
                                                this.Angle.ChangeTo<TNewAngular>());
    }

    #endregion

    #region IEnumerable<IPolarPoint> Members

    public IEnumerator<IPolarPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (PolarPoint<TLinear, TAngular>)this[i];
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

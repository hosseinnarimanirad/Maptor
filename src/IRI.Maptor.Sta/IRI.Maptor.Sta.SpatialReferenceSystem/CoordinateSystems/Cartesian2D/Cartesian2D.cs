// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public class Cartesian2D<T> : ICartesian2D
    where T : LinearUnit, new()
{
    #region Fields

    private string m_Name;

    private AxisType m_Handedness;

    private const int m_Dimension = 2;

    private ILinearCollection m_X, m_Y;

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
        get { return this.X.Length; }
    }

    public AxisType Handedness
    {
        get { return this.m_Handedness; }
    }

    public LinearMode LinearMode
    {
        get { return this.X.Mode; }
    }

    public ILinearCollection X
    {
        get { return this.m_X; }
    }

    public ILinearCollection Y
    {
        get { return this.m_Y; }
    }

    #endregion

    #region Constructors&Indexers

    public ICartesian2DPoint this[int index]
    {
        get { return new Cartesian2DPoint<T>(this.X[index], this.Y[index]); }

        set
        {
            this.X[index] = value.X;

            this.Y[index] = value.Y;
        }
    }

    public Cartesian2D(ILinearCollection x, ILinearCollection y)
        : this("Cartesian2D", x, y, AxisType.RightHanded) { }

    public Cartesian2D(ILinearCollection x, ILinearCollection y, AxisType handedness)
        : this("Cartesian2D", x, y, handedness) { }

    public Cartesian2D(string name, ILinearCollection x, ILinearCollection y, AxisType handedness)
    {
        if (x.Length != y.Length)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_X = (LinearCollection<T>)x.ChangeTo<T>();

        this.m_Y = (LinearCollection<T>)y.ChangeTo<T>();
    }

    public Cartesian2D(string name, Matrix values, AxisType handedness, LinearPrefix prefix)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_X = new LinearCollection<T>(values.GetColumn(0));

        this.m_Y = new LinearCollection<T>(values.GetColumn(1));
    }

    #endregion

    #region Methods

    public IEnumerator<ICartesian2DPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (Cartesian2DPoint<T>)this[i];
        }
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.X.ToArray(), this.Y.ToArray() });
    }

    public ICartesian2D Clone()
    {
        return new Cartesian2D<T>(string.Format("{1}{2}", this.Name, ".Clone"),
                                            this.X.Clone(),
                                            this.Y.Clone(),
                                            this.Handedness);
    }

    public ICartesian2D Rotate(AngularUnit rotateAngle, RotateDirection direction)
    {
        LinearCollection<T> newX = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> newY = new LinearCollection<T>(this.NumberOfPoints);

        double sin = (int)direction * (int)this.Handedness * rotateAngle.Sin;

        double cos = rotateAngle.Cos;

        for (int i = 0; i < NumberOfPoints; i++)
        {
            newX.SetTheValue(i, X.GetTheValue(i) * cos + Y.GetTheValue(i) * sin);

            newY.SetTheValue(i, (-1) * X.GetTheValue(i) * sin + Y.GetTheValue(i) * cos);
        }

        return new Cartesian2D<T>(newX, newY, this.Handedness);
    }

    public ICartesian2D Shift(ICartesian2DPoint newBase)
    {
        double tempX = (newBase.X.ChangeTo<T>()).Value;

        double tempY = (newBase.Y.ChangeTo<T>()).Value;

        ILinearCollection newX = X.SubtractAllValuesWith(tempX);

        ILinearCollection newY = Y.SubtractAllValuesWith(tempY);

        return new Cartesian2D<T>(newX, newY, this.Handedness);
    }

    public Polar<TLinear, TAngular> ToPolar<TLinear, TAngular>(AngleRange range)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        ILinearCollection radius = new LinearCollection<TLinear>(this.NumberOfPoints);

        IAngularCollection angle = new AngularCollection<TAngular>(this.NumberOfPoints, range);

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            double tempX = this.X[i].ChangeTo<TLinear>().Value;

            double tempY = this.Y[i].ChangeTo<TLinear>().Value;

            radius.SetTheValue(i, Math.Sqrt(tempX * tempX + tempY * tempY));

            angle[i] = new Radian(Math.Atan2(tempY, tempX), AngleRange.MinusPIToPI);
        }

        return new Polar<TLinear, TAngular>(radius, angle, this.Handedness);
    }

    public Cartesian2D<TNewType> ChangeTo<TNewType>() where TNewType : LinearUnit, new()
    {
        return new Cartesian2D<TNewType>(this.X.ChangeTo<TNewType>(), this.Y.ChangeTo<TNewType>());
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion

}
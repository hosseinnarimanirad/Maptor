// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public struct PolarPoint<TLinear, TAngular> : IPolarPoint
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private LinearUnit m_Radius;

    private AngularUnit m_Angle;

    #endregion

    #region Properties

    public LinearUnit Radius
    {
        get { return m_Radius; }
        set { m_Radius = value; }
    }

    public AngularUnit Angle
    {
        get { return m_Angle; }
        set { m_Angle = value; }
    }

    public LinearMode LinearMode
    {
        get { return Radius.Mode; }
    }

    public AngleMode AngularMode
    {
        get { return Angle.Mode; }
    }

    #endregion

    #region Constructors

    public PolarPoint(LinearUnit radius, AngularUnit angle)
    {
        this.m_Radius = (TLinear)radius.ChangeTo<TLinear>();

        this.m_Angle = (TAngular)angle.ChangeTo<TAngular>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(IPolarPoint))
        {
            return this == (IPolarPoint)obj;
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
                                "Radius: {0}, Angle: {1}",
                                this.Radius,
                                this.Angle);
    }

    public Cartesian2DPoint<T> ToCartesian<T>() where T : LinearUnit, new()
    {
        T x = new T();

        T y = new T();

        double tempRadius = this.Radius.ChangeTo<T>().Value;

        x.Value = tempRadius * this.Angle.Cos;

        y.Value = tempRadius * this.Angle.Sin;

        return new Cartesian2DPoint<T>(x, y);
    }

    #endregion

    #region Operators

    public static bool operator ==(PolarPoint<TLinear, TAngular> firstPoint, IPolarPoint secondPoint)
    {
        return (firstPoint.Radius == secondPoint.Radius &&
                firstPoint.Angle == secondPoint.Angle);
    }

    public static bool operator !=(PolarPoint<TLinear, TAngular> firstPoint, IPolarPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    #endregion
}
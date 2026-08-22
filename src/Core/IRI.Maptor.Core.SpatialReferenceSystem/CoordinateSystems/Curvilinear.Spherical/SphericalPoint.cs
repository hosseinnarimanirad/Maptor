// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public struct SphericalPoint<TLinear, TAngular> : ISphericalPoint
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private LinearUnit m_Radius;

    private AngularUnit m_HorizontalAngle;

    private AngularUnit m_VerticalAngle;

    #endregion

    #region Properties

    public LinearUnit Radius
    {
        get { return m_Radius; }
        set { m_Radius = value; }
    }

    public AngularUnit HorizontalAngle
    {
        get { return m_HorizontalAngle; }
        set { m_HorizontalAngle = value; }
    }

    public AngularUnit VerticalAngle
    {
        get { return m_VerticalAngle; }
        set { m_VerticalAngle = value; }
    }

    public LinearMode LinearMode
    {
        get { return Radius.Mode; }
    }

    public AngleMode AngularMode
    {
        get { return HorizontalAngle.Mode; }
    }

    public AngleRange HorizontalRange
    {
        get { return HorizontalAngle.Range; }
        set { HorizontalAngle.Range = value; }
    }

    #endregion

    #region Constructors

    public SphericalPoint(LinearUnit radius, AngularUnit HorizontalAngle, AngularUnit verticalAngle)
    {
        this.m_Radius = radius.ChangeTo<TLinear>();

        this.m_HorizontalAngle = HorizontalAngle.ChangeTo<TAngular>();

        this.m_VerticalAngle = verticalAngle.ChangeTo<TAngular>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(ISphericalPoint))
        {
            return this == (ISphericalPoint)obj;
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
                                "Radius: {0}, HorizontalAngle: {1}, VerticalAngle: {2}",
                                this.Radius,
                                this.HorizontalAngle,
                                this.VerticalAngle);
    }

    public Cartesian3DPoint<T> ToCartesian<T>() where T : LinearUnit, new()
    {
        T x = new T();

        T y = new T();

        T z = new T();

        double tempRadius = this.Radius.ChangeTo<T>().Value;

        x.Value = tempRadius * this.VerticalAngle.Cos * this.HorizontalAngle.Cos;

        y.Value = tempRadius * this.VerticalAngle.Cos * this.HorizontalAngle.Sin;

        z.Value = tempRadius * this.VerticalAngle.Sin;

        return new Cartesian3DPoint<T>(x, y, z);
    }

    #endregion

    #region Operators

    public static bool operator ==(SphericalPoint<TLinear, TAngular> firstPoint, ISphericalPoint secondPoint)
    {
        return (firstPoint.Radius == secondPoint.Radius &&
                firstPoint.HorizontalAngle == secondPoint.HorizontalAngle &&
                firstPoint.VerticalAngle == secondPoint.VerticalAngle);
    }

    public static bool operator !=(SphericalPoint<TLinear, TAngular> firstPoint, ISphericalPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    #endregion
}

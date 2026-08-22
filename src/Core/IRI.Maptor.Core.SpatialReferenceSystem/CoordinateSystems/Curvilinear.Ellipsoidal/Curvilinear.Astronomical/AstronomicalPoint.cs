// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public struct AstronomicalPoint<TAngular> :  IAstronomicalPoint
    where TAngular : AngularUnit, new()
{
    #region Fields

    private AngularUnit m_HorizontalAngle;

    private AngularUnit m_VerticalAngle;

    #endregion

    #region Properties

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

    public AstronomicalPoint( AngularUnit HorizontalAngle, AngularUnit verticalAngle)
    {
        //Check if verticalAngle is not between -90 and 90
        this.m_HorizontalAngle = HorizontalAngle.ChangeTo<TAngular>();

        this.m_VerticalAngle = verticalAngle.ChangeTo<TAngular>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(IAstronomicalPoint))
        {
            return this == (IAstronomicalPoint)obj;
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
                                "HorizontalAngle: {0}, VerticalAngle: {1}",
                                this.HorizontalAngle,
                                this.VerticalAngle);
    }

    public Cartesian3DPoint<T> ToCartesian<T>() where T : LinearUnit, new()
    {
        T x = new T();

        T y = new T();

        T z = new T();

        x.Value = 1 * this.VerticalAngle.Cos * this.HorizontalAngle.Cos;

        y.Value = 1 * this.VerticalAngle.Cos * this.HorizontalAngle.Sin;

        z.Value = 1 * this.VerticalAngle.Sin;

        return new Cartesian3DPoint<T>(x, y, z);
    }

    #endregion

    #region Operators

    public static bool operator ==(AstronomicalPoint< TAngular> firstPoint, IAstronomicalPoint secondPoint)
    {
        return (firstPoint.HorizontalAngle == secondPoint.HorizontalAngle &&
                firstPoint.VerticalAngle == secondPoint.VerticalAngle);
    }

    public static bool operator !=(AstronomicalPoint<TAngular> firstPoint, IAstronomicalPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    #endregion
}

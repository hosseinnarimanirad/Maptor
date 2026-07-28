// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public struct Cartesian2DPoint<T> : ICartesian2DPoint
    where T : LinearUnit, new()
{
    #region Fields

    private LinearUnit m_X;

    private LinearUnit m_Y;

    private static readonly int auxiliaryValue1 = 1, auxiliaryValue2 = 4;

    #endregion

    #region Properties

    public LinearUnit X
    {
        get { return m_X; }
        set { m_X = value; }
    }

    public LinearUnit Y
    {
        get { return m_Y; }
        set { m_Y = value; }
    }

    public LinearMode LinearMode
    {
        get { return X.Mode; }
    }

    public CoordinateRegion Region
    {
        get { return Cartesian2DPoint<Meter>.GetRegion(this.X.Value, this.Y.Value); }
    }

    #endregion

    #region Constructors

    public Cartesian2DPoint(LinearUnit x, LinearUnit y)
    {
        this.m_X = x.ChangeTo<T>();

        this.m_Y = y.ChangeTo<T>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(ICartesian2DPoint))
        {
            return this == (ICartesian2DPoint)obj;
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
                                "X: {0} , Y: {1}",
                                this.X,
                                this.Y);
    }

    public static CoordinateRegion GetRegion(double x, double y)
    {
        return (CoordinateRegion)((int)Math.Pow(2, Math.Sign(x) + auxiliaryValue1) |
                                    (int)Math.Pow(2, Math.Sign(y) + auxiliaryValue2));
    }

    public PolarPoint<TRadius, TAngle> ToPolar<TRadius, TAngle>(AngleRange range)
        where TRadius : LinearUnit, new()
        where TAngle : AngularUnit, new()
    {
        TRadius radius = new TRadius();

        double tempX = this.X.ChangeTo<TRadius>().Value;

        double tempY = this.Y.ChangeTo<TRadius>().Value;

        radius.Value = Math.Sqrt(tempX * tempX + tempY * tempY);

        Radian angle = new Radian(Math.Atan2(tempY, tempX), range); //use tempX and tempY

        return new PolarPoint<TRadius, TAngle>(radius, angle.ChangeTo<TAngle>()); //do not need to cast!

    }

    #endregion

    #region Operators

    public static bool operator ==(Cartesian2DPoint<T> firstPoint, ICartesian2DPoint secondPoint)
    {
        return (firstPoint.X == secondPoint.X &&
                firstPoint.Y == secondPoint.Y);
    }

    public static bool operator !=(Cartesian2DPoint<T> firstPoint, ICartesian2DPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    #endregion
}

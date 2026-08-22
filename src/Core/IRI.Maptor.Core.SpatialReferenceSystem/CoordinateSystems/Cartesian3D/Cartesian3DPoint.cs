// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public struct Cartesian3DPoint<T> : ICartesian3DPoint
    where T : LinearUnit, new()
{
    #region Fields

    private LinearUnit m_X;

    private LinearUnit m_Y;

    private LinearUnit m_Z;

    private const double allowedDifference = 0.00001;

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

    public LinearUnit Z
    {
        get { return this.m_Z; }
        set { this.m_Z = value; }
    }

    public LinearMode LinearMode
    {
        get { return X.Mode; }
    }

    #endregion

    #region Constructors

    public Cartesian3DPoint(LinearUnit x, LinearUnit y, LinearUnit z)
    {
        this.m_X = x.ChangeTo<T>();

        this.m_Y = y.ChangeTo<T>();

        this.m_Z = z.ChangeTo<T>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(ICartesian3DPoint))
        {
            return this == (ICartesian3DPoint)obj;
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
                                "X: {0}, Y: {1}, Z:{2}",
                                this.X,
                                this.Y,
                                this.Z);
    }

    public ICartesian3DPoint Negate()
    {
        return new Cartesian3DPoint<T>(this.X.Negate(), this.Y.Negate(), this.Z.Negate());
    }

    public SphericalPoint<TLinear, TAngular> ToSpherical<TLinear, TAngular>(AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        T radius = new T();

        double tempX = this.X.Value;

        double tempY = this.Y.Value;

        double tempZ = this.Z.Value;

        radius.Value = Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ);

        Radian horizontalAngle = new Radian(Math.Atan2(this.Y.Value, this.X.Value), horizontalRange);

        Radian verticalAngle = new Radian(Math.Atan2(tempZ, Math.Sqrt(tempX * tempX + tempY * tempY)), AngleRange.MinusPIToPI);

        return new SphericalPoint<TLinear, TAngular>(radius, horizontalAngle, verticalAngle);
    }

    public AstronomicalPoint<TAngular> ToAstronomic<TAngular>(AngleRange horizontalRange)
        where TAngular : AngularUnit, new()
    {
        double tempX = this.X.Value;

        double tempY = this.Y.Value;

        double tempZ = this.Z.Value;

        //if ((tempX * tempX + tempY * tempY + tempZ * tempZ) != 1)
        //{
        //    throw new NotImplementedException();
        //}

        Radian horizontalAngle = new Radian(Math.Atan2(tempY, tempX), horizontalRange);

        Radian verticalAngle = new Radian(Math.Atan2(Z.Value, Math.Sqrt(tempX * tempX + tempY * tempY)), AngleRange.MinusPIToPI);

        return new AstronomicalPoint<TAngular>(horizontalAngle, verticalAngle);
    }

    public GeodeticPoint<TLinear, TAngular> ToGeodetic<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange longitudinalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        double tempSemiMajor = ellipsoid.SemiMajorAxis.ChangeTo<TLinear>().Value;

        double tempSemiMinor = ellipsoid.SemiMinorAxis.ChangeTo<TLinear>().Value;

        double e2TempValue = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        double tempX = this.X.ChangeTo<TLinear>().Value;

        double tempY = this.Y.ChangeTo<TLinear>().Value;

        double tempZ = this.Z.ChangeTo<TLinear>().Value;

        PolarPoint<TLinear, TAngular> tempValue = (new Cartesian2DPoint<TLinear>(new TLinear() { Value = tempX }, new TLinear() { Value = tempY })).ToPolar<TLinear, TAngular>(longitudinalRange);

        double pTempValue = tempValue.Radius.Value;

        double nTempValue = ellipsoid.SemiMajorAxis.Value;

        double hTempValue1 = Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ)
                            -
                            Math.Sqrt(tempSemiMajor * tempSemiMinor);

        double latitudeTempValue1 = Math.Atan(tempZ / pTempValue *
                                                1 / (1 - (e2TempValue * nTempValue) / (nTempValue + hTempValue1)));

        if (latitudeTempValue1.Equals(double.NaN))
        {
            return new GeodeticPoint<TLinear, TAngular>(ellipsoid, new TLinear() { Value = 0 }, tempValue.Angle, (new Radian(0, longitudinalRange)));
        }

        double hTempValue2 = 0, latitudeTempValue2 = 0;

        bool conditionValue = true;

        do
        {
            nTempValue = ellipsoid.CalculateN(new Radian(latitudeTempValue1, AngleRange.MinusPIToPI)).ChangeTo<TLinear>().Value;

            hTempValue2 = pTempValue / Math.Cos(latitudeTempValue1) - nTempValue;

            latitudeTempValue2 = Math.Atan(tempZ / pTempValue *
                                                1 / (1 - (e2TempValue * nTempValue) / (nTempValue + hTempValue2)));

            if (Math.Abs(hTempValue2 - hTempValue1) + Math.Abs(latitudeTempValue2 - latitudeTempValue1) < allowedDifference)
            {
                conditionValue = false;
            }
            else
            {
                hTempValue1 = hTempValue2;

                latitudeTempValue1 = latitudeTempValue2;
            }

        } while (conditionValue);

        TLinear height = new TLinear() { Value = hTempValue2 };

        return new GeodeticPoint<TLinear, TAngular>(ellipsoid, height, tempValue.Angle, (new Radian(latitudeTempValue2, longitudinalRange)));
    }

    public EllipsoidalPoint<TLinear, TAngular> ToEllipsoidal<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        double tempSemiMajor = ellipsoid.SemiMajorAxis.ChangeTo<TLinear>().Value;

        double tempSemiMinor = ellipsoid.SemiMinorAxis.ChangeTo<TLinear>().Value;

        double e2TempValue = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        double tempX = this.X.ChangeTo<TLinear>().Value;

        double tempY = this.Y.ChangeTo<TLinear>().Value;

        double tempZ = this.Z.ChangeTo<TLinear>().Value;

        PolarPoint<TLinear, TAngular> tempValue = (new Cartesian2DPoint<TLinear>(new TLinear() { Value = tempX }, new TLinear() { Value = tempY })).ToPolar<TLinear, TAngular>(horizontalRange);

        double pTempValue = tempValue.Radius.Value;

        double nTempValue = ellipsoid.SemiMajorAxis.Value;

        double latitudeTempValue1 = Math.Atan(tempZ / pTempValue *
                                                1 / (1 - (e2TempValue * nTempValue) / (nTempValue + 0)));

        if (latitudeTempValue1.Equals(double.NaN))
        {
            return new EllipsoidalPoint<TLinear, TAngular>(ellipsoid, tempValue.Angle, (new Radian(0, horizontalRange)));
        }

        double latitudeTempValue2 = 0;

        bool conditionValue = true;

        do
        {
            nTempValue = ellipsoid.CalculateN(new Radian(latitudeTempValue1, AngleRange.MinusPIToPI)).ChangeTo<TLinear>().Value;

            latitudeTempValue2 = Math.Atan(tempZ / pTempValue *
                                                1 / (1 - (e2TempValue * nTempValue) / (nTempValue + 0)));

            if (Math.Abs(0 - 0) + Math.Abs(latitudeTempValue2 - latitudeTempValue1) < allowedDifference)
            {
                conditionValue = false;
            }
            else
            {
                latitudeTempValue1 = latitudeTempValue2;
            }

        } while (conditionValue);

        return new EllipsoidalPoint<TLinear, TAngular>(ellipsoid, tempValue.Angle, (new Radian(latitudeTempValue2, horizontalRange)));
    }

    #endregion

    #region Operators

    public static bool operator ==(Cartesian3DPoint<T> firstPoint, ICartesian3DPoint secondPoint)
    {
        return (firstPoint.X == secondPoint.X &&
                firstPoint.Y == secondPoint.Y &&
                firstPoint.Z == secondPoint.Z);
    }

    public static bool operator !=(Cartesian3DPoint<T> firstPoint, ICartesian3DPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    public static Cartesian3DPoint<T> operator -(Cartesian3DPoint<T> point)
    {
        return (Cartesian3DPoint<T>)point.Negate();
    }

    #endregion

}

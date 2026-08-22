// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Metrics;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public struct EllipsoidalPoint<TLinear, TAngular> : IEllipsoidalPoint
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private IEllipsoid m_Datum;

    private AngularUnit m_VerticalAngle;

    private AngularUnit m_HorizontalAngle;

    #endregion

    #region Properties

    public IEllipsoid Datum
    {
        get { return this.m_Datum; }
    }

    public AngularUnit VerticalAngle
    {
        get { return m_VerticalAngle; }
        set { m_VerticalAngle = value; }
    }

    public AngularUnit HorizontalAngle
    {
        get { return m_HorizontalAngle; }
        set { m_HorizontalAngle = value; }
    }

    public AngleMode AngularMode
    {
        get { return VerticalAngle.Mode; }
    }

    public AngleRange HorizontalRange
    {
        get { return HorizontalAngle.Range; }
        set { HorizontalAngle.Range = value; }
    }

    #endregion

    #region Constructors

    public EllipsoidalPoint(IEllipsoid ellipsoid, AngularUnit horizontalAngle, AngularUnit verticalAngle)
    {
        if (verticalAngle.Range != AngleRange.MinusPIToPI)
        {
            verticalAngle.Range = AngleRange.MinusPIToPI;
        }

        this.m_Datum = ellipsoid.ChangeTo<TLinear, TAngular>();

        this.m_VerticalAngle = verticalAngle.ChangeTo<TAngular>();

        this.m_HorizontalAngle = horizontalAngle.ChangeTo<TAngular>();
    }

    #endregion

    #region Methods

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(IEllipsoidalPoint))
        {
            return this == (IEllipsoidalPoint)obj;
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
                                "Longitude: {0}, Latitude: {1}, Associated Ellipsoid: {2}",
                                this.HorizontalAngle,
                                this.VerticalAngle,
                                this.Datum);
    }

    public Cartesian3DPoint<T> ToCartesian<T>() where T : LinearUnit, new()
    {
        T x = new T();

        T y = new T();

        T z = new T();

        double tempSemiMinor = Datum.SemiMinorAxis.ChangeTo<T>().Value;

        double tempSemiMajor = Datum.SemiMajorAxis.ChangeTo<T>().Value;

        //double tempOriginX = this.AssociatedEllipsoid.Origin.X.ChangeTo<T>().Value;

        //double tempOriginY = this.AssociatedEllipsoid.Origin.Y.ChangeTo<T>().Value;

        //double tempOriginZ = this.AssociatedEllipsoid.Origin.Z.ChangeTo<T>().Value;

        //Matrix rotationMatrix = Transformation.CalculateEulerElementMatrix(AssociatedEllipsoid.Omega, AssociatedEllipsoid.Phi, AssociatedEllipsoid.Kappa);

        //Matrix transferMatrix = new Matrix(new double[][] { new double[] { tempOriginX, tempOriginY, tempOriginZ } });

        double tempN = this.Datum.CalculateN(this.VerticalAngle).ChangeTo<T>().Value;

        double longCos = this.HorizontalAngle.Cos; double longSin = this.HorizontalAngle.Sin;

        double latCos = this.VerticalAngle.Cos; double latSin = this.VerticalAngle.Sin;

        Matrix tempGeodetic = new Matrix(new double[][] { 
                                            new double[] { 
                                                tempN * latCos * longCos, 
                                                tempN * latCos * longSin, 
                                                tempN * tempSemiMinor * tempSemiMinor / (tempSemiMajor * tempSemiMajor) * latSin } });

        //Matrix tempResult = rotationMatrix * tempGeodetic + transferMatrix;

        x.Value = tempGeodetic[0, 0];

        y.Value = tempGeodetic[1, 0];

        z.Value = tempGeodetic[2, 0];

        return new Cartesian3DPoint<T>(x, y, z);
    }

    #endregion

    #region Operators

    public static bool operator ==(EllipsoidalPoint<TLinear, TAngular> firstPoint, IEllipsoidalPoint secondPoint)
    {
        return (firstPoint.VerticalAngle == secondPoint.VerticalAngle &&
                firstPoint.HorizontalAngle == secondPoint.HorizontalAngle &&
                firstPoint.Datum == secondPoint.Datum);
    }

    public static bool operator !=(EllipsoidalPoint<TLinear, TAngular> firstPoint, IEllipsoidalPoint secondPoint)
    {
        return !(firstPoint == secondPoint);
    }

    #endregion
}


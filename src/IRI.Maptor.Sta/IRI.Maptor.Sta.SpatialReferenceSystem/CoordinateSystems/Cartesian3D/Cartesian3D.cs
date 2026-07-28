// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.SpatialReferenceSystem.Models;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public class Cartesian3D<T> : ICartesian3D
    where T : LinearUnit, new()
{
    #region Fields

    private string m_Name;

    private AxisType m_Handedness;

    private const int m_Dimension = 3;

    private ILinearCollection m_X, m_Y, m_Z;

    private const double allowedDifference = 0.00001;

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

    public ILinearCollection Z
    {
        get { return this.m_Z; }
    }


    #endregion

    #region Constructors

    public ICartesian3DPoint this[int index]
    {
        get { return new Cartesian3DPoint<T>(this.X[index], this.Y[index], this.Z[index]); }

        set
        {
            this.X[index] = value.X;

            this.Y[index] = value.Y;

            this.Z[index] = value.Z;
        }
    }

    public Cartesian3D(ILinearCollection x, ILinearCollection y, ILinearCollection z)
        : this("Cartesian2D", x, y, z, AxisType.RightHanded) { }

    public Cartesian3D(ILinearCollection x, ILinearCollection y, ILinearCollection z, AxisType handedness)
        : this("Cartesian2D", x, y, z, handedness) { }

    public Cartesian3D(string name, ILinearCollection x, ILinearCollection y, ILinearCollection z, AxisType handedness)
    {

        if (x.Length != y.Length ||
            x.Length != z.Length)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_X = (LinearCollection<T>)x.ChangeTo<T>();

        this.m_Y = (LinearCollection<T>)y.ChangeTo<T>();

        this.m_Z = (LinearCollection<T>)z.ChangeTo<T>();
    }

    public Cartesian3D(string name, Matrix values, AxisType handedness, LinearPrefix prefix)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this.m_Name = name;

        this.m_Handedness = handedness;

        this.m_X = new LinearCollection<T>(values.GetColumn(0));

        this.m_Y = new LinearCollection<T>(values.GetColumn(1));

        this.m_Z = new LinearCollection<T>(values.GetColumn(2));
    }

    #endregion

    #region Methods

    public IEnumerator<ICartesian3DPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (Cartesian3DPoint<T>)this[i];
        }
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.X.ToArray(), this.Y.ToArray(), this.Z.ToArray() });
    }

    public ICartesian3D Clone()
    {
        return new Cartesian3D<T>(string.Format("{1}{2}", this.Name, ".Clone"),
                                            this.X.Clone(),
                                            this.Y.Clone(),
                                            this.Z.Clone(),
                                            this.Handedness);
    }

    public ICartesian3D Transform(Matrix rotation, AxisType newHandedness)
    {
        return this.Transform(rotation,
                                new Cartesian3DPoint<T>(new T() { Value = 0 },
                                                        new T() { Value = 0 },
                                                        new T() { Value = 0 }),
                                newHandedness);
    }

    public ICartesian3D Transform(AngularUnit rotationAboutX, AngularUnit rotationAboutY, AngularUnit rotationAboutZ, ICartesian3DPoint translation)
    {
        Matrix rotationMatrix = Transformations.CalculateEulerElementMatrix(
                                                       new OrientationParameter(rotationAboutX,
                                                                                   rotationAboutY,
                                                                                   rotationAboutZ));

        return this.Transform(rotationMatrix, translation, this.Handedness);
    }

    public ICartesian3D Transform(Matrix rotation, ICartesian3DPoint translation)
    {
        return this.Transform(rotation, translation, this.Handedness);
    }

    public ICartesian3D Transform(Matrix rotation, ICartesian3DPoint translation, AxisType newHandedness)
    {
        if (rotation.NumberOfColumns != 3)
            throw new NotImplementedException();

        if (rotation.NumberOfRows != 3)
            throw new NotImplementedException();

        double tempX = (translation.X.ChangeTo<T>()).Value;

        double tempY = (translation.Y.ChangeTo<T>()).Value;

        double tempZ = (translation.Z.ChangeTo<T>()).Value;

        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> z = new LinearCollection<T>(this.NumberOfPoints);


        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            Matrix tempMatrix = new Matrix(new double[][] { 
                                            new double[] { 
                                                this.X.GetTheValue(i), 
                                                this.Y.GetTheValue(i), 
                                                this.Z.GetTheValue(i) } });

            Matrix result = rotation * tempMatrix;

            x.SetTheValue(i, result[0, 0] - tempX);

            y.SetTheValue(i, result[1, 0] - tempY);

            z.SetTheValue(i, result[2, 0] - tempZ);
        }

        return new Cartesian3D<T>(x, y, z, newHandedness);
    }

    public ICartesian3D Rotate(Matrix rotationMatrix)
    {
        return this.Transform(rotationMatrix, 
                                new Cartesian3DPoint<T>(new T() { Value = 0 },
                                                        new T() { Value = 0 },
                                                        new T() { Value = 0 }),
                                this.Handedness);
    }

    public ICartesian3D Rotate(AngularUnit rotationAboutX, AngularUnit rotationAboutY, AngularUnit rotationAboutZ)
    {
        Matrix rotationMatrix = Transformations.CalculateEulerElementMatrix(
                                                    new OrientationParameter(rotationAboutX,
                                                                                rotationAboutY,
                                                                                rotationAboutZ));
        return this.Rotate(rotationMatrix);

    }

    public ICartesian3D RotateAboutX(AngularUnit value, RotateDirection direction)
    {
        //ILinearArray x = this.X.Clone();

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> z = new LinearCollection<T>(this.NumberOfPoints);

        double cos = value.Cos;

        double sin = (int)direction * (int)this.Handedness * value.Sin;

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            y.SetTheValue(i, this.Y.GetTheValue(i) * cos + this.Z.GetTheValue(i) * sin);

            z.SetTheValue(i, -this.Y.GetTheValue(i) * sin + this.Z.GetTheValue(i) * cos);
        }

        return new Cartesian3D<T>(this.X.Clone(), y, z, this.Handedness);
    }

    public ICartesian3D RotateAboutY(AngularUnit value, RotateDirection direction)
    {
        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        //ILinearArray y = this.Y.Clone();

        LinearCollection<T> z = new LinearCollection<T>(this.NumberOfPoints);

        double cos = value.Cos;

        double sin = (int)direction * (int)this.Handedness * value.Sin;

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            x.SetTheValue(i, this.X.GetTheValue(i) * cos - this.Z.GetTheValue(i) * sin);

            z.SetTheValue(i, this.X.GetTheValue(i) * sin + this.Z.GetTheValue(i) * cos);
        }

        return new Cartesian3D<T>(x, this.Y.Clone(), z, this.Handedness);
    }

    public ICartesian3D RotateAboutZ(AngularUnit value, RotateDirection direction)
    {
        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        //ILinearArray z = this.Z.Clone();

        double cos = value.Cos;

        double sin = (int)direction * (int)this.Handedness * value.Sin;

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            x.SetTheValue(i, this.X.GetTheValue(i) * cos + this.Y.GetTheValue(i) * sin);

            y.SetTheValue(i, -this.X.GetTheValue(i) * sin + this.Y.GetTheValue(i) * cos);
        }

        return new Cartesian3D<T>(x, y, this.Z.Clone(), this.Handedness);
    }

    public ICartesian3D Shift(ICartesian3DPoint newBase)
    {
        double tempX = (newBase.X.ChangeTo<T>()).Value;

        double tempY = (newBase.Y.ChangeTo<T>()).Value;

        double tempZ = (newBase.Z.ChangeTo<T>()).Value;

        ILinearCollection newX = X.SubtractAllValuesWith(tempX);

        ILinearCollection newY = Y.SubtractAllValuesWith(tempY);

        ILinearCollection newZ = Z.SubtractAllValuesWith(tempZ);

        return new Cartesian3D<T>(newX, newY, newZ, this.Handedness);
    }

    public Spherical<TLinear, TAngular> ToSphericalForm<TLinear, TAngular>(AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        ILinearCollection radius = new LinearCollection<TLinear>(this.NumberOfPoints);

        IAngularCollection horizontalAngle = new AngularCollection<TAngular>(this.NumberOfPoints, horizontalRange);

        IAngularCollection verticalAngle = new AngularCollection<TAngular>(this.NumberOfPoints, AngleRange.MinusPIToPI);

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            //double tempX = this.X[i].ChangeTo<TLinear>().Value;

            //double tempY = this.Y[i].ChangeTo<TLinear>().Value;

            //double tempZ = this.Z[i].ChangeTo<TLinear>().Value;

            double tempX = this.X.GetTheValue(i);

            double tempY = this.Y.GetTheValue(i);

            double tempZ = this.Z.GetTheValue(i);

            //radius.SetTheValue(i, Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ));
            radius[i] = new T() { Value = Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ) };

            horizontalAngle[i] = new Radian(Math.Atan2(tempY, tempX), horizontalRange);

            verticalAngle[i] = new Radian(Math.Atan2(tempZ, Math.Sqrt(tempX * tempX + tempY * tempY)), AngleRange.MinusPIToPI);
        }

        return new Spherical<TLinear, TAngular>(radius, horizontalAngle, verticalAngle, this.Handedness);
    }

    public Astronomical<TAngular> ToAstronomicForm<TAngular>(AngleRange horizontalRange)
        where TAngular : AngularUnit, new()
    {
        IAngularCollection horizontalAngle = new AngularCollection<TAngular>(this.NumberOfPoints, horizontalRange);

        IAngularCollection verticalAngle = new AngularCollection<TAngular>(this.NumberOfPoints, AngleRange.MinusPIToPI);

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            double tempX = this.X.GetTheValue(i);

            double tempY = this.Y.GetTheValue(i);

            double tempZ = this.Z.GetTheValue(i);

            horizontalAngle[i] = new Radian(Math.Atan2(tempY, tempX), horizontalRange);

            verticalAngle[i] = new Radian(Math.Atan2(tempZ, Math.Sqrt(tempX * tempX + tempY * tempY)), AngleRange.MinusPIToPI);
        }

        return new Astronomical<TAngular>(horizontalAngle, verticalAngle, this.Handedness);
    }

    public Geodetic<TLinear, TAngular> ToGeodeticForm<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange longitudinalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        ILinearCollection height = new LinearCollection<TLinear>(this.NumberOfPoints);

        IAngularCollection longitude = new AngularCollection<TAngular>(this.NumberOfPoints, longitudinalRange);

        IAngularCollection latitude = new AngularCollection<TAngular>(this.NumberOfPoints, AngleRange.MinusPIToPI);

        double tempSemiMajor = ellipsoid.SemiMajorAxis.ChangeTo<TLinear>().Value;

        double tempSemiMinor = ellipsoid.SemiMinorAxis.ChangeTo<TLinear>().Value;

        double e2TempValue = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        //double tempOriginX = ellipsoid.Origin.X.ChangeTo<TLinear>().Value;

        //double tempOriginY = ellipsoid.Origin.Y.ChangeTo<TLinear>().Value;

        //double tempOriginZ = ellipsoid.Origin.Z.ChangeTo<TLinear>().Value;

        //Matrix rotationMatrix = (Transformation.CalculateEulerElementMatrix(ellipsoid.Omega, ellipsoid.Phi, ellipsoid.Kappa)).Transpose();

        //Matrix transferMatrix = new Matrix(new double[][] { new double[] { tempOriginX, tempOriginY, tempOriginZ } });

        for (int i = 0; i < this.NumberOfPoints; i++)
        {

            double tempX = this.X[i].ChangeTo<TLinear>().Value;

            double tempY = this.Y[i].ChangeTo<TLinear>().Value;

            double tempZ = this.Z[i].ChangeTo<TLinear>().Value;

            //Matrix tempCartesian = new Matrix(new double[][] { new double[] { tempX, tempY, tempZ } });

            //Matrix tempGeodetic = rotationMatrix * (tempCartesian - transferMatrix);

            //tempX = tempGeodetic[0, 0];

            //tempY = tempGeodetic[1, 0];

            //tempZ = tempGeodetic[2, 0];

            PolarPoint<TLinear, TAngular> tempValue = (new Cartesian2DPoint<TLinear>(new TLinear() { Value = tempX }, new TLinear() { Value = tempY })).ToPolar<TLinear, TAngular>(longitudinalRange);

            longitude[i] = tempValue.Angle;

            double pTempValue = tempValue.Radius.Value;

            double nTempValue = tempSemiMajor;

            double hTempValue1 = Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ)
                                -
                                Math.Sqrt(tempSemiMajor * tempSemiMinor);

            double latitudeTempValue1 = Math.Atan(tempZ / pTempValue *
                                                    1 / (1 - (e2TempValue * nTempValue) / (nTempValue + hTempValue1)));

            if (latitudeTempValue1.Equals(double.NaN))
            {
                continue;
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

            height.SetTheValue(i, hTempValue2);

            latitude[i] = new Radian(latitudeTempValue2, AngleRange.MinusPIToPI);
        }

        return new Geodetic<TLinear, TAngular>(height, longitude, latitude, ellipsoid, this.Handedness);
    }

    public Ellipsoidal<TLinear, TAngular> ToEllipsoidalForm<TLinear, TAngular>(IEllipsoid ellipsoid, AngleRange horizontalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        IAngularCollection longitude = new AngularCollection<TAngular>(this.NumberOfPoints, horizontalRange);

        IAngularCollection latitude = new AngularCollection<TAngular>(this.NumberOfPoints, AngleRange.MinusPIToPI);

        double tempSemiMajor = ellipsoid.SemiMajorAxis.ChangeTo<TLinear>().Value;

        double tempSemiMinor = ellipsoid.SemiMinorAxis.ChangeTo<TLinear>().Value;

        double e2TempValue = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        //double tempOriginX = ellipsoid.Origin.X.ChangeTo<TLinear>().Value;

        //double tempOriginY = ellipsoid.Origin.Y.ChangeTo<TLinear>().Value;

        //double tempOriginZ = ellipsoid.Origin.Z.ChangeTo<TLinear>().Value;

        //Matrix rotationMatrix = (Transformation.CalculateEulerElementMatrix(ellipsoid.Omega, ellipsoid.Phi, ellipsoid.Kappa)).Transpose();

        //Matrix transferMatrix = new Matrix(new double[][] { new double[] { tempOriginX, tempOriginY, tempOriginZ } });

        for (int i = 0; i < this.NumberOfPoints; i++)
        {

            double tempX = this.X[i].ChangeTo<TLinear>().Value;

            double tempY = this.Y[i].ChangeTo<TLinear>().Value;

            double tempZ = this.Z[i].ChangeTo<TLinear>().Value;

            //Matrix tempCartesian = new Matrix(new double[][] { new double[] { tempX, tempY, tempZ } });

            //Matrix tempGeodetic = rotationMatrix * (tempCartesian - transferMatrix);

            //tempX = tempGeodetic[0, 0];

            //tempY = tempGeodetic[1, 0];

            //tempZ = tempGeodetic[2, 0];

            PolarPoint<TLinear, TAngular> tempValue = (new Cartesian2DPoint<TLinear>(new TLinear() { Value = tempX }, new TLinear() { Value = tempY })).ToPolar<TLinear, TAngular>(horizontalRange);

            longitude[i] = tempValue.Angle;

            double pTempValue = tempValue.Radius.Value;

            double nTempValue = tempSemiMajor;

            //double hTempValue1 = Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ)
            //                    -
            //                    Math.Sqrt(tempSemiMajor * tempSemiMinor);

            double latitudeTempValue1 = Math.Atan(tempZ / pTempValue *
                                                    1 / (1 - (e2TempValue * nTempValue) / (nTempValue + 0)));

            if (latitudeTempValue1.Equals(double.NaN))
            {
                continue;
            }
            //double hTempValue2 = 0;

            double latitudeTempValue2 = 0;

            bool conditionValue = true;

            do
            {
                nTempValue = ellipsoid.CalculateN(new Radian(latitudeTempValue1, AngleRange.MinusPIToPI)).ChangeTo<TLinear>().Value;

                //hTempValue2 = pTempValue / Math.Cos(latitudeTempValue1) - nTempValue;

                latitudeTempValue2 = Math.Atan(tempZ / pTempValue *
                                                    1 / (1 - (e2TempValue * nTempValue) / (nTempValue + 0)));

                if (Math.Abs(0 - 0) + Math.Abs(latitudeTempValue2 - latitudeTempValue1) < allowedDifference)
                {
                    conditionValue = false;
                }
                else
                {
                    //hTempValue1 = hTempValue2;

                    latitudeTempValue1 = latitudeTempValue2;
                }

            } while (conditionValue);

            //height.SetTheValue(i, hTempValue2);

            latitude[i] = new Radian(latitudeTempValue2, AngleRange.MinusPIToPI);//).ChangeTo<TAngular>();
        }

        return new Ellipsoidal<TLinear, TAngular>(longitude, latitude, ellipsoid, this.Handedness);
    }

    public Cartesian3D<TNewType> ChangeTo<TNewType>() where TNewType : LinearUnit, new()
    {
        return new Cartesian3D<TNewType>(this.X.ChangeTo<TNewType>(),
                                            this.Y.ChangeTo<TNewType>(),
                                            this.Z.ChangeTo<TNewType>());
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion

}

// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.Models;
using IRI.Maptor.Sta.Common.Abstractions;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public static class Transformations
{
    #region General

    public static Matrix CalculateEulerElementMatrix(AngularUnit rotationAboutX, AngularUnit rotationAboutY, AngularUnit rotationAboutZ)
    {
        double tempOmegaCos = rotationAboutX.Cos;

        double tempOmegaSin = rotationAboutX.Sin;

        double tempPhiCos = rotationAboutY.Cos;

        double tempPhiSin = rotationAboutY.Sin;

        double tempKappaCos = rotationAboutZ.Cos;

        double tempKappaSin = rotationAboutZ.Sin;

        return new Matrix(new double[][]{

                            new double[]{tempPhiCos*tempKappaCos,
                                            -tempPhiCos*tempKappaSin,
                                            tempPhiSin},

                            new double[]{tempOmegaCos*tempKappaSin+tempOmegaSin*tempPhiSin*tempKappaCos,
                                            tempOmegaCos*tempKappaCos-tempOmegaSin*tempPhiSin*tempKappaSin,
                                            -tempOmegaSin*tempPhiCos},

                            new double[]{tempOmegaSin*tempKappaSin-tempOmegaCos*tempPhiSin*tempKappaCos,
                                            tempOmegaSin*tempKappaCos+tempOmegaCos*tempPhiSin*tempKappaSin,
                                            tempOmegaCos*tempPhiCos}});

    }

    public static Matrix CalculateEulerElementMatrix(OrientationParameter angles)
    {
        double tempOmegaCos = angles.Omega.Cos;

        double tempOmegaSin = angles.Omega.Sin;

        double tempPhiCos = angles.Phi.Cos;

        double tempPhiSin = angles.Phi.Sin;

        double tempKappaCos = angles.Kappa.Cos;

        double tempKappaSin = angles.Kappa.Sin;

        return new Matrix(new double[][]{

                            new double[]{tempPhiCos*tempKappaCos,
                                            -tempPhiCos*tempKappaSin,
                                            tempPhiSin},

                            new double[]{tempOmegaCos*tempKappaSin+tempOmegaSin*tempPhiSin*tempKappaCos,
                                            tempOmegaCos*tempKappaCos-tempOmegaSin*tempPhiSin*tempKappaSin,
                                            -tempOmegaSin*tempPhiCos},

                            new double[]{tempOmegaSin*tempKappaSin-tempOmegaCos*tempPhiSin*tempKappaCos,
                                            tempOmegaSin*tempKappaCos+tempOmegaCos*tempPhiSin*tempKappaSin,
                                            tempOmegaCos*tempPhiCos}});

    }

    public static Matrix CalculateRotationMatrixAroundX(AngularUnit angle)
    {
        double cos = angle.Cos;

        double sin = angle.Sin;

        return new Matrix(new double[][] {
                            new double[] { 1, 0, 0 },
                            new double[] { 0, cos, -sin },
                            new double[] { 0, sin, cos } });
    }

    public static Matrix CalculateRotationMatrixAroundY(AngularUnit angle)
    {
        double cos = angle.Cos;

        double sin = angle.Sin;

        return new Matrix(new double[][] {
                            new double[] { cos, 0, sin },
                            new double[] { 0, 1, 0 },
                            new double[] { -sin, 0, cos } });
    }

    public static Matrix CalculateRotationMatrixAroundZ(AngularUnit angle)
    {
        double cos = angle.Cos;

        double sin = angle.Sin;

        return new Matrix(new double[][] {
                            new double[] { cos, -sin, 0 },
                            new double[] { sin, cos, 0 },
                            new double[] { 0, 0, 1 } });
    }

    public static Matrix CalculateReflectionMatrix()
    {
        return new Matrix(new double[][] {
                            new double[] { 1, 0, 0 },
                            new double[] { 0, -1, 0 },
                            new double[] { 0, 0, 1 } });
    }

    public static OrientationParameter CalculateLocalAstronomicLocalGeodeticParameter(IGeodeticPoint initialPoint, AngularUnit astronomicalLatitude, AngularUnit astronomicalLongitude)
    {
        double tempLatSin = initialPoint.Latitude.Sin;

        double tempLatCos = initialPoint.Latitude.Cos;

        double tempLongSin = initialPoint.Longitude.Sin;

        double tempLongCos = initialPoint.Longitude.Cos;

        double tempEx = initialPoint.Datum.DatumMisalignment.Omega.ChangeTo<Radian>().Value;

        double tempEy = initialPoint.Datum.DatumMisalignment.Phi.ChangeTo<Radian>().Value;

        double tempEz = initialPoint.Datum.DatumMisalignment.Kappa.ChangeTo<Radian>().Value;

        double tempDeltaLambda = astronomicalLongitude.Subtract(initialPoint.Longitude).ChangeTo<Radian>().Value;

        double tempDeltaPhi = astronomicalLatitude.Subtract(initialPoint.Latitude).ChangeTo<Radian>().Value;

        double tempDeltaAzimuth = tempDeltaLambda * tempLatSin - tempLatCos * (tempEx * tempLongCos + tempEy * tempLongSin) - tempEz * tempLatSin;

        double tempKesi = tempDeltaPhi - tempEx * tempLongSin - tempEy * tempLongCos;

        double tempEta = tempDeltaLambda * tempLongCos + tempLatSin * (tempEx * tempLongCos + tempEy * tempLongSin) - tempEz * tempLatCos;

        return new OrientationParameter(new Radian(tempEta),
                                        new Radian(tempKesi),
                                        new Radian(tempDeltaAzimuth));
    }

    #endregion

    #region CT, IT

    public static Cartesian3D<T> AverageToInstantaneous<T>(
                                                   ICartesian3D averageTerrestrial,
                                                   AngularUnit poleX,
                                                   AngularUnit poleY)
        where T : LinearUnit, new()
    {
        //check if average was not Right Handed!

        Matrix Rx = CalculateRotationMatrixAroundX(poleY);

        Matrix Ry = CalculateRotationMatrixAroundY(poleX);

        return averageTerrestrial.Rotate(Rx * Ry).ChangeTo<T>();
    }

    public static Cartesian3D<T> InstantaneousToAverage<T>(
                                                    ICartesian3D instantaneousTerrestrial,
                                                    AngularUnit poleX,
                                                    AngularUnit poleY)
        where T : LinearUnit, new()
    {
        //check if instantaneous was not Right Handed!

        Matrix Rx = CalculateRotationMatrixAroundX(poleY.Negate());

        Matrix Ry = CalculateRotationMatrixAroundY(poleX.Negate());

        return instantaneousTerrestrial.Rotate(Ry * Rx).ChangeTo<T>();
    }

    #endregion

    #region CT, G

    public static Geodetic<TLinear, TAngular> AverageToGeodetic<TLinear, TAngular>(
                                                                ICartesian3D averageTerrestrial,
                                                                IEllipsoid ellipsoid,
                                                                AngleRange longitudinalRange)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        //check if cartesian is not Right Handed, do the appropriate task!

        ICartesian3D shiftedCoordinate = averageTerrestrial.Shift(ellipsoid.DatumTranslation);

        Matrix rotationMatrix = Transformations.CalculateEulerElementMatrix(ellipsoid.DatumMisalignment).Transpose();

        return shiftedCoordinate.Rotate(rotationMatrix).ToGeodeticForm<TLinear, TAngular>(ellipsoid, longitudinalRange);

    }


    public static Cartesian3D<TLinear> GeodeticToAverage<TLinear>(IGeodetic geodetic)
        where TLinear : LinearUnit, new()
    {
        //check if not geodetic is Right Handed

        Cartesian3D<TLinear> cartesianFormOfGeodetic = geodetic.GetCartesianForm<TLinear>();

        Matrix rotationMatrix = Transformations.CalculateEulerElementMatrix(geodetic.Datum.DatumMisalignment);

        return (Cartesian3D<TLinear>)cartesianFormOfGeodetic.Transform(rotationMatrix, geodetic.Datum.DatumTranslation.Negate());

    }

    public static Cartesian3DPoint<T> GeodeticToAverage<T>(IGeodeticPoint geodetic)
        where T : LinearUnit, new()
    {
        //check if not geodetic is Right Handed
        Cartesian3DPoint<T> semiGeodetic = geodetic.ToCartesian<T>();

        double tempOriginX = geodetic.Datum.DatumTranslation.X.ChangeTo<T>().Value;

        double tempOriginY = geodetic.Datum.DatumTranslation.Y.ChangeTo<T>().Value;

        double tempOriginZ = geodetic.Datum.DatumTranslation.Z.ChangeTo<T>().Value;

        Matrix rotationMatrix = Transformations.CalculateEulerElementMatrix(geodetic.Datum.DatumMisalignment);

        Matrix transferMatrix = new Matrix(new double[][] { new double[] { tempOriginX, tempOriginY, tempOriginZ } });

        Matrix tempSemiGeodetic = new Matrix(new double[][]{new double[]{semiGeodetic.X.Value,
                                                                            semiGeodetic.Y.Value,
                                                                            semiGeodetic.Z.Value}});

        Matrix tempResult = rotationMatrix * tempSemiGeodetic + transferMatrix;

        return new Cartesian3DPoint<T>(new T() { Value = tempResult[0, 0] },
                                        new T() { Value = tempResult[1, 0] },
                                        new T() { Value = tempResult[2, 0] });

    }

    #endregion

    #region G1, G2

    private const double allowedDifference = 0.00001;

    public static Geodetic<TLinear, TAngular> ChangeDatum<TLinear, TAngular>(
                                                            IGeodetic oldGeodeticCoordinate,
                                                            IEllipsoid newDatum)
        where TLinear : LinearUnit, new()
        where TAngular : AngularUnit, new()
    {
        ICartesian3D tempCoordinate = GeodeticToAverage<TLinear>(oldGeodeticCoordinate);

        return AverageToGeodetic<TLinear, TAngular>(tempCoordinate, newDatum, oldGeodeticCoordinate.LongitudinalRange);
    }

    public static TPoint ChangeDatum<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> sourceDatum, Ellipsoid<Meter, Degree> destinationDatum) where TPoint : IPoint, new()
    {
        Geodetic<Meter, Degree> source = new Geodetic<Meter, Degree>(new LinearCollection<Meter>(new double[] { 0 }),
                                                                        new AngularCollection<Degree>(new double[] { geodeticPoint.X }, AngleRange.ZeroTo2Pi),
                                                                        new AngularCollection<Degree>(new double[] { geodeticPoint.Y }, AngleRange.MinusPIToPI),
                                                                        sourceDatum);

        var result = ChangeDatum<Meter, Degree>(source, destinationDatum);

        return new TPoint() { X = result.Longitude.GetValue(0).Value, Y = result.Latitude.GetValue(0).Value };
    }

    public static PointZM ToCartesian(IPoint geodeticPoint, IEllipsoid ellipsoid)
    {
        var scale = Math.PI / 180.0;

        var phi = geodeticPoint.Y * scale;

        var lambda = geodeticPoint.X * scale;

        var cosPhi = Math.Cos(phi);

        var sinPhi = Math.Sin(phi);

        var cosLambda = Math.Cos(lambda);

        var sinLambda = Math.Sin(lambda);

        var N = ellipsoid.CalculateN(geodeticPoint.Y);

        var x = ellipsoid.DatumTranslation.X.Value + N * cosPhi * cosLambda;

        var y = ellipsoid.DatumTranslation.Y.Value + N * cosPhi * sinLambda;

        var z = ellipsoid.DatumTranslation.Z.Value + N * ellipsoid.SemiMinorAxis.Value * ellipsoid.SemiMinorAxis.Value / (ellipsoid.SemiMajorAxis.Value * ellipsoid.SemiMajorAxis.Value) * sinPhi;

        return new PointZM(x, y, z);
    }

    public static TPoint ToGeodetic<TPoint>(PointZM cartesianPoint, IEllipsoid ellipsoid) where TPoint : IPoint, new()
    {
        double tempSemiMajor = ellipsoid.SemiMajorAxis.Value;

        double tempSemiMinor = ellipsoid.SemiMinorAxis.Value;

        double e2TempValue = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        double tempX = cartesianPoint.X - ellipsoid.DatumTranslation.X.Value;

        double tempY = cartesianPoint.Y - ellipsoid.DatumTranslation.Y.Value;

        double tempZ = cartesianPoint.Z - ellipsoid.DatumTranslation.Z.Value;

        //PolarPoint<TLinear, TAngular> tempValue = (new Cartesian2DPoint<TLinear>(new TLinear() { Value = tempX }, new TLinear() { Value = tempY })).ToPolar<TLinear, TAngular>(longitudinalRange);

        //double pTempValue = tempValue.Radius.Value;

        double pTempValue = Math.Sqrt(tempX * tempX + tempY * tempY);

        double nTempValue = ellipsoid.SemiMajorAxis.Value;

        double hTempValue1 = Math.Sqrt(tempX * tempX + tempY * tempY + tempZ * tempZ)
                            -
                            Math.Sqrt(tempSemiMajor * tempSemiMinor);

        double latitudeTempValue1 = Math.Atan(tempZ / pTempValue *
                                                1.0 / (1 - (e2TempValue * nTempValue) / (nTempValue + hTempValue1)));

        if (latitudeTempValue1.Equals(double.NaN))
        {
            //return new GeodeticPoint<TLinear, TAngular>(ellipsoid, new TLinear() { Value = 0 }, tempValue.Angle, (new Radian(0, longitudinalRange)));
            return new TPoint();
        }

        double hTempValue2 = 0, latitudeTempValue2 = 0;

        bool conditionValue = true;

        do
        {
            nTempValue = ellipsoid.CalculateN(new Radian(latitudeTempValue1, AngleRange.MinusPIToPI)).Value;

            hTempValue2 = pTempValue / Math.Cos(latitudeTempValue1) - nTempValue;

            latitudeTempValue2 = Math.Atan(tempZ / pTempValue *
                                                1.0 / (1 - (e2TempValue * nTempValue) / (nTempValue + hTempValue2)));

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

        //TLinear height = new TLinear() { Value = hTempValue2 };

        //return new GeodeticPoint<TLinear, TAngular>(ellipsoid, height, tempValue.Angle, (new Radian(latitudeTempValue2, longitudinalRange)));
        return new TPoint() { X = Math.Atan2(tempY, tempX) * 180.0 / Math.PI, Y = latitudeTempValue2 * 180.0 / Math.PI };
    }

    public static TPoint ChangeDatumSimple<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> sourceDatum, Ellipsoid<Meter, Degree> destinationDatum) where TPoint : IPoint, new()
    {
        var cartesian = ToCartesian(geodeticPoint, sourceDatum);

        return ToGeodetic<TPoint>(cartesian, destinationDatum);
    }

    //public static Point3D GeodeticToCartesian(Point3D geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid)
    //{ 
    //    var latCos = Math.Cos(geodeticPoint.Y * Math.PI / 180.0);

    //    var latSin = Math.Sin(geodeticPoint.Y * Math.PI / 180.0);

    //    var longCos = Math.Cos(geodeticPoint.X * Math.PI / 180.0);

    //    var longSin = Math.Sin(geodeticPoint.X * Math.PI / 180.0);

    //    double tempE = ellipsoid.FirstEccentricity;

    //    double tempN = ellipsoid.CalculateN(geodeticPoint.Y * Math.PI / 180.0);

    //    Point3D result = new Point3D();

    //    result.X = (geodeticPoint.Z + tempN) * latCos * longCos;

    //    result.Y = (geodeticPoint.Z + tempN) * latCos * longSin;

    //    result.Z = (geodeticPoint.Z + tempN * (1 - tempE * tempE)) * latSin;

    //    return result;

    //}

    #endregion

    #region CT, LA

    public static Cartesian3D<T> LocalAstronomicToAverage<T>(ICartesian3D localAstronomic,
                                                                IGeodeticPoint initialPoint,
                                                                AngularUnit astronomicalLongitude,
                                                                AngularUnit astronomicalLatitude)
        where T : LinearUnit, new()
    {
        //local Astronomic is lefthanded
        Cartesian3DPoint<T> shift = Transformations.GeodeticToAverage<T>(initialPoint);

        Matrix rZ = CalculateRotationMatrixAroundZ(new Radian(Math.PI).Subtract(astronomicalLongitude));

        Matrix rY = CalculateRotationMatrixAroundY(new Radian(Math.PI / 2).Subtract(astronomicalLatitude));

        Matrix p2 = CalculateReflectionMatrix();

        //CT is Right Handed
        return localAstronomic.Transform(rZ * rY * p2, shift.Negate(), AxisType.RightHanded).ChangeTo<T>();
    }

    public static Cartesian3D<T> AverageToLocalAstronomic<T>(ICartesian3D averageTerrestrial,
                                                                IGeodeticPoint initialPoint,
                                                                AngularUnit astronomicalLongitude,
                                                                AngularUnit astronomicalLatitude)
        where T : LinearUnit, new()
    {
        //average Terrestrial is right handed
        // Error.NO.1: initialPoint.ToCartesian<T> is not correct we need the coordinate in CT System

        ICartesian3D tempCoordinate = averageTerrestrial.Shift(Transformations.GeodeticToAverage<T>(initialPoint));

        Matrix rZ = CalculateRotationMatrixAroundZ(astronomicalLongitude.Subtract(new Radian(Math.PI)));

        Matrix rY = CalculateRotationMatrixAroundY(astronomicalLatitude.Subtract(new Radian(Math.PI / 2)));

        Matrix p2 = CalculateReflectionMatrix();

        //LA is Left Handed
        return tempCoordinate.Transform(p2 * rY * rZ, AxisType.LeftHanded).ChangeTo<T>();
    }


    #endregion

    #region G, LG

    public static Cartesian3D<T> LocalGeodeticToGeodetic<T>(ICartesian3D localGeodetic,
                                                                IGeodeticPoint initialPoint)
        where T : LinearUnit, new()
    {
        //local geodetic is lefthanded
        Cartesian3DPoint<T> shift = Transformations.GeodeticToAverage<T>(initialPoint);

        Matrix rZ = CalculateRotationMatrixAroundZ(new Radian(Math.PI).Subtract(initialPoint.Longitude));

        Matrix rY = CalculateRotationMatrixAroundY(new Radian(Math.PI / 2).Subtract(initialPoint.Latitude));

        Matrix p2 = CalculateReflectionMatrix();

        //G is Right Handed
        return localGeodetic.Transform(rZ * rY * p2, shift.Negate(), AxisType.RightHanded).ChangeTo<T>();
    }

    public static Cartesian3D<T> GeodeticToLocalGeodetic<T>(ICartesian3D geodetic,
                                                                IGeodeticPoint initialPoint)
        where T : LinearUnit, new()
    {
        //averageTerrestrial is right handed

        //Error.NO2: do not use initialPoint.ToCartesian<T>() is this case. we need its coordinate in CT system
        ICartesian3D tempCoordinate = geodetic.Shift(Transformations.GeodeticToAverage<T>(initialPoint));

        Matrix rZ = CalculateRotationMatrixAroundZ(initialPoint.Longitude.Subtract(new Radian(Math.PI)));

        Matrix rY = CalculateRotationMatrixAroundY(initialPoint.Latitude.Subtract(new Radian(Math.PI / 2)));

        Matrix p2 = CalculateReflectionMatrix();

        //LA is Left Handed
        return tempCoordinate.Transform(p2 * rY * rZ, AxisType.LeftHanded).ChangeTo<T>();
    }

    #endregion

    #region LA, LG

    public static Cartesian3D<T> LocalAstronomicToLocalGeodetic<T>(ICartesian3D localAstronomic,
                                                                    AngularUnit initialAstronomicAzimuth,
                                                                    AngularUnit initialGeodeticAzimuth,
                                                                    AngularUnit xi,
                                                                    AngularUnit eta)
        where T : LinearUnit, new()
    {
        Matrix rZ = CalculateRotationMatrixAroundZ(initialAstronomicAzimuth.Subtract(initialGeodeticAzimuth));

        Matrix rY = CalculateRotationMatrixAroundY(xi.Negate());

        Matrix rX = CalculateRotationMatrixAroundX(eta);

        return localAstronomic.Rotate(rZ * rY * rX).ChangeTo<T>();
    }

    public static Cartesian3D<T> LocalAstronomicToLocalGeodetic<T>(ICartesian3D localAstronomic,
                                                                   AngularUnit deltaAzimuth,
                                                                   AngularUnit xi,
                                                                   AngularUnit eta)
       where T : LinearUnit, new()
    {
        Matrix rZ = CalculateRotationMatrixAroundZ(deltaAzimuth);

        Matrix rY = CalculateRotationMatrixAroundY(xi.Negate());

        Matrix rX = CalculateRotationMatrixAroundX(eta);

        return localAstronomic.Rotate(rZ * rY * rX).ChangeTo<T>();
    }


    public static Cartesian3D<T> LocalGeodeticToLocalAstronomic<T>(ICartesian3D localGeodetic,
                                                                   AngularUnit initialAstronomicAzimuth,
                                                                   AngularUnit initialGeodeticAzimuth,
                                                                   AngularUnit xi,
                                                                   AngularUnit eta)
       where T : LinearUnit, new()
    {
        Matrix rZ = CalculateRotationMatrixAroundZ((initialAstronomicAzimuth.Subtract(initialGeodeticAzimuth)).Negate());

        Matrix rY = CalculateRotationMatrixAroundY(xi);

        Matrix rX = CalculateRotationMatrixAroundX(eta.Negate());

        return localGeodetic.Rotate((rX * rY * rZ)).ChangeTo<T>();
    }

    public static Cartesian3D<T> LocalGeodeticToLocalAstronomic<T>(ICartesian3D localGeodetic,
                                                                   AngularUnit deltaAzimuth,
                                                                   AngularUnit xi,
                                                                   AngularUnit eta)
       where T : LinearUnit, new()
    {
        Matrix rZ = CalculateRotationMatrixAroundZ(deltaAzimuth.Negate());

        Matrix rY = CalculateRotationMatrixAroundY(xi);

        Matrix rX = CalculateRotationMatrixAroundX(eta.Negate());

        return localGeodetic.Rotate(rX * rY * rZ).ChangeTo<T>();
    }

    #endregion

    #region LA, HA

    public static Astronomical<T> LocalAstronomicToHorizontalAngle<T>(IAstronomical localAstronomic, AngularUnit astronomicalLatitude)
        where T : AngularUnit, new()
    {

        ICartesian3D tempCoordinate = localAstronomic.ToCartesian<Meter>();

        Matrix rZ = CalculateRotationMatrixAroundZ(new Radian(Math.PI));

        Matrix rY = CalculateRotationMatrixAroundY(astronomicalLatitude.Subtract(new Radian(Math.PI)));

        return tempCoordinate.Rotate(rY * rZ).ToAstronomicForm<T>(localAstronomic.HorizontalAngleRange);
    }

    public static Astronomical<T> HorizontalAngleToLocalAstronomic<T>(IAstronomical horizontalAngle, AngularUnit astronomicalLatitude)
        where T : AngularUnit, new()
    {

        ICartesian3D tempCoordinate = horizontalAngle.ToCartesian<Meter>();

        Matrix rZ = CalculateRotationMatrixAroundZ(new Radian(Math.PI));

        Matrix rY = CalculateRotationMatrixAroundY(astronomicalLatitude.Subtract(new Radian(Math.PI)).Negate());

        return tempCoordinate.Rotate(rZ * rY).ToAstronomicForm<T>(horizontalAngle.HorizontalAngleRange);
    }

    #endregion

    #region HA, AP

    public static Astronomical<T> ApparentPlaceToHorizontalAngle<T>(IAstronomical apparentPlace, AngularUnit localApparentSiderealTime)
        where T : AngularUnit, new()
    {

        ICartesian3D tempCoordinate = apparentPlace.ToCartesian<Meter>();

        Matrix rZ = CalculateRotationMatrixAroundZ(localApparentSiderealTime);

        Matrix reflection = CalculateReflectionMatrix();

        return tempCoordinate.Rotate(reflection * rZ).ToAstronomicForm<T>(apparentPlace.HorizontalAngleRange);
    }

    public static Astronomical<T> HorizontalAngleToApparentPlace<T>(IAstronomical horizontalAngle, AngularUnit localApparentSiderealTime)
        where T : AngularUnit, new()
    {
        ICartesian3D tempCoordinate = horizontalAngle.ToCartesian<Meter>();

        Matrix rZ = CalculateRotationMatrixAroundZ(localApparentSiderealTime.Negate());

        Matrix reflection = CalculateReflectionMatrix();

        return tempCoordinate.Rotate(rZ * reflection).ToAstronomicForm<T>(horizontalAngle.HorizontalAngleRange);
    }

    #endregion

    #region IT, AP

    public static Cartesian3D<TLinear> ApparentPlaceToInstantaneous<TLinear>(IAstronomical apparentPlace, AngularUnit greenwichApparentSiderealTime)
        where TLinear : LinearUnit, new()
    {
        ICartesian3D tempCoordinate = apparentPlace.ToCartesian<TLinear>();

        Matrix rZ = CalculateRotationMatrixAroundZ(greenwichApparentSiderealTime);

        return (Cartesian3D<TLinear>)tempCoordinate.Rotate(rZ);
    }

    public static Astronomical<TAngular> InstantaneousToApparentPlace<TAngular>(ICartesian3D InstantaneousTerrestrial,
                                                                                    AngularUnit greenwichApparentSiderealTime,
                                                                                    AngleRange horizontalAngleRange)
        where TAngular : AngularUnit, new()
    {
        Matrix rZ = CalculateRotationMatrixAroundZ(greenwichApparentSiderealTime.Negate());

        return InstantaneousTerrestrial.Rotate(rZ).ToAstronomicForm<TAngular>(horizontalAngleRange);
    }

    #endregion

    #region OR, AP

    public static Cartesian3D<TLinear> ApparentPlaceToOrbital<TLinear>(IAstronomical apparentPlace,
                                                                            AngularUnit rightAscensionOfAscendingNode,
                                                                            AngularUnit inclination,
                                                                            AngularUnit argumentOfPerigee)
        where TLinear : LinearUnit, new()
    {
        ICartesian3D tempCoordinate = apparentPlace.ToCartesian<TLinear>();

        Matrix firstRZ = CalculateRotationMatrixAroundZ(rightAscensionOfAscendingNode);

        Matrix rX = CalculateRotationMatrixAroundX(inclination);

        Matrix secondRZ = CalculateRotationMatrixAroundZ(argumentOfPerigee);

        return (Cartesian3D<TLinear>)tempCoordinate.Rotate(secondRZ * rX * firstRZ);
    }

    public static Astronomical<TAngular> OrbitalToApparentPlace<TAngular>(ICartesian3D orbital,
                                                                            AngularUnit rightAscensionOfAscendingNode,
                                                                            AngularUnit inclination,
                                                                            AngularUnit argumentOfPerigee,
                                                                            AngleRange horizontalAngleRange)
        where TAngular : AngularUnit, new()
    {
        Matrix firstRZ = CalculateRotationMatrixAroundZ(rightAscensionOfAscendingNode.Negate());

        Matrix rX = CalculateRotationMatrixAroundX(inclination.Negate());

        Matrix secondRZ = CalculateRotationMatrixAroundZ(argumentOfPerigee.Negate());

        return orbital.Rotate(firstRZ * rX * secondRZ).ToAstronomicForm<TAngular>(horizontalAngleRange);
    }

    #endregion
}

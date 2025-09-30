using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public static class MapProjects
{
    #region Helper Functions

    internal static double _MaxConvertableToIsometricLatitude = 90;

    //phi = 90 => q=271
    internal static double _MaxAllowableIsometricLatitude = 271;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="latitude">Must be in Degree</param>
    /// <param name="firstEccentricity"></param>
    /// <returns></returns>
    public static double GeodeticLatitudeToIsometricLatitude(double latitude, double firstEccentricity)
    {
        //Limit the latitude value
        if (Math.Abs(latitude) > _MaxConvertableToIsometricLatitude)
        {
            latitude = _MaxConvertableToIsometricLatitude * (latitude < 0 ? -1 : 1);
        }

        double angleInRadian = latitude * Math.PI / 180;

        double eSin = firstEccentricity * Math.Sin(angleInRadian);

        return Math.Log(Math.Tan(Math.PI / 4 + angleInRadian / 2.0) * Math.Pow((1 - eSin) / (1 + eSin), firstEccentricity / 2.0)) * 180.0 / Math.PI;
    }

    public static double[] GeodeticLatitudeToIsometricLatitude(double[] latitudes, double firstEccentricity)
    {
        //phi must be in degree (Geodetic Latitude)
        //e=sqrt((a*a-b*b)/(a*a))

        double[] result = new double[latitudes.Length];

        for (int i = 0; i < latitudes.Length; i++)
        {
            result[i] = GeodeticLatitudeToIsometricLatitude(latitudes[i], firstEccentricity);
        }

        return result;
    }

    public static double IsometricLatitudeToGeodeticLatitude(double isometricLatitude, double e)
    {
        if (Math.Abs(isometricLatitude) > _MaxAllowableIsometricLatitude)
        {
            isometricLatitude = _MaxAllowableIsometricLatitude * (isometricLatitude < 0 ? -1 : 1);
        }

        if (double.IsNaN(isometricLatitude))
        {
            return double.NaN;
        }

        //'q must be in degree (Isometric Latitude)
        //'e=sqrt((a*a-b*b)/(a*a)):First Eccentricity of the Ellipsoid


        double tempQ = isometricLatitude * Math.PI / 180;

        double phi0 = 2 * Math.Atan(Math.Exp(tempQ)) - Math.PI / 2;

        byte counter = 0;

        while (true)
        {
            double sinOfphi0 = Math.Sin(phi0);

            double f0 = 1.0 / 2.0 * (Math.Log(1 + sinOfphi0) - Math.Log(1 - sinOfphi0) + e * Math.Log(1 - e * sinOfphi0) - e * Math.Log(1 + e * sinOfphi0)) - tempQ;

            double f1 = (1 - e * e) / ((1 - e * e * sinOfphi0 * sinOfphi0) * Math.Cos(phi0));

            double phi1 = phi0 - f0 / f1;

            counter++;

            if (Math.Abs(phi0 - phi1) < 0.1E-13)
            {
                return phi1 * 180 / Math.PI;
            }
            else if (counter == 10)
            {
                throw new NotImplementedException();
            }
            else
            {
                phi0 = phi1;
            }
        }
    }

    public static double[] IsometricLatitudeToGeodeticLatitude(double[] isometricLatitude, double e)
    {
        //'q must be in degree (Isometric Latitude)
        //'e=sqrt((a*a-b*b)/(a*a)):First Eccentricity of the Ellipsoid

        for (int i = 0; i < isometricLatitude.Length; i++)
        {
            isometricLatitude[i] = IsometricLatitudeToGeodeticLatitude(isometricLatitude[i], e);
        }

        return isometricLatitude;
    }

    public static double GetPrimeVerticalPlaneCurvatureRadius(double a, double e, double latitude)
    {
        //latitude must be in degree

        double temp = Math.Sin(latitude * Math.PI / 180);

        return a / Math.Sqrt(1.0 - e * e * temp * temp);
    }

    public static double GetMeridianCurvatureRadius(double a, double e, double latitude)
    {
        //latitude must be in degree

        double temp01 = Math.Sin(latitude * Math.PI / 180);

        double temp02 = 1.0 - e * e * temp01 * temp01;

        return a * (1 - e * e) / (temp02 * Math.Sqrt(temp02));
    }

    //*******
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="latitude">Must be in radian</param>
    /// <returns></returns>
    internal static double CalculateM(double e, double latitude)
    {
        //latitude must be in radian
        double sin = Math.Sin(latitude);

        return Math.Cos(latitude) / Math.Sqrt(1 - e * e * sin * sin);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="latitude">latitude must be in radian</param>
    /// <returns></returns>
    private static double CalculateQ(double e, double latitude)
    {
        //latitude must be in radian

        double sin = Math.Sin(latitude);

        double eSin = e * sin;

        return (1 - e * e) * (sin / (1 - eSin * eSin) - Math.Log((1 - eSin) / (1 + eSin)) / (2 * e));
    }

    //*******************************Others
    public static byte FindUtmZone(double lambda)
    {
        while (lambda < 0)
        {
            lambda += 360;
        }

        if (lambda >= 0 && lambda <= 180)
        {
            return (byte)(30 + Math.Ceiling(lambda / 6));
        }
        else if (lambda > 180 && lambda <= 360)
        {
            return (byte)(Math.Ceiling(lambda / 6) - 30);
        }
        else
        {
            throw new NotImplementedException();
        }

    }

    public static int CalculateCentralMeridian(int zone)
    {
        if (zone >= 0 && zone < 30)
        {
            return 180 + zone * 6 - 3;
        }
        else if (zone >= 30 && zone <= 60)
        {
            return (zone - 30) * 6 - 3;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static bool CheckLongitude(double centralLongitude, double[] longitude)
    {

        for (int i = 0; i < longitude.Length; i++)
        {
            if (Math.Abs(centralLongitude - longitude[i]) > 3)
            {
                return false;
            }
        }

        return true;
    }

    #endregion


    #region Mercator

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoint">(long, lat) pair in degree</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static TPoint GeodeticToMercator<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        double q = GeodeticLatitudeToIsometricLatitude(geodeticPoint.Y, ellipsoid.FirstEccentricity);

        return new TPoint()
        {
            X =
                ellipsoid.SemiMajorAxis.Value * geodeticPoint.X * Math.PI / 180,
            Y = ellipsoid.SemiMajorAxis.Value * q * Math.PI / 180
        };
    }

    /// <summary>
    /// Transform Geodetic point in WGS84 ellipsoid to Mercator point
    /// </summary>
    /// <param name="geodeticPoint"></param>
    /// <returns></returns>
    public static TPoint GeodeticToMercator<TPoint>(TPoint geodeticPoint) where TPoint : IPoint, new()
    {
        return GeodeticToMercator(geodeticPoint, Ellipsoids.WGS84);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoints">Array of (long, lat) pairs in degree</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static TPoint[] GeodeticToMercator<TPoint>(TPoint[] geodeticPoints, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        TPoint[] result = new TPoint[geodeticPoints.Length];

        for (int i = 0; i < geodeticPoints.Length; i++)
        {
            result[i] = GeodeticToMercator(geodeticPoints[i], ellipsoid);
        }

        return result;
    }

    public static double[][] GeodeticToMercator(double[] longitudes, double[] latitudes, Ellipsoid<Meter, Degree> ellipsoid)
    {
        if (longitudes.Length != latitudes.Length)
        {
            return null;
        }

        double[] q = GeodeticLatitudeToIsometricLatitude(latitudes, ellipsoid.FirstEccentricity);

        double[] x = new double[longitudes.Length];

        double[] y = new double[longitudes.Length];

        for (int i = 0; i < longitudes.Length; i++)
        {
            x[i] = ellipsoid.SemiMajorAxis.Value * longitudes[i] * Math.PI / 180;

            y[i] = ellipsoid.SemiMajorAxis.Value * q[i] * Math.PI / 180;
        }

        return new double[][] { x, y };
    }


    public static TPoint MercatorToGeodetic<TPoint>(TPoint mercatorPoint, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {

        double longitude = mercatorPoint.X / ellipsoid.SemiMajorAxis.Value * 180 / Math.PI;

        double q = mercatorPoint.Y / ellipsoid.SemiMajorAxis.Value * 180 / Math.PI;

        double latitude = IsometricLatitudeToGeodeticLatitude(q, ellipsoid.FirstEccentricity);

        return new TPoint() { X = longitude, Y = latitude };
    }

    /// <summary>
    /// Note: it use the WGS84 as the ellipsoid
    /// </summary>
    /// <param name="mercatorPoint"></param>
    /// <returns></returns>
    public static TPoint MercatorToGeodetic<TPoint>(TPoint mercatorPoint) where TPoint : IPoint, new()
    {
        return MercatorToGeodetic(mercatorPoint, Ellipsoids.WGS84);
    }

    public static TPoint[] MercatorToGeodetic<TPoint>(TPoint[] mercatorPoints, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        int n = mercatorPoints.Length;

        TPoint[] result = new TPoint[n];

        for (int i = 0; i < n; i++)
        {
            result[i] = MercatorToGeodetic(mercatorPoints[i], ellipsoid);
        }

        return result;
    }

    public static double[][] MercatorToGeodetic(double[] x, double[] y, Ellipsoid<Meter, Degree> ellipsoid)
    {
        if (x.Length != y.Length)
        {
            return null;
        }

        double[] q = new double[x.Length];

        double[] longitudes = new double[x.Length];

        for (int i = 0; i < x.Length; i++)
        {
            longitudes[i] = x[i] / ellipsoid.SemiMajorAxis.Value * 180 / Math.PI;

            q[i] = y[i] / ellipsoid.SemiMajorAxis.Value * 180 / Math.PI;
        }

        double[] latitudes = IsometricLatitudeToGeodeticLatitude(q, ellipsoid.FirstEccentricity);

        return new double[][] { longitudes, latitudes };
    }

    #endregion


    #region WebMercator

    public static TPoint GeodeticWgs84ToWebMercator<TPoint>(TPoint geodetic) where TPoint : IPoint, new()
    {
        var a = Ellipsoids.WGS84.SemiMajorAxis.Value;
        //var a = earthRadius;

        var x = a * geodetic.X * Math.PI / 180.0;

        double y;

        if (geodetic.Y == 0)
        {
            y = 0;
        }
        else
        {
            y = a * Math.Log(Math.Tan(Math.PI / 4.0 + geodetic.Y / 2.0 * Math.PI / 180.0));
        }

        return new TPoint() { X = x, Y = y };
    }

    public static TPoint WebMercatorToMercatorWgs84<TPoint>(TPoint webMercator) where TPoint : IPoint, new()
    {
        var earthRadius = Ellipsoids.WGS84.SemiMajorAxis.Value;

        var a = Ellipsoids.WGS84.SemiMajorAxis.Value;

        var e = Ellipsoids.WGS84.FirstEccentricity;

        var x = a / earthRadius * webMercator.X;

        var tempY = e * Math.Tanh(webMercator.Y / a);

        var y = a / earthRadius * webMercator.Y - a * e * (1.0 / 2.0) * Math.Log((1 + tempY) / (1 - tempY));

        return new TPoint() { X = x, Y = y };
    }

    public static TPoint WebMercatorToGeodeticWgs84Slow<TPoint>(TPoint webMercator) where TPoint : IPoint, new()
    {
        return MercatorToGeodetic(WebMercatorToMercatorWgs84(webMercator));
    }

    public static TPoint WebMercatorToGeodeticWgs84<TPoint>(TPoint webMercator) where TPoint : IPoint, new()
    {
        var a = Ellipsoids.WGS84.SemiMajorAxis.Value;

        double longitude = webMercator.X / a * 180 / Math.PI; //a = 6378137.0

        double latitude = 2.0 * (Math.Atan(Math.Exp(webMercator.Y / a)) - Math.PI / 4.0) * 180 / Math.PI;

        return new TPoint() { X = longitude, Y = latitude };
    }


    #endregion


    #region Transverse Mercator
    //*******************************Helper Functions
    private static double[] TMYTOGeodeticLatitude(double[] y, Ellipsoid<Meter, Degree> ellipsoid)
    {
        double e2 = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        double a = ellipsoid.SemiMajorAxis.Value;

        double A0 = 1.0 - 1.0 / 4.0 * e2 - 3.0 / 64.0 * e2 * e2 - 5.0 / 256.0 * Math.Pow(e2, 3) - 175.0 / 16384.0 * Math.Pow(e2, 4);

        double A2 = 3.0 / 8.0 * (e2 + e2 * e2 / 4.0 + 15.0 / 128.0 * Math.Pow(e2, 3) - 455.0 / 4096.0 * Math.Pow(e2, 4));

        double A4 = 15.0 / 256.0 * (e2 * e2 + 3.0 / 4.0 * Math.Pow(e2, 3) - 77.0 / 128.0 * Math.Pow(e2, 4));

        double A6 = 35.0 / 3072.0 * (Math.Pow(e2, 3) - 41.0 / 32.0 * Math.Pow(e2, 4));

        double A8 = -315.0 / 131072.0 * Math.Pow(e2, 4);

        double[] result = new double[y.Length];

        for (int i = 0; i < y.Length; i++)
        {
            double phi0 = y[i] / a;

            int counter = 0;

            while (true)
            {
                double f0 = a * (A0 * phi0 - A2 * Math.Sin(2 * phi0) + A4 * Math.Sin(4 * phi0) - A6 * Math.Sin(6 * phi0) + A8 * Math.Sin(8 * phi0)) - y[i];

                double f1 = a * (A0 - 2 * A2 * Math.Cos(2 * phi0) + 4 * A4 * Math.Cos(4 * phi0) - 6 * A6 * Math.Cos(6 * phi0) + 8 * A8 * Math.Cos(8 * phi0));

                double phi1 = phi0 - f0 / f1;

                counter++;

                if (Math.Abs(phi0 - phi1) < 0.1E-14)
                {
                    result[i] = phi1;

                    break;
                }
                else if (counter == 10)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    phi0 = phi1;
                }
            }
        }

        return result;
    }

    private static double TMYTOGeodeticLatitude(double y, Ellipsoid<Meter, Degree> ellipsoid)
    {
        double e2 = ellipsoid.FirstEccentricity * ellipsoid.FirstEccentricity;

        double a = ellipsoid.SemiMajorAxis.Value;

        double A0 = 1.0 - 1.0 / 4.0 * e2 - 3.0 / 64.0 * e2 * e2 - 5.0 / 256.0 * Math.Pow(e2, 3) - 175.0 / 16384.0 * Math.Pow(e2, 4);

        double A2 = 3.0 / 8.0 * (e2 + e2 * e2 / 4.0 + 15.0 / 128.0 * Math.Pow(e2, 3) - 455.0 / 4096.0 * Math.Pow(e2, 4));

        double A4 = 15.0 / 256.0 * (e2 * e2 + 3.0 / 4.0 * Math.Pow(e2, 3) - 77.0 / 128.0 * Math.Pow(e2, 4));

        double A6 = 35.0 / 3072.0 * (Math.Pow(e2, 3) - 41.0 / 32.0 * Math.Pow(e2, 4));

        double A8 = -315.0 / 131072.0 * Math.Pow(e2, 4);


        double result;

        double phi0 = y / a;

        int counter = 0;

        while (true)
        {
            double f0 = a * (A0 * phi0 - A2 * Math.Sin(2 * phi0) + A4 * Math.Sin(4 * phi0) - A6 * Math.Sin(6 * phi0) + A8 * Math.Sin(8 * phi0)) - y;

            double f1 = a * (A0 - 2 * A2 * Math.Cos(2 * phi0) + 4 * A4 * Math.Cos(4 * phi0) - 6 * A6 * Math.Cos(6 * phi0) + 8 * A8 * Math.Cos(8 * phi0));

            double phi1 = phi0 - f0 / f1;

            counter++;

            if (Math.Abs(phi0 - phi1) < 0.1E-14)
            {
                result = phi1;

                break;
            }
            else if (counter == 10)
            {
                System.Diagnostics.Debug.WriteLine("Calculating Latitude in TMYTOGeodeticLatitude results in NaN");

                return double.NaN;
            }
            else
            {
                phi0 = phi1;
            }
        }

        return result;
    }

    //*******************************Transformations
    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoint">(long, lat) pair in degree</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static TPoint GeodeticToTransverseMercator<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodeti Longitude in degree

        double a = ellipsoid.SemiMajorAxis.Value;

        double b = ellipsoid.SemiMinorAxis.Value;

        double e2 = (a * a - b * b) / (a * a);

        double A0 = 1.0 - 1.0 / 4.0 * e2 - 3.0 / 64.0 * e2 * e2 - 5.0 / 256.0 * Math.Pow(e2, 3) - 175.0 / 16384.0 * Math.Pow(e2, 4);

        double A2 = 3.0 / 8.0 * (e2 + e2 * e2 / 4.0 + 15.0 / 128.0 * Math.Pow(e2, 3) - 455.0 / 4096.0 * Math.Pow(e2, 4));

        double A4 = 15.0 / 256.0 * (e2 * e2 + 3.0 / 4.0 * Math.Pow(e2, 3) - 77.0 / 128.0 * Math.Pow(e2, 4));

        double A6 = 35.0 / 3072.0 * (Math.Pow(e2, 3) - 41.0 / 32.0 * Math.Pow(e2, 4));

        double A8 = -315.0 / 131072.0 * Math.Pow(e2, 4);


        double N = ellipsoid.CalculateN(new Degree(geodeticPoint.Y, AngleRange.MinusPiTOPi)).Value;

        double p = geodeticPoint.Y * Math.PI / 180.0;

        double l = geodeticPoint.X * Math.PI / 180.0;

        double t = Math.Tan(p);

        double noo2 = (a * a - b * b) / (b * b) * Math.Pow(Math.Cos(p), 2.0);

        double CosineP = Math.Cos(p);

        double SineP = Math.Sin(p);

        double x = l * Math.Cos(p) + Math.Pow(l * CosineP, 3) / 6.0 * (1 - t * t + noo2);

        x = x + Math.Pow(l * CosineP, 5) / 120.0 * (5 - 18 * t * t + Math.Pow(t, 4) + 14 * noo2 - 58 * t * t * noo2
              + 13 * Math.Pow(noo2, 2) + 4 * Math.Pow(noo2, 3) - 64 * Math.Pow(noo2, 2) * t * t - 24 * Math.Pow(noo2, 3) * t * t);

        x = x + Math.Pow(l * CosineP, 7) / 5040.0 * (61 - 479 * t * t + 179 * Math.Pow(t, 4) - Math.Pow(t, 6));

        x = x * N;

        double y = a * (A0 * p - A2 * Math.Sin(2 * p) + A4 * Math.Sin(4 * p) - A6 * Math.Sin(6 * p) + A8 * Math.Sin(8 * p));

        y = y / N + l * l / 2 * SineP * CosineP;

        y = y + Math.Pow(l, 4) / 24.0 * SineP * Math.Pow(CosineP, 3) * (5 - t * t + 9 * noo2 + 4 * noo2 * noo2);

        y = y + Math.Pow(l, 6) / 720.0 * SineP * Math.Pow(CosineP, 5) * (61 - 58 * t * t + Math.Pow(t, 4) + 270 * noo2 - 330 * t * t * noo2
              + 445 * noo2 * noo2 + 324 * Math.Pow(noo2, 3) - 680 * Math.Pow(noo2, 2) * t * t + 88 * Math.Pow(noo2, 4) - 600 * Math.Pow(noo2, 3) * t * t - 192 * Math.Pow(noo2, 4) * t * t);

        y = y + Math.Pow(l, 8) / 40320.0 * SineP * Math.Pow(CosineP, 7) * (1385 - 311 * t * t + 543 * Math.Pow(t, 4) - Math.Pow(t, 6));

        y = y * N;

        return new TPoint() { X = x, Y = y };
    }

    /// <summary>
    /// Note: it use the WGS84 as the ellipsoid
    /// </summary>
    /// <param name="geodeticPoint"></param>
    /// <returns></returns>
    public static TPoint GeodeticToTransverseMercator<TPoint>(TPoint geodeticPoint) where TPoint : IPoint, new()
    {
        return GeodeticToTransverseMercator(geodeticPoint, Ellipsoids.WGS84);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoints">Array of (long, lat) pairs in degree</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static TPoint[] GeodeticToTransverseMercator<TPoint>(TPoint[] geodeticPoints, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodeti Longitude in degree

        int numberOfPoints = geodeticPoints.Length;

        TPoint[] result = new TPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = GeodeticToTransverseMercator(geodeticPoints[i], ellipsoid);
        }

        return result;
    }

    public static double[][] GeodeticToTransverseMercator(double[] longitudes, double[] latitudes, Ellipsoid<Meter, Degree> ellipsoid)
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodeti Longitude in degree

        if (longitudes.Length != latitudes.Length) return null;

        int numberOfPoints = longitudes.Length;

        double[] x = new double[numberOfPoints];

        double[] y = new double[numberOfPoints];

        double a = ellipsoid.SemiMajorAxis.Value;

        double b = ellipsoid.SemiMinorAxis.Value;

        double e2 = (a * a - b * b) / (a * a);

        double A0 = 1.0 - 1.0 / 4.0 * e2 - 3.0 / 64.0 * e2 * e2 - 5.0 / 256.0 * Math.Pow(e2, 3) - 175.0 / 16384.0 * Math.Pow(e2, 4);

        double A2 = 3.0 / 8.0 * (e2 + e2 * e2 / 4.0 + 15.0 / 128.0 * Math.Pow(e2, 3) - 455.0 / 4096.0 * Math.Pow(e2, 4));

        double A4 = 15.0 / 256.0 * (e2 * e2 + 3.0 / 4.0 * Math.Pow(e2, 3) - 77.0 / 128.0 * Math.Pow(e2, 4));

        double A6 = 35.0 / 3072.0 * (Math.Pow(e2, 3) - 41.0 / 32.0 * Math.Pow(e2, 4));

        double A8 = -315.0 / 131072.0 * Math.Pow(e2, 4);

        for (int i = 0; i < numberOfPoints; i++)
        {
            double N = ellipsoid.CalculateN(new Degree(latitudes[i], AngleRange.MinusPiTOPi)).Value;

            double p = latitudes[i] * Math.PI / 180.0;

            double l = longitudes[i] * Math.PI / 180.0;

            double t = Math.Tan(p);

            double noo2 = (a * a - b * b) / (b * b) * Math.Pow(Math.Cos(p), 2.0);

            double CosineP = Math.Cos(p);

            double SineP = Math.Sin(p);

            x[i] = l * Math.Cos(p) + Math.Pow(l * CosineP, 3) / 6.0 * (1 - t * t + noo2);

            x[i] = x[i] + Math.Pow(l * CosineP, 5) / 120.0 * (5 - 18 * t * t + Math.Pow(t, 4) + 14 * noo2 - 58 * t * t * noo2
                        + 13 * Math.Pow(noo2, 2) + 4 * Math.Pow(noo2, 3) - 64 * Math.Pow(noo2, 2) * t * t - 24 * Math.Pow(noo2, 3) * t * t);

            x[i] = x[i] + Math.Pow(l * CosineP, 7) / 5040.0 * (61 - 479 * t * t + 179 * Math.Pow(t, 4) - Math.Pow(t, 6));

            x[i] = x[i] * N;

            y[i] = a * (A0 * p - A2 * Math.Sin(2 * p) + A4 * Math.Sin(4 * p) - A6 * Math.Sin(6 * p) + A8 * Math.Sin(8 * p));

            y[i] = y[i] / N + l * l / 2 * SineP * CosineP;

            y[i] = y[i] + Math.Pow(l, 4) / 24.0 * SineP * Math.Pow(CosineP, 3) * (5 - t * t + 9 * noo2 + 4 * noo2 * noo2);

            y[i] = y[i] + Math.Pow(l, 6) / 720.0 * SineP * Math.Pow(CosineP, 5) * (61 - 58 * t * t + Math.Pow(t, 4) + 270 * noo2 - 330 * t * t * noo2
                    + 445 * noo2 * noo2 + 324 * Math.Pow(noo2, 3) - 680 * Math.Pow(noo2, 2) * t * t + 88 * Math.Pow(noo2, 4) - 600 * Math.Pow(noo2, 3) * t * t - 192 * Math.Pow(noo2, 4) * t * t);

            y[i] = y[i] + Math.Pow(l, 8) / 40320.0 * SineP * Math.Pow(CosineP, 7) * (1385 - 311 * t * t + 543 * Math.Pow(t, 4) - Math.Pow(t, 6));

            y[i] = y[i] * N;
        }

        return new double[][] { x, y };
    }


    public static TPoint TransverseMercatorToGeodetic<TPoint>(TPoint tmPoint, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        double a = ellipsoid.SemiMajorAxis.Value;

        double b = ellipsoid.SemiMinorAxis.Value;

        double e2 = (a * a - b * b) / (a * a);

        double ePrime2 = (a * a - b * b) / (b * b);

        double phiExpansionPoint = TMYTOGeodeticLatitude(tmPoint.Y, ellipsoid);

        if (double.IsNaN(phiExpansionPoint))
        {
            //return Point.NaN;
            return new TPoint() { X = double.NaN, Y = double.NaN };
        }

        double N = ellipsoid.CalculateN(new Degree(phiExpansionPoint * 180 / Math.PI, AngleRange.MinusPiTOPi)).Value;

        double M = ellipsoid.CalculateM(new Degree(phiExpansionPoint * 180 / Math.PI, AngleRange.MinusPiTOPi)).Value;

        double t = Math.Tan(phiExpansionPoint);

        double noo2 = ePrime2 * Math.Pow(Math.Cos(phiExpansionPoint), 2);

        double longitude = tmPoint.X / N - 1 / 6.0 * Math.Pow(tmPoint.X / N, 3) * (1 + 2 * t * t + noo2);

        longitude = longitude + 1 / 120.0 * Math.Pow(tmPoint.X / N, 5) * (5 + 6 * noo2 + 28 * t * t - 3 * Math.Pow(noo2, 2)
                 + 8 * t * t * noo2 + 24 * Math.Pow(t, 4) - 4 * Math.Pow(noo2, 3) + 4 * t * t * Math.Pow(noo2, 2) + 24 * t * t * Math.Pow(noo2, 3));

        longitude = longitude - 1 / 5040.0 * Math.Pow(tmPoint.X / N, 7) * (61 + 662 * t * t + 1320 * Math.Pow(t, 4) + 720 * Math.Pow(t, 6));

        longitude = longitude / Math.Cos(phiExpansionPoint) * 180 / Math.PI;


        double latitude = phiExpansionPoint - t * tmPoint.X * tmPoint.X / (2 * M * N);

        latitude = latitude + t * Math.Pow(tmPoint.X, 4) / (24.0 * M * Math.Pow(N, 3)) * (5 + 3 * t * t + noo2 - 4 * Math.Pow(noo2, 2) - 9 * t * t * noo2);

        latitude = latitude - t * Math.Pow(tmPoint.X, 6) / (720.0 * M * Math.Pow(N, 5)) * (61 - 90 * t * t + 46 * noo2
                 + 45 * Math.Pow(t, 4) - 252 * t * t * noo2 - 3 * Math.Pow(noo2, 2) + 100 * Math.Pow(noo2, 3) - 66 * t * t * Math.Pow(noo2, 2)
                 - 90 * Math.Pow(t, 4) * noo2 + 88 * Math.Pow(noo2, 4) + 225 * Math.Pow(t, 4) * Math.Pow(noo2, 2) + 84 * t * t * Math.Pow(noo2, 3) - 192 * t * t * Math.Pow(noo2, 4));

        latitude = latitude + t * Math.Pow(tmPoint.X, 8) / (40320.0 * M * Math.Pow(N, 7)) * (1385 + 3633 * t * t + 4095 * Math.Pow(t, 4) + 1575 * Math.Pow(t, 6));

        latitude = latitude * 180 / Math.PI;

        return new TPoint() { X = longitude, Y = latitude };
    }

    public static TPoint[] TransverseMercatorToGeodetic<TPoint>(TPoint[] tmPoints, Ellipsoid<Meter, Degree> ellipsoid) where TPoint : IPoint, new()
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodeti Longitude in degree

        int numberOfPoints = tmPoints.Length;

        TPoint[] result = new TPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = TransverseMercatorToGeodetic(tmPoints[i], ellipsoid);
        }

        return result;
    }

    public static double[][] TransverseMercatorToGeodetic(double[] x, double[] y, Ellipsoid<Meter, Degree> ellipsoid)
    {
        if (x.Length != y.Length)
        {
            throw new NotImplementedException();
        }

        double a = ellipsoid.SemiMajorAxis.Value;

        double b = ellipsoid.SemiMinorAxis.Value;

        double e2 = (a * a - b * b) / (a * a);

        double ePrime2 = (a * a - b * b) / (b * b);

        double[] longitude = new double[x.Length];

        double[] latitude = new double[x.Length];

        double[] phiExpansionPoint = TMYTOGeodeticLatitude(y, ellipsoid);

        for (int i = 0; i < x.Length; i++)
        {
            double N = ellipsoid.CalculateN(new Degree(phiExpansionPoint[i] * 180 / Math.PI, AngleRange.MinusPiTOPi)).Value;

            double M = ellipsoid.CalculateM(new Degree(phiExpansionPoint[i] * 180 / Math.PI, AngleRange.MinusPiTOPi)).Value;

            double t = Math.Tan(phiExpansionPoint[i]);

            double noo2 = ePrime2 * Math.Pow(Math.Cos(phiExpansionPoint[i]), 2);

            longitude[i] = x[i] / N - 1 / 6.0 * Math.Pow(x[i] / N, 3) * (1 + 2 * t * t + noo2);

            longitude[i] = longitude[i] + 1 / 120.0 * Math.Pow(x[i] / N, 5) * (5 + 6 * noo2 + 28 * t * t - 3 * Math.Pow(noo2, 2)
                        + 8 * t * t * noo2 + 24 * Math.Pow(t, 4) - 4 * Math.Pow(noo2, 3) + 4 * t * t * Math.Pow(noo2, 2) + 24 * t * t * Math.Pow(noo2, 3));

            longitude[i] = longitude[i] - 1 / 5040.0 * Math.Pow(x[i] / N, 7) * (61 + 662 * t * t + 1320 * Math.Pow(t, 4) + 720 * Math.Pow(t, 6));

            longitude[i] = longitude[i] / Math.Cos(phiExpansionPoint[i]) * 180 / Math.PI;


            latitude[i] = phiExpansionPoint[i] - t * x[i] * x[i] / (2 * M * N);

            latitude[i] = latitude[i] + t * Math.Pow(x[i], 4) / (24.0 * M * Math.Pow(N, 3)) * (5 + 3 * t * t + noo2 - 4 * Math.Pow(noo2, 2) - 9 * t * t * noo2);

            latitude[i] = latitude[i] - t * Math.Pow(x[i], 6) / (720.0 * M * Math.Pow(N, 5)) * (61 - 90 * t * t + 46 * noo2
                     + 45 * Math.Pow(t, 4) - 252 * t * t * noo2 - 3 * Math.Pow(noo2, 2) + 100 * Math.Pow(noo2, 3) - 66 * t * t * Math.Pow(noo2, 2)
                     - 90 * Math.Pow(t, 4) * noo2 + 88 * Math.Pow(noo2, 4) + 225 * Math.Pow(t, 4) * Math.Pow(noo2, 2) + 84 * t * t * Math.Pow(noo2, 3) - 192 * t * t * Math.Pow(noo2, 4));

            latitude[i] = latitude[i] + t * Math.Pow(x[i], 8) / (40320.0 * M * Math.Pow(N, 7)) * (1385 + 3633 * t * t + 4095 * Math.Pow(t, 4) + 1575 * Math.Pow(t, 6));

            latitude[i] = latitude[i] * 180 / Math.PI;
        }

        return new double[][] { longitude, latitude };
    }

    #endregion


    #region UTM

    //*******************************UTM
    public static TPoint GeodeticToUTM<TPoint>(TPoint geodeticPoint, bool isNorthHemisphere = true) where TPoint : IPoint, new()
    {
        return GeodeticToUTM(geodeticPoint, Ellipsoids.WGS84, isNorthHemisphere);
    }

    public static TPoint GeodeticToUTM<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid, int zone, bool isNorthHemisphere = true) where TPoint : IPoint, new()
    {
        int centralMeredian = CalculateCentralMeridian(zone);

        double tempLongitude = geodeticPoint.X - centralMeredian;

        Point result = GeodeticToTransverseMercator(new Point(tempLongitude, geodeticPoint.Y), ellipsoid);

        return new TPoint()
        {
            X = result.X * 0.9996 + 500000,
            Y = isNorthHemisphere ? result.Y * 0.9996 : result.Y * 0.9996 + 10000000
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoint">(long, lat) pair in degree</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static TPoint GeodeticToUTM<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid, bool isNorthHemisphere = true) where TPoint : IPoint, new()
    {
        int zone = FindUtmZone(geodeticPoint.X);

        return GeodeticToUTM(geodeticPoint, ellipsoid, zone, isNorthHemisphere);
        //int centralMeredian = CalculateCentralMeridian(zone);

        //double tempLongitude = geodeticPoint.X - centralMeredian;

        //Point result = GeodeticToTransverseMercator(new Point(tempLongitude, geodeticPoint.Y), ellipsoid);

        //return new Point(result.X * 0.9996 + 500000, isNorthHemisphere ? result.Y * 0.9996 : result.Y * 0.9996 + 10000000);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoints">(long, lat) pairs in degree</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static TPoint[] GeodeticToUTM<TPoint>(TPoint[] geodeticPoints, Ellipsoid<Meter, Degree> ellipsoid, bool isNorthHemisphere = true) where TPoint : IPoint, new()
    {
        int numberOfPoints = geodeticPoints.Length;

        TPoint[] result = new TPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = GeodeticToUTM(geodeticPoints[i], ellipsoid, isNorthHemisphere);
        }

        return result;
    }

    public static TPoint[] GeodeticToUTM<TPoint>(TPoint[] geodeticPoints, bool isNorthHemisphere = true) where TPoint : IPoint, new()
    {
        return GeodeticToUTM(geodeticPoints, Ellipsoids.WGS84, isNorthHemisphere);
    }

    public static double[][] GeodeticToUTM(double[] longitude, double[] latitude, Ellipsoid<Meter, Degree> ellipsoid, double centralLongitude)
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodeti Longitude in degree
        //centralLongitude: must be in degree

        if (longitude.Length != latitude.Length)
        {
            throw new NotImplementedException();
        }

        double[] tempLongitude = new double[longitude.Length];

        for (int i = 0; i < longitude.Length; i++)
        {
            tempLongitude[i] = longitude[i] - centralLongitude;
        }

        double[][] result = GeodeticToTransverseMercator(tempLongitude, latitude, ellipsoid);

        for (int i = 0; i < longitude.Length; i++)
        {
            result[0][i] = result[0][i] * 0.9996 + 500000;

            //result[1][i] = result[1][i] * 0.9996 + 10000000;
            result[1][i] = result[1][i] * 0.9996;
        }

        return result;
    }

    public static double[][] GeodeticToUTM(double[] longitude, double[] latitude, Ellipsoid<Meter, Degree> ellipsoid)
    {
        int zone = FindUtmZone(longitude[0]);

        return GeodeticToUTM(longitude, latitude, ellipsoid, zone);
    }



    public static TPoint UTMToGeodetic<TPoint>(TPoint utmPoint, Ellipsoid<Meter, Degree> ellipsoid, double centralLongitude, bool isNorthHemisphere = true) where TPoint : IPoint, new()
    {
        double tempX = 0;
        double tempY = 0;

        tempX = (utmPoint.X - 500000) / 0.9996;

        tempY = isNorthHemisphere ? utmPoint.Y / 0.9996 : (utmPoint.Y - 10000000.0) / 0.9996;

        Point result = TransverseMercatorToGeodetic(new Point(tempX, tempY), ellipsoid);

        return new TPoint()
        {
            X = result.X + centralLongitude,
            Y = result.Y
        };

        //result.X = result.X + centralLongitude;

        //return result;
    }

    public static TPoint UTMToGeodetic<TPoint>(TPoint utmPoint, Ellipsoid<Meter, Degree> ellipsoid, int zone) where TPoint : IPoint, new()
    {
        double centralLongitude = CalculateCentralMeridian(zone);

        return UTMToGeodetic(utmPoint, ellipsoid, centralLongitude);
    }

    public static TPoint UTMToGeodetic<TPoint>(TPoint utmPoint, int zone) where TPoint : IPoint, new()
    {
        return UTMToGeodetic(utmPoint, Ellipsoids.WGS84, zone);
    }

    public static TPoint[] UTMToGeodetic<TPoint>(TPoint[] utmPoints, Ellipsoid<Meter, Degree> ellipsoid, int zone) where TPoint : IPoint, new()
    {
        int numberOfPoints = utmPoints.Length;

        TPoint[] result = new TPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = UTMToGeodetic(utmPoints[i], ellipsoid, zone);
        }

        return result;
    }

    //public static double[][] UTMToGeodetic(double[] x, double[] y, Ellipsoid<Meter, Degree> ellipsoid, int zone)
    //{
    //    if (x.Length != y.Length)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    double[] tempX = new double[x.Length];
    //    double[] tempY = new double[x.Length];

    //    for (int i = 0; i < x.Length; i++)
    //    {
    //        tempX[i] = (x[i] - 500000) / 0.9996;

    //        //y[i] = (y[i] - 10000000) / 0.9996;
    //        tempY[i] = (y[i]) / 0.9996;
    //    }

    //    double[][] result = TransverseMercatorToGeodetic(tempX, tempY, ellipsoid);

    //    double centralLongitude = CalculateCentralMeridian(zone);

    //    for (int i = 0; i < x.Length; i++)
    //    {
    //        result[0][i] = result[0][i] + centralLongitude;
    //    }

    //    return result;
    //}

    #endregion


    #region Cylindrical Equal-area

    //*******************************Cylindrical Equal-area
    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoint">(long, lat) pair in degree</param>
    /// <param name="ellipsoid"></param>
    /// <param name="centralLongitude"></param>
    /// <param name="standardLatitude"></param>
    /// <returns></returns>
    public static TPoint GeodeticToCylindricalEqualArea<TPoint>(TPoint geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid, double centralLongitude = 0, double standardLatitude = 0) where TPoint : IPoint, new()
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodetic Longitude in degree
        //centralLongitude: must be in degree

        double e = ellipsoid.FirstEccentricity;

        double phi0 = standardLatitude * Math.PI / 180.0;

        //double k0 = Math.Cos(phi0) / Math.Pow(1 - e * e * Math.Sin(phi0) * Math.Sin(phi0), 0.5);
        double k0 = Math.Cos(phi0) / Math.Sqrt(1 - e * e * Math.Sin(phi0) * Math.Sin(phi0));

        double eSin = e * Math.Sin(geodeticPoint.Y * Math.PI / 180.0);

        double q = (1.0 - e * e) * (
            Math.Sin(geodeticPoint.Y * Math.PI / 180.0) / (1.0 - eSin * eSin) -
            Math.Log((1.0 - eSin) / (1.0 + eSin)) / (2.0 * e));

        return new TPoint()
        {
            X = ellipsoid.SemiMajorAxis.Value * k0 * (geodeticPoint.X - centralLongitude) * Math.PI / 180.0,
            Y = ellipsoid.SemiMajorAxis.Value * q / (2.0 * k0)
        };
    }

    public static TPoint GeodeticToCylindricalEqualArea<TPoint>(TPoint geodeticPoint) where TPoint : IPoint, new()
    {
        return GeodeticToCylindricalEqualArea(geodeticPoint, Ellipsoids.WGS84);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geodeticPoints">Array of (long, lat) pairs in degree</param>
    /// <param name="ellipsoid"></param>
    /// <param name="centralLongitude"></param>
    /// <param name="standardLatitude"></param>
    /// <returns></returns>
    public static TPoint[] GeodeticToCylindricalEqualArea<TPoint>(TPoint[] geodeticPoints, Ellipsoid<Meter, Degree> ellipsoid, double centralLongitude = 0, double standardLatitude = 0) where TPoint : IPoint, new()
    {
        int numberOfPoints = geodeticPoints.Length;

        TPoint[] result = new TPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = GeodeticToCylindricalEqualArea(geodeticPoints[i], ellipsoid, centralLongitude, standardLatitude);
        }

        return result;
    }

    public static double[][] GeodeticToCylindricalEqualArea(double[] longitude, double[] latitude, Ellipsoid<Meter, Degree> ellipsoid, double centralLongitude = 0, double standardLatitude = 0)
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodetic Longitude in degree
        //centralLongitude: must be in degree

        if (longitude.Length != latitude.Length)
        {
            throw new NotImplementedException();
        }

        int numberOfPoints = longitude.Length;

        double[] x = new double[numberOfPoints];

        double[] y = new double[numberOfPoints];

        double e = ellipsoid.FirstEccentricity;

        double phi0 = standardLatitude * Math.PI / 180.0;

        double k0 = Math.Cos(phi0) / Math.Pow(1 - e * e * Math.Sin(phi0) * Math.Sin(phi0), 0.5);

        for (int i = 0; i < longitude.Length; i++)
        {
            double eSin = e * Math.Sin(latitude[i] * Math.PI / 180.0);

            double q = (1.0 - e * e) * (
                Math.Sin(latitude[i] * Math.PI / 180.0) / (1.0 - eSin * eSin) -
                Math.Log((1.0 - eSin) / (1.0 + eSin)) / (2.0 * e));

            x[i] = ellipsoid.SemiMajorAxis.Value * k0 * (longitude[i] - centralLongitude) * Math.PI / 180.0;

            y[i] = ellipsoid.SemiMajorAxis.Value * q / (2.0 * k0);
        }

        return new double[][] { x, y };

    }


    public static TPoint CylindricalEqualAreaToGeodetic<TPoint>(TPoint ceaPoint, Ellipsoid<Meter, Degree> ellipsoid, double centeralLongitude = 0, double standardLatitude = 0) where TPoint : IPoint, new()
    {
        double e = ellipsoid.FirstEccentricity;

        double phi0 = standardLatitude * Math.PI / 180.0;

        double k0 = Math.Cos(phi0) / Math.Sqrt(1 - e * e * Math.Sin(phi0) * Math.Sin(phi0));

        double qAtPole = (1.0 - e * e) * (
            1.0 / (1.0 - e * e) -
            1.0 / (2.0 * e) *
            Math.Log((1.0 - e) / (1.0 + e)));

        double longitude = centeralLongitude + ceaPoint.X / (ellipsoid.SemiMajorAxis.Value * k0) * 180 / Math.PI;

        double beta = Math.Asin(2 * ceaPoint.Y * k0 / (ellipsoid.SemiMajorAxis.Value * qAtPole));

        double deltaLambda = (centeralLongitude - longitude) * Math.PI / 180.0;

        double qC = qAtPole * Math.Sin(beta);

        double latitude = IterativelyComputeLatitude(qC, e) * 180.0 / Math.PI;

        return new TPoint() { X = longitude, Y = latitude };

    }

    public static TPoint[] CylindricalEqualAreaToGeodetic<TPoint>(TPoint[] ceaPoints, Ellipsoid<Meter, Degree> ellipsoid, double centralLongitude = 0, double standardLatitude = 0) where TPoint : IPoint, new()
    {
        int numberOfPoints = ceaPoints.Length;

        TPoint[] result = new TPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            result[i] = CylindricalEqualAreaToGeodetic(ceaPoints[i], ellipsoid, centralLongitude, standardLatitude);
        }

        return result;
    }

    public static double[][] CylindricalEqualAreaToGeodetic(double[] x, double[] y, Ellipsoid<Meter, Degree> ellipsoid, double centeralLongitude = 0, double standardLatitude = 0)
    {
        //'Phi : Geodetic Latitude, in degree
        //'Lambda : Geodetic Longitude in degree
        //centralLongitude: must be in degree

        if (x.Length != y.Length)
        {
            throw new NotImplementedException();
        }

        int numberOfPoints = x.Length;

        double[] latitude = new double[numberOfPoints];

        double[] longitude = new double[numberOfPoints];

        double e = ellipsoid.FirstEccentricity;

        double phi0 = standardLatitude * Math.PI / 180.0;

        double k0 = Math.Cos(phi0) / Math.Pow(1 - e * e * Math.Sin(phi0) * Math.Sin(phi0), 0.5);

        double qAtPole = (1.0 - e * e) * (
            1.0 / (1.0 - e * e) -
            1.0 / (2.0 * e) *
            Math.Log((1.0 - e) / (1.0 + e)));

        for (int i = 0; i < x.Length; i++)
        {
            longitude[i] = centeralLongitude + x[i] / (ellipsoid.SemiMajorAxis.Value * k0) * 180 / Math.PI;

            double beta = Math.Asin(2 * y[i] * k0 / (ellipsoid.SemiMajorAxis.Value * qAtPole));

            double deltaLambda = (centeralLongitude - longitude[i]) * Math.PI / 180.0;

            //double betaC = Math.Atan(Math.Tan(beta) / Math.Cos(deltaLambda));

            //double qC = qp * Math.Sin(betaC);
            double qC = qAtPole * Math.Sin(beta);

            latitude[i] = IterativelyComputeLatitude(qC, e) * 180.0 / Math.PI;
        }

        return new double[][] { longitude, latitude };

    }

    private static double IterativelyComputeLatitude(double qC, double e)
    {
        double phiC = Math.Asin(qC / 2.0);

        double temp;

        do
        {
            double eSin = e * Math.Sin(phiC);

            temp = (1 - eSin * eSin) * (1 - eSin * eSin) / (2 * Math.Cos(phiC)) *
                            (qC / (1 - e * e) - Math.Sin(phiC) / (1 - eSin * eSin) + 1.0 / (2.0 * e) * Math.Log((1.0 - eSin) / (1.0 + eSin)));

            phiC = phiC + temp;

        } while (Math.Abs(temp) > 10E-10);

        return phiC;
    }

    #endregion


    #region Albers Equal Area Conic
    //*******************************Albers Equal Area Conic
    public static double[][] GeodeticToAlbersEqualAreaConic(double[] longitude, double[] latitude,
                                                                Ellipsoid<Meter, Degree> ellipsoid,
                                                                double centralLongitude,
                                                                double standardLatitude,
                                                                double firstParallel, double secondParallel)
    {
        double e = ellipsoid.FirstEccentricity;
        double phi1 = firstParallel * Math.PI / 180.0;
        double phi2 = secondParallel * Math.PI / 180.0;
        double phi0 = standardLatitude * Math.PI / 180.0;
        //double phi1 = firstParallel * Math.PI / 180.0;
        //double phi1 = firstParallel * Math.PI / 180.0;
        double n =
            (Math.Pow(CalculateM(e, phi1), 2) - Math.Pow(CalculateM(e, phi2), 2)) /
            (CalculateQ(e, phi2) - CalculateQ(e, phi1));

        double c = Math.Pow(CalculateM(e, phi1), 2) + n * CalculateQ(e, phi1);

        double a = ellipsoid.SemiMajorAxis.Value;

        double rho0 = a * Math.Sqrt(c - n * CalculateQ(e, phi0)) / n;

        int numberOfPoints = longitude.Length;

        double[] x = new double[numberOfPoints];

        double[] y = new double[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            double rho = a * Math.Sqrt(c - n * CalculateQ(e, latitude[i] * Math.PI / 180.0)) / n;

            double theta = n * (longitude[i] - centralLongitude) * Math.PI / 180.0;

            x[i] = rho * Math.Sin(theta);

            y[i] = rho0 - rho * Math.Cos(theta);
        }

        return new double[][] { x, y };
    }

    public static double[][] AlbersEqualAreaConicToGeodetic(double[] x, double[] y, Ellipsoid<Meter, Degree> ellipsoid,
                                                                double centralLongitude,
                                                                double standardLatitude,
                                                                double firstParallel, double secondParallel)
    {
        double e = ellipsoid.FirstEccentricity;
        double phi1 = firstParallel * Math.PI / 180.0;
        double phi2 = secondParallel * Math.PI / 180.0;
        double phi0 = standardLatitude * Math.PI / 180.0;
        //double phi1 = firstParallel * Math.PI / 180.0;
        //double phi1 = firstParallel * Math.PI / 180.0;
        double n =
            (Math.Pow(CalculateM(e, phi1), 2) - Math.Pow(CalculateM(e, phi2), 2)) /
            (CalculateQ(e, phi2) - CalculateQ(e, phi1));

        double c = Math.Pow(CalculateM(e, phi1), 2) + n * CalculateQ(e, phi1);

        double a = ellipsoid.SemiMajorAxis.Value;

        double rho0 = a * Math.Sqrt(c - n * CalculateQ(e, phi0)) / n;

        int numberOfPoints = x.Length;

        double[] longitude = new double[numberOfPoints];

        double[] latitude = new double[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            double theta = Math.Atan(x[i] / (rho0 - y[i]));

            double rho = Math.Sqrt(x[i] * x[i] + (rho0 - y[i]) * (rho0 - y[i]));

            double q = (c - rho * rho * n * n / (a * a)) / n;

            longitude[i] = centralLongitude + theta / n * 180.0 / Math.PI;

            latitude[i] = IterativelyComputeLatitude(q, e) * 180.0 / Math.PI;
        }

        return new double[][] { longitude, latitude };

    }

    #endregion


    /// <summary>
    /// Calculate the Elevation Factor
    /// </summary>
    /// <param name="h">height of the point above the Ellipsoid in meter</param>
    /// <returns></returns>
    public static double CalculateElevationFactor(double h)
    {
        return 6371000 / (6371000 + h);
    }

    public static double CalculateUTMScaleFactorByMercatorLocation<TPoint>(TPoint mercatorPoint) where TPoint : IPoint, new()
    {
        var geodeticPoint = MercatorToGeodetic(mercatorPoint);

        return CalculateUTMScaleFactor(geodeticPoint);
    }

    /// <summary>
    /// Calculate UTM Scale Factor
    /// </summary>
    /// <param name="geodeticPoint">phi, lambda</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static double CalculateUTMScaleFactor(IPoint geodeticPoint, Ellipsoid<Meter, Degree> ellipsoid)
    {
        double phi = geodeticPoint.Y * Math.PI / 180.0;

        double cosinePhi = Math.Cos(phi);

        double C = ellipsoid.SecondEccentricity * ellipsoid.SecondEccentricity * cosinePhi * cosinePhi;

        double k0 = 0.9996;

        double lambda0 = CalculateCentralMeridian(FindUtmZone(geodeticPoint.X));

        double deltaLambda = (geodeticPoint.X - lambda0) * Math.PI / 180.0;

        return k0 * (1.0 + (1.0 + C) * (deltaLambda * deltaLambda * cosinePhi * cosinePhi) / 2.0); 
    }

    /// <summary>
    /// Calculate UTM Scale Factor for WGS84
    /// </summary>
    /// <param name="geodeticPoint">phi, lambda</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static double CalculateUTMScaleFactor(IPoint geodeticPoint)
    {
        return CalculateUTMScaleFactor(geodeticPoint, Ellipsoids.WGS84);
    }

    /// <summary>
    /// Calcualte the grid factor = UTM Scale Factor * Elevation Factor
    /// </summary>
    /// <param name="geodeticPoint">lambda, phi</param>
    /// <param name="h">Elevation above the ellipsoid</param>
    /// <param name="ellipsoid"></param>
    /// <returns></returns>
    public static double CalculateGridFactor(IPoint geodeticPoint, double h, Ellipsoid<Meter, Degree> ellipsoid)
    {
        return CalculateUTMScaleFactor(geodeticPoint, ellipsoid) * CalculateElevationFactor(h);
    }

    /// <summary>
    /// Calcualte the grid factor = UTM Scale Factor * Elevation Factor based on WGS84
    /// </summary>
    /// <param name="geodeticPoint">lambda, phi</param>
    /// <returns></returns>
    public static double CalculateGridFactor(IPoint geodeticPoint)
    {
        return CalculateGridFactor(geodeticPoint);
    }

}
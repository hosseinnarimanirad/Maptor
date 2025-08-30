using System.Globalization;

using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Spatial.Analysis;

public static class SpatialUtility
{
    public const double EpsilonDistance = 0.0000001;

    /// <summary>
    /// return square (^2) of the Euclidian distance between two
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static double GetSquareEuclideanDistance<T>(T first, T second) where T : IPoint
    {
        var dx = first.X - second.X;

        var dy = first.Y - second.Y;

        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Euclidian distance
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static double GetEuclideanDistance<T>(T first, T second) where T : IPoint
    {
        return Math.Sqrt(GetSquareEuclideanDistance(first, second));
    }

    #region Area

    /// <summary>
    /// Calculate Signed Euclidean area for ring. 
    /// Clockwise rings have negative area and CounterClockwise rings have positive area
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="points">last point should not be repeated for ring</param>
    /// <returns></returns>
    public static double GetSignedRingArea<T>(List<T> points) where T : IPoint
    {
        if (points == null || points.Count < 3)
            return 0;

        if (SpatialUtility.GetEuclideanDistance(points[0], points[points.Count - 1]) == 0)
            throw new NotImplementedException("SpatialUtility > GetSignedRingArea");

        double area = 0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            double temp = points[i].X * points[i + 1].Y - points[i].Y * points[i + 1].X;

            area += temp;
        }

        // 1399.06.11
        // تکرار نقطه اخر چند ضلعی
        // فرض بر این هست که داخل لیست نقطه‌ها
        // این نقطه تکرار نشده باشه
        area += points[points.Count - 1].X * points[0].Y - points[points.Count - 1].Y * points[0].X;

        return area / 2.0;
    }

    //1399.06.11
    //در این جا فرض شده که نقطه اخر چند حلقه تکرار 
    //نشده
    /// <summary>
    /// Calculate unsigned Euclidean area for ring. 
    /// </summary>
    /// <param name="points">last point should not be repeated for ring</param>
    /// <returns></returns>
    public static double GetUnsignedRingArea<T>(List<T> points) where T : IPoint, new()
    {
        return Math.Abs(GetSignedRingArea(points));
    }

    /// <summary>
    /// Calculate unsigned Euclidean area for triangle
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetUnsignedTriangleArea<T>(T firstPoint, T middlePoint, T lastPoint) where T : IPoint
    {
        return Math.Abs(GetSignedTriangleArea(firstPoint, middlePoint, lastPoint));
    }

    /// <summary>
    /// Calculate signed Euclidean area for triangle
    /// Clockwise triangles have negative area and CounterClockwise triangles have positive area
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetSignedTriangleArea<T>(T firstPoint, T middlePoint, T lastPoint) where T : IPoint
    {
        return (firstPoint.X * (middlePoint.Y - lastPoint.Y) + middlePoint.X * (lastPoint.Y - firstPoint.Y) + lastPoint.X * (firstPoint.Y - middlePoint.Y)) / 2.0;
    }

    #endregion

    #region True Ground Area

    #region True Area

    public static double CalculateGroundArea(Geometry<Point> geometry, Func<Point, Point> toWgs84Geodetic)
    {
        return CalculateGroundArea(geometry.Transform(toWgs84Geodetic, SridHelper.GeodeticWGS84));
    }

    public static double CalculateGroundArea<T>(Geometry<T> geography) where T : IPoint, new()
    {
        var newGeo = geography.Project(new CylindricalEqualArea());

        return newGeo.EuclideanArea;
    }

    #endregion

    #region Ellipsoidal Area (Authalic Sphere)

    /// <summary>
    /// Calculates the area (m²) of this geometry on the WGS-84 ellipsoid,
    /// using the authalic (equal-area) sphere approximation.
    /// For Polygon: outer ring area minus sum of hole areas.
    /// For MultiPolygon: sum of polygon areas.
    /// Returns 0 for non-areal geometries.
    /// </summary>
    public static double CalculateSphericalArea<T>(Geometry<T> geography) where T : IPoint, new()
    {
        switch (geography.Type)
        {
            case GeometryType.Polygon:
                return CalculatePolygonAreaOnEllipsoid(geography);

            case GeometryType.MultiPolygon:
                if (geography.Geometries == null || geography.Geometries.Count == 0) return 0;
                double sum = 0;
                for (int i = 0; i < geography.Geometries.Count; i++)
                {
                    sum += CalculatePolygonAreaOnEllipsoid(geography.Geometries[i]);
                }
                return sum;

            default:
                return 0;
        }
    }

    private static double CalculatePolygonAreaOnEllipsoid<TPoint>(Geometry<TPoint> polygon)
        where TPoint : IPoint, new()
    {
        if (polygon?.Geometries == null || polygon.Geometries.Count == 0)
            return 0;

        // WGS-84 params
        const double a = 6378137.0;               // semi-major axis (m)
        const double f = 1.0 / 298.257223563;     // flattening
        double Rq = AuthalicRadius(a, f);         // authalic radius (m)

        // Outer ring (index 0) MINUS holes (index >= 1)
        double outer = RingAreaOnAuthalicSphere(polygon.Geometries[0].Points, Rq);
        double holes = 0;

        for (int i = 1; i < polygon.Geometries.Count; i++)
        {
            holes += RingAreaOnAuthalicSphere(polygon.Geometries[i].Points, Rq);
        }

        double area = outer - holes;

        // Guard against tiny negative due to FP error
        return area < 0 && Math.Abs(area) < 1e-6 ? 0 : area;
    }

    /// <summary>
    /// Area of a single closed ring on the authalic sphere (m²).
    /// Points are (lon, lat) in degrees, first point is not repeated at end.
    /// Uses the trapezoidal/spherical excess form:
    ///   A = R² * ½ * Σ (Δλ_wrapped) * (sin φ_i + sin φ_{i+1})
    /// with longitude wrapping to handle antimeridian-crossing rings.
    /// </summary>
    private static double RingAreaOnAuthalicSphere<TPoint>(List<TPoint> ring, double R)
        where TPoint : IPoint, new()
    {
        if (ring == null || ring.Count < 3) return 0;

        // Accumulate signed spherical area (on unit sphere); scale by R² at the end
        double sum = 0;

        // Work in radians
        int n = ring.Count;
        for (int i = 0; i < n; i++)
        {
            var p1 = ring[i];
            var p2 = ring[(i + 1) % n];

            double φ1 = DegToRad(p1.Y);
            double φ2 = DegToRad(p2.Y);
            double λ1 = DegToRad(p1.X);
            double λ2 = DegToRad(p2.X);

            double dλ = WrapDeltaLon(λ2 - λ1); // wrap into [-π, π]

            // Trapezoidal integration on the sphere
            sum += dλ * (Math.Sin(φ1) + Math.Sin(φ2));
        }

        // area on unit sphere
        double A_unit = 0.5 * Math.Abs(sum);

        // Clamp to at most full sphere (numerical robustness)
        double fullSphere = 4.0 * Math.PI;
        if (A_unit > fullSphere) A_unit = fullSphere;

        // Prefer the smaller of A and (4π - A) for a single ring
        if (A_unit > 2.0 * Math.PI)
            A_unit = fullSphere - A_unit;

        return A_unit * R * R;
    }

    /// <summary>
    /// Authalic (equal-area) radius for oblate spheroid defined by (a, f).
    /// </summary>
    private static double AuthalicRadius(double a, double f)
    {
        double b = a * (1.0 - f);
        double e2 = 1.0 - (b * b) / (a * a);
        if (e2 <= 0) return a;

        double e = Math.Sqrt(e2);

        // Ellipsoid surface area:
        // S = 2π a² (1 + (1 - e²)/e * atanh(e))
        double S = 2.0 * Math.PI * a * a * (1.0 + ((1.0 - e2) / e) * Atanh(e));

        // Rq = sqrt(S / (4π))
        return Math.Sqrt(S / (4.0 * Math.PI));
    }

    private static double WrapDeltaLon(double dλ)
    {
        const double twoPi = 2.0 * Math.PI;
        // Wrap into [-π, π]
        if (dλ > Math.PI)
        {
            dλ -= twoPi;
        }
        else if (dλ < -Math.PI)
        {
            dλ += twoPi;
        }
        return dλ;
    }

    #endregion

    #region Karney’s algorithm

    /// <summary>
    /// Calculates polygon or multipolygon area on WGS84 ellipsoid in m².
    /// Uses ellipsoidal formula (Karney's algorithm for geodesic polygons).
    /// </summary>
    public static double CalculateEllipsoidalArea<T>(Geometry<T> geography) where T : IPoint, new()
    {
        switch (geography.Type)
        {
            case GeometryType.Polygon:
                return CalculateEllipsoidPolygonArea(geography);

            case GeometryType.MultiPolygon:
                return geography.Geometries.Sum(g => CalculateEllipsoidPolygonArea(g));

            default:
                return 0;
        }
    }

    private static double CalculateEllipsoidPolygonArea<TPoint>(Geometry<TPoint> polygon)
        where TPoint : IPoint, new()
    {
        if (polygon?.Geometries == null || polygon.Geometries.Count == 0)
            return 0;

        const double a = 6378137.0;             // semi-major axis
        const double f = 1.0 / 298.257223563;   // flattening
        double b = a * (1 - f);

        double totalArea = 0;

        for (int ringIndex = 0; ringIndex < polygon.Geometries.Count; ringIndex++)
        {
            var ring = polygon.Geometries[ringIndex];
            if (ring.Points == null || ring.Points.Count < 3)
                continue;

            double area = ComputeRingAreaOnEllipsoid(ring.Points, a, f, b);

            if (ringIndex == 0)
                totalArea += area;  // outer ring
            else
                totalArea -= area;  // holes
        }

        return Math.Abs(totalArea);
    }

    /// <summary>
    /// Compute ring area using Karney’s algorithm for geodesic polygons on an ellipsoid.
    /// </summary>
    private static double ComputeRingAreaOnEllipsoid<TPoint>(List<TPoint> points, double a, double f, double b)
        where TPoint : IPoint
    {
        // Use GeographicLib-style series for area accumulation
        double area = 0.0;
        int n = points.Count;

        // Convert to radians
        List<(double lat, double lon)> radPoints = new List<(double lat, double lon)>(n);
        for (int i = 0; i < n; i++)
        {
            radPoints.Add((DegToRad(points[i].Y), DegToRad(points[i].X)));
        }

        for (int i = 0; i < n; i++)
        {
            var (lat1, lon1) = radPoints[i];
            var (lat2, lon2) = radPoints[(i + 1) % n];

            double segArea = ComputeSegmentArea(lat1, lon1, lat2, lon2, a, f, b);
            area += segArea;
        }

        return Math.Abs(area);
    }

    /// <summary>
    /// Compute area contribution of a single geodesic segment using ellipsoidal corrections.
    /// </summary>
    private static double ComputeSegmentArea(double lat1, double lon1, double lat2, double lon2, double a, double f, double b)
    {
        // Vincenty formula for azimuth and length
        double L = lon2 - lon1;
        double U1 = Math.Atan((1 - f) * Math.Tan(lat1));
        double U2 = Math.Atan((1 - f) * Math.Tan(lat2));
        double sinU1 = Math.Sin(U1), cosU1 = Math.Cos(U1);
        double sinU2 = Math.Sin(U2), cosU2 = Math.Cos(U2);

        double λ = L, λPrev;
        double sinλ, cosλ;
        double sinσ, cosσ, σ;
        double sinα, cos2α, cos2σm;
        double C;

        const double epsilon = 1e-12;
        int iterations = 100;
        do
        {
            sinλ = Math.Sin(λ);
            cosλ = Math.Cos(λ);

            sinσ = Math.Sqrt((cosU2 * sinλ) * (cosU2 * sinλ) +
                             (cosU1 * sinU2 - sinU1 * cosU2 * cosλ) *
                             (cosU1 * sinU2 - sinU1 * cosU2 * cosλ));
            if (sinσ == 0)
                return 0; // coincident points

            cosσ = sinU1 * sinU2 + cosU1 * cosU2 * cosλ;
            σ = Math.Atan2(sinσ, cosσ);

            sinα = cosU1 * cosU2 * sinλ / sinσ;
            cos2α = 1 - sinα * sinα;

            cos2σm = cosσ - 2 * sinU1 * sinU2 / cos2α;
            if (double.IsNaN(cos2σm)) cos2σm = 0;

            C = f / 16 * cos2α * (4 + f * (4 - 3 * cos2α));

            λPrev = λ;
            λ = L + (1 - C) * f * sinα *
                (σ + C * sinσ * (cos2σm + C * cosσ * (-1 + 2 * cos2σm * cos2σm)));
        }
        while (Math.Abs(λ - λPrev) > epsilon && --iterations > 0);

        double uSquared = cos2α * (a * a - b * b) / (b * b);
        double A = 1 + uSquared / 16384 * (4096 + uSquared * (-768 + uSquared * (320 - 175 * uSquared)));
        double B = uSquared / 1024 * (256 + uSquared * (-128 + uSquared * (74 - 47 * uSquared)));
        double Δσ = B * sinσ *
                    (cos2σm + B / 4 * (cosσ * (-1 + 2 * cos2σm * cos2σm) -
                    B / 6 * cos2σm * (-3 + 4 * sinσ * sinσ) * (-3 + 4 * cos2σm * cos2σm)));

        double length = b * A * (σ - Δσ);

        // For ellipsoidal area: use spherical excess approximation
        double sphericalExcess = L * (Math.Sin(lat1) + Math.Sin(lat2)) / 2;
        return sphericalExcess * a * a; // approximate
    }

    #endregion


    private static double DegToRad(double deg) => deg * Math.PI / 180.0;

    // atanh(x) = 0.5 * ln((1+x)/(1-x))
    private static double Atanh(double x) => 0.5 * Math.Log((1.0 + x) / (1.0 - x));

    #endregion


    #region True Ground Length

    // https://medium.com/swlh/calculating-the-distance-between-two-points-on-earth-bac5cd50c840
    // https://www.movable-type.co.uk/scripts/latlong.html
    // https://stormconsultancy.co.uk/blog/storm-news/the-haversine-formula-in-c-and-sql/
    // https://social.msdn.microsoft.com/Forums/sqlserver/en-US/6a0cc084-5056-4f97-9978-a5f88cb57d0f/stdistance-vs-doing-the-math-manually?forum=sqlspatial
    // https://stackoverflow.com/questions/42237521/sql-server-geography-stdistance-function-is-returning-big-difference-than-other
    // https://stackoverflow.com/questions/27708490/haversine-formula-definition-for-sql
    // https://medium.com/swlh/calculating-the-distance-between-two-points-on-earth-bac5cd50c840
    public static double SphericalDistance(IPoint firstPoint, IPoint secondPoint)
    {
        //var radius = 6371008.8; // in meters

        //var radius = 6368045.28;
        //var radius = 6367538.5803727582

        var radius = (Ellipsoids.WGS84.SemiMajorAxis.Value + Ellipsoids.WGS84.SemiMinorAxis.Value) / 2.0;

        //            Haversine
        //formula: 	a = sin²(Δφ / 2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ / 2)
        //c = 2 ⋅ atan2( √a, √(1−a) )
        //d = R ⋅ c
        var phi1 = firstPoint.Y * Math.PI / 180.0;

        var phi2 = secondPoint.Y * Math.PI / 180.0;

        var a = Ellipsoids.WGS84.SemiMajorAxis.Value;
        var b = Ellipsoids.WGS84.SemiMinorAxis.Value;
        var meanPhi = (phi1 + phi2) / 2.0;
        var newR = Math.Sqrt(a * a * Math.Cos(meanPhi) * Math.Cos(meanPhi) + b * b * Math.Sin(meanPhi) * Math.Sin(meanPhi));

        var deltaPhi = (secondPoint.Y - firstPoint.Y) * Math.PI / 180.0;

        var deltaLambda = (secondPoint.X - firstPoint.X) * Math.PI / 180.0;

        //var temp = radius * Math.Acos(Math.Cos(phi1) * Math.Cos(phi2) * Math.Cos(deltaLambda) + Math.Sin(phi1) * Math.Sin(phi2)); //72092.799646276282

        var haversine = Math.Sin(deltaPhi / 2.0) * Math.Sin(deltaPhi / 2.0) +
                        Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(deltaLambda / 2.0) * Math.Sin(deltaLambda / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));

        //var c2 = 2.0 * Math.Asin(Math.Min(1, Math.Sqrt(haversine)));
        //var t3 = radius * c2;

        return newR * c; // in meters
    }


    public static double VincentyDistance(IPoint firstPoint, IPoint secondPoint)
    {
        // WGS-84 ellipsoid parameters
        const double a = 6378137.0;            // semi-major axis in meters
        const double f = 1 / 298.257223563;    // flattening
        const double b = (1 - f) * a;          // semi-minor axis

        double φ1 = firstPoint.Y * Math.PI / 180.0;
        double φ2 = secondPoint.Y * Math.PI / 180.0;
        double L = (secondPoint.X - firstPoint.X) * Math.PI / 180.0;

        double U1 = Math.Atan((1 - f) * Math.Tan(φ1));
        double U2 = Math.Atan((1 - f) * Math.Tan(φ2));

        double sinU1 = Math.Sin(U1), cosU1 = Math.Cos(U1);
        double sinU2 = Math.Sin(U2), cosU2 = Math.Cos(U2);

        double λ = L, λPrev;
        double sinλ, cosλ;
        double sinσ, cosσ, σ;
        double sinα, cos2α, cos2σm;
        double C;

        const double epsilon = 1e-12;
        int iterations = 100;
        do
        {
            sinλ = Math.Sin(λ);
            cosλ = Math.Cos(λ);

            sinσ = Math.Sqrt((cosU2 * sinλ) * (cosU2 * sinλ) +
                             (cosU1 * sinU2 - sinU1 * cosU2 * cosλ) *
                             (cosU1 * sinU2 - sinU1 * cosU2 * cosλ));

            if (sinσ == 0)
                return 0; // co-incident points

            cosσ = sinU1 * sinU2 + cosU1 * cosU2 * cosλ;
            σ = Math.Atan2(sinσ, cosσ);

            sinα = (cosU1 * cosU2 * sinλ) / sinσ;
            cos2α = 1 - sinα * sinα;

            cos2σm = cosσ - (2 * sinU1 * sinU2) / cos2α;
            if (double.IsNaN(cos2σm)) cos2σm = 0; // equatorial line

            C = (f / 16) * cos2α * (4 + f * (4 - 3 * cos2α));

            λPrev = λ;
            λ = L + (1 - C) * f * sinα *
                (σ + C * sinσ * (cos2σm + C * cosσ * (-1 + 2 * cos2σm * cos2σm)));
        }
        while (Math.Abs(λ - λPrev) > epsilon && --iterations > 0);

        if (iterations == 0)
            throw new InvalidOperationException("Vincenty formula failed to converge");

        double uSquared = cos2α * (a * a - b * b) / (b * b);
        double A = 1 + (uSquared / 16384.0) *
                    (4096.0 + uSquared * (-768 + uSquared * (320 - 175 * uSquared)));
        double B = (uSquared / 1024.0) *
                    (256.0 + uSquared * (-128 + uSquared * (74 - 47 * uSquared)));
        double Δσ = B * sinσ *
                    (cos2σm + (B / 4.0) *
                    (cosσ * (-1 + 2 * cos2σm * cos2σm) -
                    (B / 6.0) * cos2σm * (-3 + 4 * sinσ * sinσ) *
                    (-3 + 4 * cos2σm * cos2σm)));

        double s = b * A * (σ - Δσ);

        return s; // in meters
    }


    #endregion


    #region Primitive Area

    //1399.06.11
    //مساحت مثلت‌های تشکیل دهنده یک خط یا 
    //حلقه توسط نقاط ورودی
    public static List<double> GetPrimitiveAreas<T>(IEnumerable<T> points, bool isRing) where T : IPoint
    {
        List<double> result = new List<double>();

        var n = points.Count();

        if (points == null || n < 3)
            return result;

        for (int i = 0; i < n - 2; i++)
        {
            result.Add(GetUnsignedTriangleArea(points.ElementAt(i), points.ElementAt(i + 1), points.ElementAt(i + 2)));
        }

        if (isRing && n > 3)
        {
            result.Add(GetUnsignedTriangleArea(points.ElementAt(n - 2), points.ElementAt(n - 1), points.ElementAt(0)));

            result.Add(GetUnsignedTriangleArea(points.ElementAt(n - 1), points.ElementAt(0), points.ElementAt(1)));
        }

        return result;
    }

    //1399.06.11
    //مساحت مثلت‌های تشکیل دهنده شکل هندسی
    public static List<double> GetPrimitiveAreas<T>(Geometry<T> geometry) where T : IPoint, new()
    {
        var result = new List<double>();

        if (geometry == null)
        {
            return result;
        }

        switch (geometry.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return result;

            case GeometryType.LineString:
                return GetPrimitiveAreas(geometry.Points, false);

            case GeometryType.Polygon:
                return geometry.Geometries.SelectMany(g => GetPrimitiveAreas(g.Points, true)).ToList();

            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
                return geometry.Geometries.SelectMany(g => GetPrimitiveAreas(g)).ToList();

            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException("SpatialUtility.cs > GetPrimitiveAreas");
        }
    }

    public static List<double> GetPrimitiveAreas<T>(List<Geometry<T>> geometries) where T : IPoint, new()
    {
        if (geometries == null)
        {
            return new List<double>();
        }

        return geometries.SelectMany(g => GetPrimitiveAreas(g)).ToList();
    }

    #endregion

    #region Measure

    public static double GetMeasure(Geometry<Point> geometry, Func<Point, Point> toWgs84Geodetic)
    {
        switch (geometry.Type)
        {
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return geometry.CalculateGroundLength(toWgs84Geodetic);

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return CalculateGroundArea(geometry.Transform(toWgs84Geodetic, SridHelper.GeodeticWGS84));

            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    public static string GetMeasureLabel(Geometry<Point> geometry, Func<Point, Point> toWgs84Geodetic)
    {
        switch (geometry.Type)
        {
            case GeometryType.LineString:
            case GeometryType.MultiLineString:
                return UnitHelper.GetLengthLabel(geometry.CalculateGroundLength(toWgs84Geodetic));

            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                var area = CalculateGroundArea(geometry.Transform(toWgs84Geodetic, SridHelper.GeodeticWGS84));
                return UnitHelper.GetAreaLabel(area);

            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.GeometryCollection:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            default:
                throw new NotImplementedException();
        }
    }

    #endregion

    #region LineSegment

    public static double CalculateLength<T>(LineSegment<T> line, Func<T, T> toGeodeticWgs84Func) where T : IPoint, new()
    {
        var start = toGeodeticWgs84Func(line.Start);

        var end = toGeodeticWgs84Func(line.End);
         
        return VincentyDistance(start, end);

        //var geodeticLine = SqlSpatialUtility.MakeGeography(new List<T>() { start, end }, false);
        //return geodeticLine.STLength().Value;
    }

    public static string GetLengthLabel<T>(LineSegment<T> line, Func<T, T> toGeodeticWgs84Func) where T : IPoint, new()
    {
        var length = CalculateLength(line, toGeodeticWgs84Func);

        return UnitHelper.GetLengthLabel(length);
    }

    #endregion

    #region Angle
    public static double GetSignedInnerAngle<T>(T firstPoint, T middlePoint, T lastPoint, AngleMode mode = AngleMode.Radian) where T : IPoint
    {
        var isClockwise = IsClockwise(new List<T> { firstPoint, middlePoint, lastPoint });

        var angle = GetInnerAngle(firstPoint, middlePoint, lastPoint, mode);

        return isClockwise ? angle : -angle;
    }

    public static double GetSignedOuterAngle<T>(T firstPoint, T middlePoint, T lastPoint, AngleMode mode = AngleMode.Radian) where T : IPoint
    {
        var isClockwise = IsClockwise(new List<T> { firstPoint, middlePoint, lastPoint });

        var angle = GetOuterAngle(firstPoint, middlePoint, lastPoint, mode);

        return isClockwise ? angle : -angle;
    }

    /// <summary>
    /// returns the angle in desired mode between 0 and 180
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetInnerAngle<T>(T firstPoint, T middlePoint, T lastPoint, AngleMode mode = AngleMode.Radian) where T : IPoint
    {
        var radianAngle = Math.Acos(GetCosineOfInnerAngle(firstPoint, middlePoint, lastPoint));

        if (mode == AngleMode.Radian)
        {
            return radianAngle;
        }
        else if (mode == AngleMode.Degree)
        {
            return UnitConversion.RadianToDegree(radianAngle);
        }
        else if (mode == AngleMode.Grade)
        {
            return UnitConversion.RadianToGrade(radianAngle);
        }

        throw new NotImplementedException("SpatialUtility > GetAngle");
    }

    /// <summary>
    /// returns the angle in desired mode between 0 and 180
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetOuterAngle<T>(T firstPoint, T middlePoint, T lastPoint, AngleMode mode = AngleMode.Radian) where T : IPoint
    {
        var radianAngle = Math.Acos(GetCosineOfOuterAngle(firstPoint, middlePoint, lastPoint));

        if (mode == AngleMode.Radian)
        {
            return radianAngle;
        }
        else if (mode == AngleMode.Degree)
        {
            return UnitConversion.RadianToDegree(radianAngle);
        }
        else if (mode == AngleMode.Grade)
        {
            return UnitConversion.RadianToGrade(radianAngle);
        }

        throw new NotImplementedException("SpatialUtility > GetAngle");
    }

    /// <summary>
    /// returns cos(theta)
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetCosineOfInnerAngle<T>(T firstPoint, T middlePoint, T lastPoint) where T : IPoint
    {
        if (firstPoint.Equals(middlePoint) || middlePoint.Equals(lastPoint))
        {
            return 1;
        }

        //cos(theta) = (A.B)/(|A|*|B|)
        var ax = lastPoint.X - middlePoint.X;
        var ay = lastPoint.Y - middlePoint.Y;

        var bx = firstPoint.X - middlePoint.X;
        var by = firstPoint.Y - middlePoint.Y;

        var dotProduct = ax * bx + ay * by;

        var result = dotProduct / (Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by)));

        // 1401.03.20
        // to prevent NaN values when calculating ACos
        if (result > 1 || result < 0)
        {
            // p1: {5714923.59170073, 4259367.97165685}
            // p2: {5714923.61396463, 4259367.3548049}
            // p3: {5714923.63622853, 4259366.73795298}
            // result: 1.0000000000000002
            result = Math.Round(result, 13);
        }

        return result;
    }


    /// <summary>
    /// returns cos(theta)
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetCosineOfOuterAngle<T>(T firstPoint, T middlePoint, T lastPoint) where T : IPoint
    {
        if (firstPoint.Equals(middlePoint) || middlePoint.Equals(lastPoint))
        {
            return 1;
        }

        //cos(theta) = (A.B)/(|A|*|B|)
        var ax = lastPoint.X - middlePoint.X;
        var ay = lastPoint.Y - middlePoint.Y;

        var bx = middlePoint.X - firstPoint.X;
        var by = middlePoint.Y - firstPoint.Y;

        var dotProduct = ax * bx + ay * by;

        var result = dotProduct / (Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by)));

        // 1401.03.20
        // to prevent NaN values when calculating ACos
        if (result > 1 || result < 0)
        {
            // p1: {5714923.59170073, 4259367.97165685}
            // p2: {5714923.61396463, 4259367.3548049}
            // p3: {5714923.63622853, 4259366.73795298}
            // result: 1.0000000000000002
            result = Math.Round(result, 13);
        }

        return result;
    }

    public static double[] GetCosineOfOuterAngle<T>(T[] points) where T : IPoint
    {
        if (points == null || points.Length == 0 || points.Length == 2)
            return null;

        double[] result = new double[points.Length - 2];

        for (int i = 0; i < points.Length - 2; i++)
        {
            result[i] = GetCosineOfOuterAngle(points[i], points[i + 1], points[i + 2]);
        }

        return result;
    }

    /// <summary>
    /// return cos(theta)^2
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="middlePoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    public static double GetSquareCosineOfAngle<T>(T firstPoint, T middlePoint, T lastPoint) where T : IPoint
    {
        if (firstPoint.Equals(middlePoint) || middlePoint.Equals(lastPoint))
        {
            return 1;
        }

        //cos(theta) = (A.B)/(|A|*|B|)
        var ax = lastPoint.X - middlePoint.X;
        var ay = lastPoint.Y - middlePoint.Y;

        var bx = middlePoint.X - firstPoint.X;
        var by = middlePoint.Y - firstPoint.Y;

        var dotProduct = ax * bx + ay * by;


        //result: cos(theta)^2
        var result = dotProduct * dotProduct / ((ax * ax + ay * ay) * (bx * bx + by * by));

        //return dotProduct < 0 ? -1 * result : result;
        return result;
    }

    #endregion


    #region Rotation

    /// <summary>
    /// Checks if sequence of points are clockwise or not
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static bool IsClockwise<T>(List<T> points) where T : IPoint
    {
        return GetSignedRingArea(points) < 0;
    }

    ///// <summary>
    ///// Checks if sequence of points are clockwise or not
    ///// </summary>
    ///// <param name="points"></param>
    ///// <returns></returns>
    //public static bool IsClockwise(IPoint[] points)
    //{
    //    int numberOfPoints = points.Length;

    //    List<double> values = new List<double>(numberOfPoints);

    //    for (int i = 0; i < numberOfPoints - 1; i++)
    //    {
    //        values.Add((points[i + 1].X - points[i].X) * (points[i + 1].Y + points[i].Y));
    //    }

    //    return values.Sum() > 0;
    //}

    #endregion


    #region Circle-Rectangle Topology

    public static bool CircleRectangleIntersects(IPoint circleCenter, double circleRadius, BoundingBox axisAlignedRectangle)
    {
        var rectangleCenter = axisAlignedRectangle.Center;

        //Circle.Center.X - Rectangle.Center.X
        var xDifference = Math.Abs(circleCenter.X - rectangleCenter.X);

        //Circle.Center.Y - Rectangle.Center.Y
        var yDifference = Math.Abs(circleCenter.Y - rectangleCenter.Y);

        var rectangleHalfWidth = axisAlignedRectangle.Width / 2.0;
        var rectangleHalfHeight = axisAlignedRectangle.Height / 2.0;

        if (xDifference > (rectangleHalfWidth + circleRadius)) { return false; }
        if (yDifference > (rectangleHalfHeight + circleRadius)) { return false; }

        if (xDifference <= rectangleHalfWidth) { return true; }
        if (yDifference <= rectangleHalfHeight) { return true; }


        var cornerDistance = (xDifference - rectangleHalfWidth) * (xDifference - rectangleHalfWidth) +
                                (yDifference - rectangleHalfHeight) * (yDifference - rectangleHalfHeight);

        return cornerDistance <= (circleRadius * circleRadius);
    }

    public static bool IsAxisAlignedRectangleInsideCircle(IPoint circleCenter, double circleRadius, BoundingBox axisAlignedRectangle)
    {
        var rectangleCenter = axisAlignedRectangle.Center;

        var xDifference = Math.Abs(circleCenter.X - rectangleCenter.X);
        var yDifference = Math.Abs(circleCenter.Y - rectangleCenter.Y);

        var rectangleHalfWidth = axisAlignedRectangle.Width / 2.0;
        var rectangleHalfHeight = axisAlignedRectangle.Height / 2.0;

        if (xDifference > (rectangleHalfWidth + circleRadius)) { return false; }
        if (yDifference > (rectangleHalfHeight + circleRadius)) { return false; }

        var cornerDistance = (xDifference + rectangleHalfWidth) * (xDifference + rectangleHalfWidth) +
                                (yDifference + rectangleHalfHeight) * (yDifference + rectangleHalfHeight);

        return cornerDistance <= (circleRadius * circleRadius);
    }

    /// <summary>
    /// returns whether the rectangle is contained by the circle or they intersects or they are disjoint
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="circleRadius"></param>
    /// <param name="axisAlignedRectangle"></param>
    /// <returns></returns>
    public static SpatialRelation CalculateAxisAlignedRectangleRelationToCircle(IPoint circleCenter, double circleRadius, BoundingBox axisAlignedRectangle)
    {
        var rectangleCenter = axisAlignedRectangle.Center;

        var xDifference = Math.Abs(circleCenter.X - rectangleCenter.X);
        var yDifference = Math.Abs(circleCenter.Y - rectangleCenter.Y);

        var rectangleHalfWidth = axisAlignedRectangle.Width / 2.0;
        var rectangleHalfHeight = axisAlignedRectangle.Height / 2.0;

        if (xDifference > (rectangleHalfWidth + circleRadius)) { return SpatialRelation.Disjoint; }
        if (yDifference > (rectangleHalfHeight + circleRadius)) { return SpatialRelation.Disjoint; }

        var semiRadius = circleRadius * circleRadius;

        if (xDifference <= rectangleHalfWidth || yDifference <= rectangleHalfHeight)
        {
            var farCornerDistance = (xDifference + rectangleHalfWidth) * (xDifference + rectangleHalfWidth) +
                                    (yDifference + rectangleHalfHeight) * (yDifference + rectangleHalfHeight);

            if (farCornerDistance <= semiRadius)
            {
                return SpatialRelation.Contained;
            }

            return SpatialRelation.Intersects;
        }

        var nearCornerDistance = (xDifference - rectangleHalfWidth) * (xDifference - rectangleHalfWidth) +
                                (yDifference - rectangleHalfHeight) * (yDifference - rectangleHalfHeight);

        if (nearCornerDistance <= semiRadius)
            return SpatialRelation.Intersects;
        else
        {
            return SpatialRelation.Disjoint;
        }
    }

    #endregion


    #region Point-Line 

    public static double GetPointToLineSegmentDistance<T>(T lineSegmentStart, T lineSegmentEnd, T targetPoint) where T : IPoint, new()
    {
        var dySegment = lineSegmentEnd.Y - lineSegmentStart.Y;

        var dxSegment = lineSegmentEnd.X - lineSegmentStart.X;

        //اگر دو نقطه پاره خط منطبق بودند
        //فاصله بین آن‌ها تا نقطه هدف در 
        //نظر گرفته می‌شود.
        if (dxSegment == 0 && dySegment == 0)
        {
            return SpatialUtility.GetEuclideanDistance(lineSegmentStart, targetPoint);
        }

        return Math.Abs(dySegment * targetPoint.X - dxSegment * targetPoint.Y + lineSegmentEnd.X * lineSegmentStart.Y - lineSegmentEnd.Y * lineSegmentStart.X)
                /
                Math.Sqrt(dySegment * dySegment + dxSegment * dxSegment);
    }

    public static double GetPointToLineSegmentSquareDistance<T>(T lineSegmentStart, T lineSegmentEnd, T targetPoint) where T : IPoint, new()
    {
        var dySegment = (lineSegmentEnd.Y - lineSegmentStart.Y);

        var dxSegment = (lineSegmentEnd.X - lineSegmentStart.X);

        //اگر دو نقطه پاره خط منطبق بودند
        //فاصله بین آن‌ها تا نقطه هدف در 
        //نظر گرفته می‌شود.
        if (dxSegment == 0 && dySegment == 0)
        {
            return SpatialUtility.GetSquareEuclideanDistance(lineSegmentStart, targetPoint);
        }

        var numerator = (dySegment * targetPoint.X - dxSegment * targetPoint.Y + lineSegmentEnd.X * lineSegmentStart.Y - lineSegmentEnd.Y * lineSegmentStart.X);

        return (numerator * numerator)
                /
                (dySegment * dySegment + dxSegment * dxSegment);
    }

    #endregion


    // McMaster, R. B. (1986). A statistical analysis of mathematical measures for linear simplification. The American Cartographer, 13(2), 103-116.
    #region Measurement of Displacement

    // todo: consider ring mode
    public static double CalculateTotalVectorDisplacement<T>(List<T> originalPoints, List<T> simplifiedPoints, bool isRingMode) where T : IPoint, new()
    {
        int currentSimplifiedIndex_Start = 0;
        int currentSimplifiedIndex_End = 1;

        double result = 0;


        // تعیین ارتباط بین اندکس نقطه در لیست 
        // اصلی و اندکس نقطه در لیست ساده شده
        //Dictionary<int, (int, int)?> indexMap = new Dictionary<int, (int, int)?>();

        for (int originalIndex = 0; originalIndex < originalPoints.Count; originalIndex++)
        {
            var currentPoint = originalPoints[originalIndex];

            //if (currentPoint.DistanceTo(simplifiedPoints[currentSimplifiedIndex_Start]) < EpsilonDistance)
            //{
            //    //indexMap.Add(originalIndex, null);
            //}
            /*else */
            if (SpatialUtility.GetEuclideanDistance(currentPoint, simplifiedPoints[currentSimplifiedIndex_End]) < EpsilonDistance)
            {
                //indexMap.Add(originalIndex, null);
                currentSimplifiedIndex_Start = currentSimplifiedIndex_End;

                if (isRingMode && currentSimplifiedIndex_End == simplifiedPoints.Count - 1)
                    currentSimplifiedIndex_End = 0;

                else
                    currentSimplifiedIndex_End++;

                continue;
            }
            //else
            //{ 
            var distance = GetPointToLineSegmentDistance(simplifiedPoints[currentSimplifiedIndex_Start], simplifiedPoints[currentSimplifiedIndex_End], currentPoint);

            result += distance;
            //}
        }

        return result;
    }


    #endregion


    public static T CalculateMidPoint<T>(T firstPoint, T secondPoint) where T : IPoint, new()
    {
        return new T() { X = (firstPoint.X + secondPoint.X) / 2, Y = (firstPoint.Y + secondPoint.Y) / 2 };
    }

    public static double CalculateSlope<T>(T firstPoint, T secondPoint) where T : IPoint, new()
    {
        return (secondPoint.Y - firstPoint.Y) / (secondPoint.X - firstPoint.X);
    }


    public static string AsPolygon(List<Point> points, Func<Point, Point> transform = null)
    {
        var stringArray = transform == null ? points.Select(i => i.AsExactString()) : points.Select(i => transform(i).AsExactString());

        return string.Format(CultureInfo.InvariantCulture, "POLYGON(({0}))", string.Join(",", stringArray));

    }

    public static string AsPolyline(List<Point> points, Func<Point, Point> transform = null)
    {
        var stringArray = transform == null ? points.Select(i => i.AsExactString()) : points.Select(i => transform(i).AsExactString());

        return string.Format(CultureInfo.InvariantCulture, "LINESTRING({0})", string.Join(",", stringArray));

    }

}

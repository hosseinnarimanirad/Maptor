using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public static class CoordinateHelper
{
    public static (string x, string y) Format(
        Point webMercator,
        CoordinateDisplayMode mode,
        bool thousandSeparator,
        int? utmZone,
        int? latLongPrecision,
        int? xyPrecision,
        Ellipsoid? ellipsoid)
    {

        // Convert Web Mercator to selected SRS 
        var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercator);
         
        int theUtmZone = utmZone ?? MapProjects.FindUtmZone(geodetic.X);

        int theLatLongPrecision = latLongPrecision ?? 5;

        int theXyPrecision = xyPrecision ?? 2;

        Ellipsoid theEllipsoid = ellipsoid ?? Ellipsoids.WGS84;
         
        try
        {
            switch (mode)
            {
                case CoordinateDisplayMode.UTM:
                    // UTM always uses WGS84 ellipsoid 
                    var utmPoint = MapProjects.GeodeticToUTM(geodetic, Ellipsoids.WGS84, theUtmZone, geodetic.Y > 0);

                    return (FormatWithPrecision(utmPoint.X, theXyPrecision, thousandSeparator), FormatWithPrecision(utmPoint.Y, theXyPrecision, thousandSeparator));

                case CoordinateDisplayMode.WebMercator:
                    return (FormatWithPrecision(webMercator.X, theXyPrecision, thousandSeparator), FormatWithPrecision(webMercator.Y, theXyPrecision, thousandSeparator));

                case CoordinateDisplayMode.GeodeticDecimal:
                case CoordinateDisplayMode.GeodeticDms:
                    // If ellipsoid is not WGS84, convert to selected ellipsoid
                    if (!theEllipsoid.AreTheSame(Ellipsoids.WGS84))
                    {
                        geodetic = Transformations.ChangeDatum(geodetic, Ellipsoids.WGS84, theEllipsoid);
                    }

                    if (mode == CoordinateDisplayMode.GeodeticDms)
                        return (DegreeHelper.ToDms(geodetic.X, true), DegreeHelper.ToDms(geodetic.Y, true));

                    else
                        return (FormatWithPrecision(geodetic.X, theLatLongPrecision, thousandSeparator), FormatWithPrecision(geodetic.Y, theLatLongPrecision, thousandSeparator));

                default:
                    return (string.Empty, string.Empty);
            }
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }


    private static string FormatWithPrecision(double value, int precision, bool thousandSeparator)
    {
        var defaultFormat = thousandSeparator ? "#,#" : "#";

        if (precision == 0)
            return value.ToString(defaultFormat);
        //return value.ToString("#,#");


        string format = $"{defaultFormat}." + new string('0', precision);
        //string format = "#,#." + new string('0', precision);

        return value.ToString(format);
    }

}

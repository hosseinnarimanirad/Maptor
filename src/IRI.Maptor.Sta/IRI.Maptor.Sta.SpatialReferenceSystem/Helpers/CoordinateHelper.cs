using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public static class CoordinateHelper
{
    public static (string x, string y) Format(
        Point webMercator,
        CoordinateDisplayMode mode,
        CopyCoordinateOptions options)
    //bool thousandSeparator,
    //int? utmZone,
    //int? latLongPrecision,
    //int? xyPrecision,
    //Ellipsoid? ellipsoid)
    {

        // Convert Web Mercator to selected SRS 
        var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercator);

        int theUtmZone = options.UtmZone ?? MapProjects.FindUtmZone(geodetic.X);

        //int theLatLongPrecision = options.LatLongPrecision;

        //int theXyPrecision = xyPrecision ?? 2;

        //Ellipsoid theEllipsoid = ellipsoid ?? Ellipsoids.WGS84;

        try
        {
            switch (mode)
            {
                case CoordinateDisplayMode.UTM:
                    // UTM always uses WGS84 ellipsoid 
                    var utmPoint = MapProjects.GeodeticToUTM(geodetic, Ellipsoids.WGS84, theUtmZone, geodetic.Y > 0);

                    return (FormatWithPrecision(utmPoint.X, options.XyPrecision, options.UseThousandSeparator), FormatWithPrecision(utmPoint.Y, options.XyPrecision, options.UseThousandSeparator));

                case CoordinateDisplayMode.WebMercator:
                    return (FormatWithPrecision(webMercator.X, options.XyPrecision, options.UseThousandSeparator), FormatWithPrecision(webMercator.Y, options.XyPrecision, options.UseThousandSeparator));

                case CoordinateDisplayMode.GeodeticDecimal:
                case CoordinateDisplayMode.GeodeticDms:
                    // If ellipsoid is not WGS84, convert to selected ellipsoid
                    if (!options.Ellipsoid.AreTheSame(Ellipsoids.WGS84))
                    {
                        geodetic = Transformations.ChangeDatum(geodetic, Ellipsoids.WGS84, options.Ellipsoid);
                    }

                    if (mode == CoordinateDisplayMode.GeodeticDms)
                        return (DegreeHelper.ToDms(geodetic.X, true), DegreeHelper.ToDms(geodetic.Y, true));

                    else
                        return (FormatWithPrecision(geodetic.X, options.LatLongPrecision, options.UseThousandSeparator), FormatWithPrecision(geodetic.Y, options.LatLongPrecision, options.UseThousandSeparator));

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
            return value.ToString(defaultFormat, System.Globalization.CultureInfo.InvariantCulture);
        //return value.ToString("#,#");

        string format = $"{defaultFormat}." + new string('0', precision);
        //string format = "#,#." + new string('0', precision);

        return value.ToString(format);
    }

}

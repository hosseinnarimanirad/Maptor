using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class CoordinateDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return string.Empty;

        if (values[0] is not Locateable locateable)
            return string.Empty;

        if (values[1] is not CoordinateDisplayMode srsType)
            return string.Empty;


        // Convert Web Mercator to selected SRS
        var webMercatorPoint = new Point(locateable.X, locateable.Y);
        //var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);


        int? utmZone = (values.Length > 2 && values[2] is int zone) ? zone : null;

        int? latLongPrecision = (values.Length > 2 && values[3] is int llPrec) ? llPrec : null;

        int? xyPrecision = (values.Length > 2 && values[4] is int xyPrec) ? xyPrec : null;

        Ellipsoid? ellipsoid = (values.Length > 2 && values[5] is Ellipsoid e) ? e : null;

        bool isX = parameter?.ToString()?.ToUpper() == "X";

        //double coordinateValue;

        var format = CoordinateHelper.Format(webMercatorPoint, srsType, utmZone, latLongPrecision, xyPrecision, ellipsoid);

        return isX ? format.x : format.y;

        //try
        //{
        //    switch (srsType)
        //    {
        //        case CoordinateDisplayMode.UTM:
        //            // UTM always uses WGS84 ellipsoid 
        //            var utmPoint = MapProjects.GeodeticToUTM(geodetic, Ellipsoids.WGS84, utmZone, geodetic.Y > 0);
        //            coordinateValue = isX ? utmPoint.X : utmPoint.Y;
        //            return FormatWithPrecision(coordinateValue, xyPrecision);

        //        case CoordinateDisplayMode.WebMercator:
        //            coordinateValue = isX ? webMercatorPoint.X : webMercatorPoint.Y;
        //            return FormatWithPrecision(coordinateValue, xyPrecision);

        //        case CoordinateDisplayMode.GeodeticDecimal:
        //        case CoordinateDisplayMode.GeodeticDms:
        //            // If ellipsoid is not WGS84, convert to selected ellipsoid
        //            if (!ellipsoid.AreTheSame(Ellipsoids.WGS84))
        //            {
        //                var convertedGeodetic = Transformations.ChangeDatum(geodetic, Ellipsoids.WGS84, ellipsoid);
        //                coordinateValue = isX ? convertedGeodetic.X : convertedGeodetic.Y;
        //            }
        //            else
        //            {
        //                coordinateValue = isX ? geodetic.X : geodetic.Y;
        //            }

        //            if (srsType == CoordinateDisplayMode.GeodeticDms)
        //                return DegreeHelper.ToDms(coordinateValue, true);

        //            else
        //                return FormatWithPrecision(coordinateValue, latLongPrecision);

        //        default:
        //            return string.Empty;
        //    }
        //}
        //catch
        //{
        //    return string.Empty;
        //}
    }

    private string FormatWithPrecision(double value, int precision)
    {
        if (precision == 0)
            return value.ToString("#,#");

        string format = "#,#." + new string('0', precision);
        return value.ToString(format);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


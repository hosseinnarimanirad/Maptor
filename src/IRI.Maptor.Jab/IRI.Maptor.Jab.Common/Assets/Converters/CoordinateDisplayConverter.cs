using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class CoordinateDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 6)
            return string.Empty;

        // values[0] = Locateable
        // values[1] = SelectedSrsType (CoordinateEditorSrsType)
        // values[2] = SelectedEllipsoid (Ellipsoid)
        // values[3] = UtmZone (int)
        // values[4] = LatLongPrecision (int)
        // values[5] = XYPrecision (int)
        // parameter = "X" or "Y" to indicate which coordinate

        if (values[0] is not Locateable locateable)
            return string.Empty;

        if (values[1] is not CoordinateDisplayMode srsType)
            return string.Empty;

        if (values[2] is not Ellipsoid ellipsoid)
            return string.Empty;

        int utmZone = values[3] is int zone ? zone : 39;
        int latLongPrecision = values[4] is int llPrec ? llPrec : 5;
        int xyPrecision = values[5] is int xyPrec ? xyPrec : 2;

        bool isX = parameter?.ToString()?.ToUpper() == "X";

        // Convert Web Mercator to selected SRS
        var webMercatorPoint = new Point(locateable.X, locateable.Y);
        double coordinateValue;

        try
        {
            switch (srsType)
            {
                case CoordinateDisplayMode.UTM:
                    // UTM always uses WGS84 ellipsoid
                    var geodeticFromWebMercator = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
                    var utmPoint = MapProjects.GeodeticToUTM(geodeticFromWebMercator, Ellipsoids.WGS84, utmZone, geodeticFromWebMercator.Y > 0);
                    coordinateValue = isX ? utmPoint.X : utmPoint.Y;
                    return FormatWithPrecision(coordinateValue, xyPrecision);

                case CoordinateDisplayMode.WebMercator:
                    coordinateValue = isX ? webMercatorPoint.X : webMercatorPoint.Y;
                    return FormatWithPrecision(coordinateValue, xyPrecision);

                case CoordinateDisplayMode.GeodeticDecimal:
                    var geodetic = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
                    // If ellipsoid is not WGS84, convert to selected ellipsoid
                    if (!ellipsoid.AreTheSame(Ellipsoids.WGS84))
                    {
                        var convertedGeodetic = Transformations.ChangeDatum(geodetic, Ellipsoids.WGS84, ellipsoid);
                        coordinateValue = isX ? convertedGeodetic.X : convertedGeodetic.Y;
                    }
                    else
                    {
                        coordinateValue = isX ? geodetic.X : geodetic.Y;
                    }
                    return FormatWithPrecision(coordinateValue, latLongPrecision);

                case CoordinateDisplayMode.GeodeticDms:
                    var geodeticDms = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);
                    // If ellipsoid is not WGS84, convert to selected ellipsoid
                    if (!ellipsoid.AreTheSame(Ellipsoids.WGS84))
                    {
                        var convertedGeodeticDms = Transformations.ChangeDatum(geodeticDms, Ellipsoids.WGS84, ellipsoid);
                        coordinateValue = isX ? convertedGeodeticDms.X : convertedGeodeticDms.Y;
                    }
                    else
                    {
                        coordinateValue = isX ? geodeticDms.X : geodeticDms.Y;
                    }
                    return DegreeHelper.ToDms(coordinateValue, true);

                default:
                    return string.Empty;
            }
        }
        catch
        {
            return string.Empty;
        }
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


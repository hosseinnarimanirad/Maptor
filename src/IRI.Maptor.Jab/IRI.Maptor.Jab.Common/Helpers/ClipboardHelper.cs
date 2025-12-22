using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Jab.Common.Helpers;

public static class ClipboardHelper
{

    public static void CopyToClipboard(Point webMercator, CoordinateDisplayMode mode, int? utmZone, int? latLongPrecision, int? xyPrecision, Ellipsoid? ellipsoid)
    {
        var format = CoordinateHelper.Format(webMercator,
                                                mode,
                                                thousandSeparator: false,
                                                utmZone: utmZone,
                                                latLongPrecision: latLongPrecision,
                                                xyPrecision: xyPrecision,
                                                ellipsoid: ellipsoid);

        if (mode == CoordinateDisplayMode.GeodeticDms || mode == CoordinateDisplayMode.GeodeticDecimal)
        {
            System.Windows.Clipboard.SetDataObject($"{format.y},{format.x}");
        }
        else
        {
            System.Windows.Clipboard.SetDataObject($"{format.x},{format.y}");
        }
    }
}

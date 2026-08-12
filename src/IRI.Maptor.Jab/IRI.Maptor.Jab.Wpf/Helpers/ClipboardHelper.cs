using IRI.Maptor.Jab.Wpf.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Wpf.Helpers;

public static class ClipboardHelper
{

    public static void CopyToClipboard(Point webMercator, CoordinateDisplayMode mode, CopyCoordinateOptions options, bool? isLatitudeFirst = null)
    {
        var format = CoordinateHelper.Format(webMercator, mode, options); 

        if ((isLatitudeFirst ?? true) && (mode == CoordinateDisplayMode.GeodeticDms || mode == CoordinateDisplayMode.GeodeticDecimal))
        {
            System.Windows.Clipboard.SetDataObject($"{format.y},{format.x}");
        }
        else
        {
            System.Windows.Clipboard.SetDataObject($"{format.x},{format.y}");
        }
    }

    public static void CopyText(string text) => System.Windows.Clipboard.SetText(text);
}

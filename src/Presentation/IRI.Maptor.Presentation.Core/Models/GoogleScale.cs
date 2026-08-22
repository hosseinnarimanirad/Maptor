using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Spatial.Helpers;

namespace IRI.Maptor.Presentation.Core.Models;

public class GoogleScale : ScaleModel
{
    private static readonly List<GoogleScale> _scales;
    public static List<GoogleScale> GoogleScales => _scales;

    private int _zoomLevel;
    public int ZoomLevel
    {
        get { return _zoomLevel; }
        set
        {
            _zoomLevel = value;
            RaisePropertyChanged();
        }
    }


    public override string ToString() => $"{ZoomLevel:D2} - {InverseScale:N1}";

    static GoogleScale()
    {
        _scales = WebMercatorUtility.ZoomLevels.Select(i => new GoogleScale(i.ZoomLevel, i.InverseScale)).ToList();
    }

    public GoogleScale(int zoomLevel, double inverseScale) : base(inverseScale)
    {
        ZoomLevel = zoomLevel;
    }

    public static GoogleScale GetNearestScale(double mapScale, double latitude = 35)
    {
        var zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale, latitude);

        return GoogleScales.Single(i => i.ZoomLevel == zoomLevel);
    }
}

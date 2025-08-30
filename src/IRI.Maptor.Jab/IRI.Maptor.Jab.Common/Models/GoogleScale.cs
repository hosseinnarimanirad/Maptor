using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.Helpers;

namespace IRI.Maptor.Jab.Common.Models;

public class GoogleScale : Notifier
{
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


    private double _inverseScale;

    public double InverseScale
    {
        get { return _inverseScale; }
        set
        {
            _inverseScale = value;
            RaisePropertyChanged();
        }
    }

    public double Scale
    {
        get { return 1.0 / InverseScale; }
    }

    public GoogleScale(int zoomLevel, double inverseScale)
    {
        this.ZoomLevel = zoomLevel;

        this.InverseScale = inverseScale;
    }

    public override string ToString()
    {
        return $"{ZoomLevel:D2} - {InverseScale:N1}";
    }

    static GoogleScale()
    {
        _scales = WebMercatorUtility.ZoomLevels.Select(i => new GoogleScale(i.ZoomLevel, i.InverseScale)).ToList();
    }

    private static List<GoogleScale> _scales;

    public static List<GoogleScale> Scales
    {
        get { return _scales; }
    }

    public static GoogleScale GetNearestScale(double mapScale, double latitude = 35)
    {
        var zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale, latitude);

        return Scales.Single(i => i.ZoomLevel == zoomLevel);
    }
}

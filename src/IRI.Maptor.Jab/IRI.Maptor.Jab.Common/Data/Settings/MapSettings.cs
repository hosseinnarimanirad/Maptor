using IRI.Maptor.Jab.Common.Data.Settings;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Data;

public class MapSettings : IMapSettings
{
    public BoundingBox? InitialExtent { get; set; }
    public bool IsDoubleClickZoomEnabled { get; set; } = true;
    public bool IsGoogleZoomLevelsEnabled { get; set; }
    public bool IsMouseWheelZoomEnabled { get; set; } = true;
    public int MaxGoogleZoomLevel { get; set; } = 20;
    public int MinGoogleZoomLevel { get; set; } = 1;

    public bool AllowLargeDataLoading { get; set; } = false;

    public bool Clipboard_IsLatitudeFirst { get; set; } = true;
    public int Clipboard_XyPrecision { get; set; } = 2;
    public int Clipboard_LatLongPrecision { get; set; } = 5;


    public bool ShowTileBorder { get; set; } = true;

    public bool Identify_IncludeNotInScaleRangeLayers { get; set; } = true;

    public bool Identify_IncludeInvisibleLayers { get; set; } = true;

    public int Identify_SelectionTolerance { get; set; } = 7;


    public static MapSettings Default = new MapSettings();
}

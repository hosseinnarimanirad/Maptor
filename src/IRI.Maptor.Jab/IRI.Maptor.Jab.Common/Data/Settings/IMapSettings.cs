using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Data.Settings;

public interface IMapSettings
{
    BoundingBox? InitialExtent { get; set; }
    bool IsDoubleClickZoomEnabled { get; set; }
    bool IsGoogleZoomLevelsEnabled { get; set; }
    bool IsMouseWheelZoomEnabled { get; set; }
    int MaxGoogleZoomLevel { get; set; }
    int MinGoogleZoomLevel { get; set; }

    bool AllowLargeDataLoading { get; set; }

    bool Clipboard_IsLatitudeFirst { get; set; }
    int Clipboard_XyPrecision { get; set; }
    int Clipboard_LatLongPrecision { get; set; }

    bool ShowTileBorder { get; set; }

    bool Identify_IncludeNotInScaleRangeLayers { get; set; }
    bool Identify_IncludeInvisibleLayers { get; set; }
    int Identify_SelectionTolerance { get; set; }
}
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Core.Data;

public interface IMapSettings
{
    BoundingBox? InitialExtent { get; set; }
    bool IsDoubleClickZoomEnabled { get; set; }
    bool IsGoogleZoomLevelsEnabled { get; set; }
    bool IsMouseWheelZoomEnabled { get; set; }
    int MaxGoogleZoomLevel { get; set; }
    int MinGoogleZoomLevel { get; set; }

    /// <summary>
    /// How many zoom steps (mouse wheel notches, zoom in/out clicks) span one google zoom level.
    /// 1 snaps every step to a google zoom level; higher values insert evenly spaced mid levels.
    /// Range 1..8.
    /// </summary>
    int ZoomStepsPerGoogleLevel { get; set; }

    /// <summary>
    /// Highest google zoom level tiles are requested at. Zooming past it keeps requesting this
    /// level and lets the tiles scale up, rather than asking the provider for a level it has no
    /// imagery for, which would blank the base map. Range 1..24.
    /// </summary>
    int MaxTileZoomLevel { get; set; }

    bool AllowLargeDataLoading { get; set; }

    bool Clipboard_IsLatitudeFirst { get; set; }
    int Clipboard_XyPrecision { get; set; }
    int Clipboard_LatLongPrecision { get; set; }

    bool ShowTileBorder { get; set; }

    bool Identify_IncludeNotInScaleRangeLayers { get; set; }
    bool Identify_IncludeInvisibleLayers { get; set; }
    int Identify_SelectionTolerance { get; set; }
}
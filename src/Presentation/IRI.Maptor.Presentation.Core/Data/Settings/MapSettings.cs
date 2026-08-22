using System.Text.Json.Serialization;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Core.Data;

public class MapSettings : IMapSettings
{
    public const int DefaultZoomStepsPerGoogleLevel = 2;

    public BoundingBox? InitialExtent { get; set; }
    public bool IsDoubleClickZoomEnabled { get; set; } = true;
    public bool IsMouseWheelZoomEnabled { get; set; } = true;
    public int MaxGoogleZoomLevel { get; set; } = 20;
    public int MinGoogleZoomLevel { get; set; } = 1;

    public int ZoomStepsPerGoogleLevel { get; set; } = DefaultZoomStepsPerGoogleLevel;

    // google road/hybrid generally serve level 22 where they have coverage; satellite often stops
    // at 21. Apps whose MaxGoogleZoomLevel is 20 or lower never reach this, so it is a no-op there.
    public int MaxTileZoomLevel { get; set; } = 22;

    /// <summary>
    /// Legacy view over <see cref="ZoomStepsPerGoogleLevel"/>; prefer the step count itself.
    /// Not serialized, so it can never be merged back over the step count it derives from.
    /// </summary>
    [JsonIgnore]
    public bool IsGoogleZoomLevelsEnabled
    {
        get => ZoomStepsPerGoogleLevel <= 1;
        set => ZoomStepsPerGoogleLevel = value ? 1 : DefaultZoomStepsPerGoogleLevel;
    }

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

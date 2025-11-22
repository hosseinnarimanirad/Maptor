using System.Windows.Input;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls.Common;

/// <summary>
/// Configuration class for common MapViewer settings used during initialization.
/// Provides default values matching common usage patterns across projects.
/// </summary>
public class MapViewerConfiguration
{
    /// <summary>
    /// Gets or sets whether pan mode should be enabled. Default: true
    /// </summary>
    public bool EnablePan { get; set; } = true;

    /// <summary>
    /// Gets or sets whether mouse wheel zooming should be enabled. Default: true
    /// </summary>
    public bool EnableMouseWheelZoom { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Google zoom levels should be enabled. Default: true
    /// </summary>
    public bool EnableGoogleZoomLevels { get; set; } = true;

    /// <summary>
    /// Gets or sets the initial cursor for the map viewer. Default: null (uses default cursor)
    /// </summary>
    public Cursor? InitialCursor { get; set; } = null;

    /// <summary>
    /// Gets or sets the initial extent to zoom to. Default: null (no initial zoom)
    /// </summary>
    public BoundingBox? InitialExtent { get; set; } = null;

    /// <summary>
    /// Gets or sets the minimum Google zoom level for the presenter. Default: 2
    /// </summary>
    public int MinGoogleZoomLevel { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum Google zoom level for the presenter. Default: 18
    /// </summary>
    public int MaxGoogleZoomLevel { get; set; } = 18;

    /// <summary>
    /// Creates a default configuration with common settings enabled.
    /// </summary>
    public static MapViewerConfiguration Default => new MapViewerConfiguration();

    /// <summary>
    /// Creates a minimal configuration with only essential settings enabled.
    /// </summary>
    public static MapViewerConfiguration Minimal => new MapViewerConfiguration
    {
        EnablePan = true,
        EnableMouseWheelZoom = false,
        EnableGoogleZoomLevels = false
    };
}


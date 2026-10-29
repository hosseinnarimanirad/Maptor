using StaPoint = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Controls;

/// <summary>Where a single tap on the <see cref="MapViewer"/> landed.</summary>
public sealed class MapTappedEventArgs : EventArgs
{
    public MapTappedEventArgs(Microsoft.Maui.Graphics.Point screen, StaPoint webMercator, double latitude, double longitude, bool hasPosition)
    {
        Screen = screen;
        WebMercator = webMercator;
        Latitude = latitude;
        Longitude = longitude;
        HasPosition = hasPosition;
    }

    /// <summary>Point of the control, device-independent pixels; pass it to <see cref="MapViewer.HitTest"/>.</summary>
    public Microsoft.Maui.Graphics.Point Screen { get; }

    /// <summary>The tapped location in WebMercator.</summary>
    public StaPoint WebMercator { get; }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>
    /// False when the platform gave no touch position; the location is then the centre of the map
    /// and should not be used to identify.
    /// </summary>
    public bool HasPosition { get; }
}

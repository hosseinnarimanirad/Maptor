using IRI.Maptor.Jab.Maui.Mvvm;
using IRI.Maptor.Sta.Common.Primitives;

using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Jab.Maui.Layers;

/// <summary>
/// A lightweight vector layer (typically loaded from GeoJSON) shown on the
/// <see cref="Controls.MapViewer"/>. Geometry is pre-projected to WebMercator and stored
/// in <see cref="Parts"/>. Changing <see cref="IsVisible"/> or <see cref="Color"/> raises
/// change notifications so the map redraws and the legend updates.
/// </summary>
public sealed class MapLayer : ObservableBase
{
    private string _name;
    private bool _isVisible = true;
    private Color _color;
    private double _strokeWidth = 2;
    private double _pointSize = 10;

    public MapLayer(string name, Color color)
    {
        _name = name;
        _color = color;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>Stroke color; the polygon fill is derived as this color with reduced alpha.</summary>
    public Color Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public double StrokeWidth
    {
        get => _strokeWidth;
        set => SetProperty(ref _strokeWidth, value);
    }

    public double PointSize
    {
        get => _pointSize;
        set => SetProperty(ref _pointSize, value);
    }

    /// <summary>Projected (WebMercator) geometry to draw.</summary>
    internal IReadOnlyList<RenderPart> Parts { get; init; } = Array.Empty<RenderPart>();

    /// <summary>Bounds of the layer in WebMercator, used for zoom-to-layer. Null if empty.</summary>
    public BoundingBox? Extent { get; init; }
}

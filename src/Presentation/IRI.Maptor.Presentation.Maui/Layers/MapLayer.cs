using IRI.Maptor.Presentation.Maui.Mvvm;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

using Microsoft.Maui.Graphics;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>How a <see cref="MapLayer"/> was created — used to build its description.</summary>
public enum LayerSource
{
    GeoJson,
    Drawn,
}

/// <summary>The symbol a point layer draws for each point.</summary>
public enum PointShape
{
    Circle,
    Square,
    Triangle,
    Diamond,
}

/// <summary>
/// A lightweight vector layer (typically loaded from GeoJSON) shown on the
/// <see cref="Controls.MapViewer"/>. Geometry is pre-projected to WebMercator and stored
/// in <see cref="Parts"/>; the features behind it (ids, attributes) in <see cref="Features"/>. Changing <see cref="IsVisible"/> or <see cref="Color"/> raises
/// change notifications so the map redraws and the legend updates.
/// </summary>
public class MapLayer : ObservableBase
{
    private string _name;
    private string _description = string.Empty;
    private bool _isVisible = true;
    private double _opacity = 1.0;
    private Color _color;
    private double _strokeWidth = 2;
    private double _pointSize = 10;
    private Color? _fillColor;
    private Color _outlineColor = Colors.White;
    private double _outlineWidth = 1;
    private PointShape _pointShape = PointShape.Circle;
    private float[]? _dashPattern;
    private double? _minZoom;
    private double? _maxZoom;
    private IReadOnlyList<MapFeature> _features = Array.Empty<MapFeature>();

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

    /// <summary>
    /// Auto-generated, human-readable layer kind, e.g. "Point (GeoJson)", "Polygon (Drawn)".
    /// Built from the geometry type and <see cref="LayerSource"/> at creation time.
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Layer opacity in [0, 1] (1 = fully opaque). Applied when the layer is drawn so an overlay
    /// (e.g. a raster MBTiles) can be faded to reveal the basemap underneath.
    /// </summary>
    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, Math.Clamp(value, 0.0, 1.0));
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

    /// <summary>
    /// Fill of polygons and point symbols. Null keeps the defaults: polygons use <see cref="Color"/>
    /// at 30% alpha, point symbols use <see cref="Color"/>.
    /// </summary>
    public Color? FillColor
    {
        get => _fillColor;
        set => SetProperty(ref _fillColor, value);
    }

    /// <summary>Outline drawn around point symbols (white by default, so points stand out on any basemap).</summary>
    public Color OutlineColor
    {
        get => _outlineColor;
        set => SetProperty(ref _outlineColor, value);
    }

    public double OutlineWidth
    {
        get => _outlineWidth;
        set => SetProperty(ref _outlineWidth, value);
    }

    public PointShape PointShape
    {
        get => _pointShape;
        set => SetProperty(ref _pointShape, value);
    }

    /// <summary>Dash pattern of lines and polygon outlines, in multiples of the stroke width; null draws solid.</summary>
    public float[]? DashPattern
    {
        get => _dashPattern;
        set => SetProperty(ref _dashPattern, value);
    }

    /// <summary>Smallest map zoom level (fractional) at which the layer is drawn; null for no limit.</summary>
    public double? MinZoom
    {
        get => _minZoom;
        set => SetProperty(ref _minZoom, value);
    }

    /// <summary>Largest map zoom level (fractional) at which the layer is drawn; null for no limit.</summary>
    public double? MaxZoom
    {
        get => _maxZoom;
        set => SetProperty(ref _maxZoom, value);
    }

    /// <summary>True when <see cref="IsVisible"/> and <paramref name="zoom"/> is within the zoom range.</summary>
    public bool IsVisibleAt(double zoom)
        => IsVisible && (MinZoom is not double min || zoom >= min) && (MaxZoom is not double max || zoom <= max);

    /// <summary>
    /// The features of the layer, in the order they were given. Replaced as a whole by
    /// <see cref="SetFeatures"/>; raises a change notification so the map redraws.
    /// </summary>
    public IReadOnlyList<MapFeature> Features
    {
        get => _features;
        private set => SetProperty(ref _features, value);
    }

    /// <summary>
    /// Replaces the layer's content with <paramref name="features"/> (geometry in WebMercator,
    /// or in another system when <paramref name="toWebMercator"/> converts it). Ids and attributes
    /// are kept for hit-testing. Safe to call from a background thread: the new content is swapped
    /// in at the end. <see cref="Extent"/> is updated to the new features.
    /// </summary>
    public void SetFeatures(IEnumerable<Feature<Point>> features, Func<Point, (double X, double Y)>? toWebMercator = null)
    {
        var toWorld = toWebMercator ?? (p => (p.X, p.Y));
        var parts = new List<RenderPart>();
        var mapFeatures = new List<MapFeature>();

        double xMin = double.MaxValue, yMin = double.MaxValue, xMax = double.MinValue, yMax = double.MinValue;

        void Track(double x, double y)
        {
            if (x < xMin) xMin = x;
            if (y < yMin) yMin = y;
            if (x > xMax) xMax = x;
            if (y > yMax) yMax = y;
        }

        foreach (var feature in features)
        {
            var mapFeature = MapFeature.From(feature);
            mapFeatures.Add(mapFeature);

            if (feature.TheGeometry is { } geometry)
            {
                RenderPartBuilder.Build(geometry, toWorld, parts, Track, mapFeature);
            }
        }

        Extent = xMax >= xMin && yMax >= yMin ? new BoundingBox(xMin, yMin, xMax, yMax) : null;
        Parts = parts;
        Features = mapFeatures;
    }

    /// <summary>
    /// Projected (WebMercator) geometry to draw. Settable so dynamic layers (e.g. vector MBTiles)
    /// can refresh their geometry as the view changes.
    /// </summary>
    internal IReadOnlyList<RenderPart> Parts { get; set; } = Array.Empty<RenderPart>();

    /// <summary>
    /// Used by layers that fill <see cref="Parts"/> themselves (GeoJSON, vector MBTiles) to publish the
    /// features those parts belong to, without raising a change notification.
    /// </summary>
    internal void SetFeaturesSilently(IReadOnlyList<MapFeature> features) => _features = features;

    /// <summary>Bounds of the layer in WebMercator, used for zoom-to-layer. Null if empty.</summary>
    public BoundingBox? Extent { get; set; }

    /// <summary>
    /// The original GeoJSON the layer was built from, if any. Stored so a project can be
    /// saved to disk and the layer rebuilt later. Null for layers with no serializable source.
    /// </summary>
    public string? SourceGeoJson { get; init; }
}

using IRI.Maptor.Presentation.Maui.Mvvm;

using Microsoft.Maui.Graphics;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>What a <see cref="MapGraphic"/> draws from its vertices.</summary>
public enum MapGraphicKind
{
    /// <summary>Each vertex as a dot.</summary>
    Points,

    /// <summary>The vertices joined in order.</summary>
    Polyline,

    /// <summary>The vertices as one closed ring (the closing vertex is implied, never repeated).</summary>
    Polygon,
}

/// <summary>
/// A temporary shape drawn above every layer of the <see cref="Controls.MapViewer"/>, in
/// <see cref="Controls.MapViewer.Overlays"/>: a measure sketch, the highlight of an identified
/// feature, and so on. Vertices are WebMercator. Any change raises a notification so the map redraws.
/// Not hit-tested.
/// </summary>
public class MapGraphic : ObservableBase
{
    private readonly List<Point> _vertices = new();

    // Set by FromFeature: the feature's own parts are drawn instead of the vertices.
    private readonly IReadOnlyList<RenderPart>? _featureParts;

    private IReadOnlyList<RenderPart> _parts = Array.Empty<RenderPart>();

    private MapGraphicKind _kind;
    private Color _strokeColor = Color.FromArgb("#FFE53935");
    private Color? _fillColor;
    private double _strokeWidth = 3;
    private float[]? _dashPattern;
    private bool _showVertices;
    private double _vertexSize = 10;
    private bool _isVisible = true;

    public MapGraphic(MapGraphicKind kind)
    {
        _kind = kind;
    }

    private MapGraphic(IReadOnlyList<RenderPart> featureParts)
    {
        _featureParts = featureParts;
        _parts = featureParts;
    }

    /// <summary>
    /// A graphic that draws the geometry of <paramref name="feature"/> (a hit-test result) in the
    /// graphic's style, e.g. to highlight it. Its <see cref="Vertices"/> stay empty.
    /// </summary>
    public static MapGraphic FromFeature(MapFeature feature) => new(feature.Parts.ToArray());

    public MapGraphicKind Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                RebuildParts();
            }
        }
    }

    /// <summary>The vertices in WebMercator, in order.</summary>
    public IReadOnlyList<Point> Vertices => _vertices;

    public void AddVertex(Point webMercator)
    {
        _vertices.Add(webMercator);
        RebuildParts();
    }

    /// <summary>Removes the last vertex; false when there is none.</summary>
    public bool RemoveLastVertex()
    {
        if (_vertices.Count == 0)
        {
            return false;
        }

        _vertices.RemoveAt(_vertices.Count - 1);
        RebuildParts();
        return true;
    }

    public void SetVertices(IEnumerable<Point> webMercatorVertices)
    {
        _vertices.Clear();
        _vertices.AddRange(webMercatorVertices);
        RebuildParts();
    }

    public void ClearVertices()
    {
        _vertices.Clear();
        RebuildParts();
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public Color StrokeColor
    {
        get => _strokeColor;
        set => SetProperty(ref _strokeColor, value);
    }

    /// <summary>Fill of polygons and of points/vertex dots; null fills polygons with the stroke at 25% alpha.</summary>
    public Color? FillColor
    {
        get => _fillColor;
        set => SetProperty(ref _fillColor, value);
    }

    public double StrokeWidth
    {
        get => _strokeWidth;
        set => SetProperty(ref _strokeWidth, value);
    }

    /// <summary>Dash pattern in multiples of the stroke width; null draws solid.</summary>
    public float[]? DashPattern
    {
        get => _dashPattern;
        set => SetProperty(ref _dashPattern, value);
    }

    /// <summary>Draws a dot on every vertex of a polyline or polygon (e.g. while sketching).</summary>
    public bool ShowVertices
    {
        get => _showVertices;
        set => SetProperty(ref _showVertices, value);
    }

    /// <summary>Diameter of point dots and vertex dots, in device-independent pixels.</summary>
    public double VertexSize
    {
        get => _vertexSize;
        set => SetProperty(ref _vertexSize, value);
    }

    // What the drawable draws. Replaced as a whole so drawing never sees a half-built list.
    internal IReadOnlyList<RenderPart> Parts => _parts;

    private void RebuildParts()
    {
        if (_featureParts is not null)
        {
            return;
        }

        if (_vertices.Count == 0)
        {
            _parts = Array.Empty<RenderPart>();
        }
        else
        {
            var xs = _vertices.Select(v => v.X).ToArray();
            var ys = _vertices.Select(v => v.Y).ToArray();

            var kind = _kind switch
            {
                MapGraphicKind.Points => RenderKind.Point,
                // A ring needs three vertices; with fewer, draw what there is as a line.
                MapGraphicKind.Polygon when _vertices.Count >= 3 => RenderKind.Polygon,
                _ => RenderKind.Line,
            };

            _parts = new[] { RenderPartBuilder.FromWorldRings(kind, new List<(double[] X, double[] Y)> { (xs, ys) }) };
        }

        OnPropertyChanged(nameof(Vertices));
    }
}

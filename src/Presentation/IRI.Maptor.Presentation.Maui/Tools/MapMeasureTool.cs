using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Maui.Layers;
using IRI.Maptor.Presentation.Maui.Mvvm;

using Microsoft.Maui.Graphics;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Tools;

/// <summary>What a <see cref="MapMeasureTool"/> measures.</summary>
public enum MeasureMode
{
    /// <summary>The length of the line through the vertices.</summary>
    Length,

    /// <summary>The area (and perimeter) of the ring through the vertices.</summary>
    Area,
}

/// <summary>
/// Measures on the map: the host adds a vertex per tap (WebMercator, e.g. <see cref="Controls.MapTappedEventArgs.WebMercator"/>)
/// and shows <see cref="Sketch"/> in <see cref="Controls.MapViewer.Overlays"/>. Lengths and areas are geodesic, on the
/// WGS84 ellipsoid (<see cref="SpatialUtility.GetEllipsoidMeasure"/>), not in the WebMercator plane, whose scale grows
/// away from the equator (about 1.25× at Tehran's latitude). UI-free apart from the sketch's colours.
/// </summary>
public sealed class MapMeasureTool : ObservableBase
{
    private static readonly Func<Point, Point> ToWgs84 = MapProjects.WebMercatorToGeodeticWgs84;

    private MeasureMode _mode;
    private double _lengthMeters;
    private double _areaSquareMeters;

    public MapMeasureTool(MeasureMode mode = MeasureMode.Length)
    {
        _mode = mode;

        Sketch = new MapGraphic(KindOf(mode))
        {
            StrokeColor = Color.FromArgb("#FFE53935"),
            StrokeWidth = 3,
            ShowVertices = true,
            VertexSize = 12,
        };
    }

    /// <summary>The line or ring being measured, with a dot on every vertex.</summary>
    public MapGraphic Sketch { get; }

    public MeasureMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                Sketch.Kind = KindOf(value);
                Recalculate();
            }
        }
    }

    /// <summary>The vertices in WebMercator, in the order added.</summary>
    public IReadOnlyList<Point> Vertices => Sketch.Vertices;

    public int VertexCount => Sketch.Vertices.Count;

    /// <summary>
    /// Metres along the line; in <see cref="MeasureMode.Area"/>, the perimeter of the closed ring (from three vertices,
    /// before that the open line). 0 with fewer than two vertices.
    /// </summary>
    public double LengthMeters
    {
        get => _lengthMeters;
        private set => SetProperty(ref _lengthMeters, value);
    }

    /// <summary>Square metres inside the ring in <see cref="MeasureMode.Area"/> with three vertices or more; else 0.</summary>
    public double AreaSquareMeters
    {
        get => _areaSquareMeters;
        private set => SetProperty(ref _areaSquareMeters, value);
    }

    public void AddVertex(Point webMercator)
    {
        Sketch.AddVertex(webMercator);
        Recalculate();
    }

    /// <summary>Removes the last vertex; false when there is none.</summary>
    public bool Undo()
    {
        if (!Sketch.RemoveLastVertex())
        {
            return false;
        }

        Recalculate();
        return true;
    }

    public void Clear()
    {
        Sketch.ClearVertices();
        Recalculate();
    }

    private void Recalculate()
    {
        var vertices = Sketch.Vertices.ToList();

        double length = 0, area = 0;

        if (vertices.Count >= 2)
        {
            bool isRing = _mode == MeasureMode.Area && vertices.Count >= 3;

            // The perimeter is the line back to the first vertex. (A polygon ring itself never repeats it.)
            var line = new List<Point>(vertices);

            if (isRing)
            {
                line.Add(vertices[0]);
            }

            length = SpatialUtility.GetEllipsoidMeasure(Geometry<Point>.Create(line, GeometryType.LineString, SridHelper.WebMercator), ToWgs84);

            if (isRing)
            {
                // CreatePolygon may reverse the list it is given (outer rings are counter-clockwise): pass a copy.
                area = Math.Abs(SpatialUtility.GetEllipsoidMeasure(Geometry<Point>.CreatePolygon(new List<Point>(vertices), SridHelper.WebMercator), ToWgs84));
            }
        }

        LengthMeters = length;
        AreaSquareMeters = area;
        OnPropertyChanged(nameof(VertexCount));
        OnPropertyChanged(nameof(Vertices));
    }

    private static MapGraphicKind KindOf(MeasureMode mode) => mode == MeasureMode.Area ? MapGraphicKind.Polygon : MapGraphicKind.Polyline;
}

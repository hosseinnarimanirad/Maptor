using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// Shows where the pointer snaps to: a square on a vertex, a circle on an edge. It lives on an overlay canvas above
/// the map, takes no mouse input and draws in screen coordinates. It does not decide where the point goes.
/// </summary>
public sealed class SnapMarker
{
    private const double Size = 12;

    private readonly Canvas _overlay;
    private readonly Shape _vertex;
    private readonly Shape _edge;

    public SnapMarker(Canvas overlay)
    {
        _overlay = overlay;

        _vertex = Create(new Rectangle());
        _edge = Create(new Ellipse());

        _overlay.Children.Add(_vertex);
        _overlay.Children.Add(_edge);
    }

    public void Show(Point screenPoint, SnapKind kind)
    {
        var shown = kind == SnapKind.Vertex ? _vertex : _edge;
        var hidden = kind == SnapKind.Vertex ? _edge : _vertex;

        hidden.Visibility = Visibility.Collapsed;

        Canvas.SetLeft(shown, screenPoint.X - Size / 2);
        Canvas.SetTop(shown, screenPoint.Y - Size / 2);

        shown.Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        _vertex.Visibility = Visibility.Collapsed;
        _edge.Visibility = Visibility.Collapsed;
    }

    private static Shape Create(Shape shape)
    {
        shape.Width = Size;
        shape.Height = Size;
        shape.StrokeThickness = 2;
        shape.IsHitTestVisible = false;
        shape.Visibility = Visibility.Collapsed;

        // the same pair as the text-on-map handles: a light fill and the theme's highlight, readable on any base map
        shape.SetResourceReference(Shape.FillProperty, "IRI.Maptor.Brushes.OnMap.HandleFill");
        shape.SetResourceReference(Shape.StrokeProperty, "MahApps.Brushes.Highlight");

        return shape;
    }
}

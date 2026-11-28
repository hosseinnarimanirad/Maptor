using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// The edge handle of a geometry being edited (decision D1, look A, 2026-11-28): a small white diamond with an accent
/// line, on the edge under the pointer; the cursor's plus says that a press there adds a vertex. It lives on the
/// overlay canvas, takes no mouse input and only shows where it is told; on the middle of an edge the map shows the
/// snap marker's triangle instead.
/// </summary>
public sealed class EdgeHandleMarker
{
    // a 7 px square turned by 45 degrees: about 10 px from corner to corner
    private const double Size = 10;

    private const string AccentBrush = "MahApps.Brushes.Highlight";
    private const string FillBrush = "IRI.Maptor.Brushes.OnMap.HandleFill";

    private readonly Path _handle;

    public EdgeHandleMarker(Canvas overlay)
    {
        _handle = new Path
        {
            Data = Geometry.Parse("M 5,0 L 10,5 L 5,10 L 0,5 Z"),
            Width = Size,
            Height = Size,
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Miter,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };

        _handle.SetResourceReference(Shape.FillProperty, FillBrush);
        _handle.SetResourceReference(Shape.StrokeProperty, AccentBrush);

        overlay.Children.Add(_handle);
    }

    public bool IsShown => _handle.Visibility == Visibility.Visible;

    /// <param name="screenPoint">The point of the edge, on the overlay.</param>
    public void Show(Point screenPoint)
    {
        Canvas.SetLeft(_handle, screenPoint.X - Size / 2);
        Canvas.SetTop(_handle, screenPoint.Y - Size / 2);

        _handle.Visibility = Visibility.Visible;
    }

    public void Hide() => _handle.Visibility = Visibility.Collapsed;
}

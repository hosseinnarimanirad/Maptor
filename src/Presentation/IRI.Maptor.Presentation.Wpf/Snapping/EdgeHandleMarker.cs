using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// The edge handle of a geometry being edited (decision D1, B's edge handle): a white circle with an accent line and
/// a plus, on the edge under the pointer, saying that a press there adds a vertex. It lives on the overlay canvas,
/// takes no mouse input and only shows where it is told; on the middle of an edge the map shows the snap marker's
/// triangle instead.
/// </summary>
public sealed class EdgeHandleMarker
{
    private const double Size = 13;

    private const string AccentBrush = "MahApps.Brushes.Highlight";
    private const string FillBrush = "IRI.Maptor.Brushes.OnMap.HandleFill";

    private readonly Grid _handle;

    public EdgeHandleMarker(Canvas overlay)
    {
        var circle = new Ellipse { Width = Size, Height = Size, StrokeThickness = 1.5 };
        circle.SetResourceReference(Shape.FillProperty, FillBrush);
        circle.SetResourceReference(Shape.StrokeProperty, AccentBrush);

        var plus = new Path
        {
            Data = Geometry.Parse("M 3.5,6.5 H 9.5 M 6.5,3.5 V 9.5"),
            StrokeThickness = 1.5,
            SnapsToDevicePixels = true,
        };
        plus.SetResourceReference(Shape.StrokeProperty, AccentBrush);

        _handle = new Grid
        {
            Width = Size,
            Height = Size,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };

        _handle.Children.Add(circle);
        _handle.Children.Add(plus);

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

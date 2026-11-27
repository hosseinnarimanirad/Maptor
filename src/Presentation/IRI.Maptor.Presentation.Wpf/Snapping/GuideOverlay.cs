using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// Draws the guide the point is on: a dashed line across the map and a small diamond where the point sits on it;
/// and, apart from that, a highlight over the existing edge that guides are drawn from.
/// It lives on the overlay canvas above the map, takes no mouse input and draws in screen coordinates. It does not
/// decide which guide is active or where the point goes.
/// </summary>
public sealed class GuideOverlay
{
    private const double MarkSize = 9;

    // the snap marker's pair: a light underlay so the dashes show on dark and on light maps
    private const string LightBrush = "IRI.Maptor.Brushes.OnMap.HandleFill";
    private const string StrongBrush = "MahApps.Brushes.Highlight";

    private readonly Line _halo;
    private readonly Line _line;
    private readonly Polygon _mark;
    private readonly Line _referenceHalo;
    private readonly Line _reference;

    public GuideOverlay(Canvas overlay)
    {
        _halo = new Line { StrokeThickness = 3, Opacity = .6 };
        _halo.SetResourceReference(Shape.StrokeProperty, LightBrush);

        _line = new Line { StrokeThickness = 1.3, StrokeDashArray = new DoubleCollection { 6, 4 } };
        _line.SetResourceReference(Shape.StrokeProperty, StrongBrush);

        var half = MarkSize / 2;

        _mark = new Polygon
        {
            Points = new PointCollection { new Point(half, 0), new Point(MarkSize, half), new Point(half, MarkSize), new Point(0, half) },
            Width = MarkSize,
            Height = MarkSize,
            StrokeThickness = 1.5,
        };
        _mark.SetResourceReference(Shape.FillProperty, LightBrush);
        _mark.SetResourceReference(Shape.StrokeProperty, StrongBrush);

        _referenceHalo = new Line { StrokeThickness = 7, Opacity = .7, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        _referenceHalo.SetResourceReference(Shape.StrokeProperty, LightBrush);

        _reference = new Line { StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        _reference.SetResourceReference(Shape.StrokeProperty, StrongBrush);

        foreach (var element in new FrameworkElement[] { _referenceHalo, _reference, _halo, _line, _mark })
        {
            element.IsHitTestVisible = false;
            element.Visibility = Visibility.Collapsed;

            overlay.Children.Add(element);
        }
    }

    /// <param name="from">One end of the guide on the screen (a ray's origin, or a line's far end).</param>
    /// <param name="to">The other end, beyond the map's edge.</param>
    /// <param name="point">Where the point sits on the guide; the diamond goes here when <paramref name="showMark"/> is set.</param>
    /// <param name="showMark">False when another marker already shows the point (a crossing has the snap marker).</param>
    public void Show(Point from, Point to, Point point, bool showMark)
    {
        foreach (var line in new[] { _halo, _line })
        {
            line.X1 = from.X;
            line.Y1 = from.Y;
            line.X2 = to.X;
            line.Y2 = to.Y;

            line.Visibility = Visibility.Visible;
        }

        if (showMark)
        {
            Canvas.SetLeft(_mark, point.X - MarkSize / 2);
            Canvas.SetTop(_mark, point.Y - MarkSize / 2);
        }

        _mark.Visibility = showMark ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Hide()
    {
        _halo.Visibility = Visibility.Collapsed;
        _line.Visibility = Visibility.Collapsed;
        _mark.Visibility = Visibility.Collapsed;
    }

    /// <summary>Highlights the existing edge guides are drawn from; it stays until <see cref="HideReference"/>.</summary>
    public void ShowReference(Point from, Point to)
    {
        foreach (var line in new[] { _referenceHalo, _reference })
        {
            line.X1 = from.X;
            line.Y1 = from.Y;
            line.X2 = to.X;
            line.Y2 = to.Y;

            line.Visibility = Visibility.Visible;
        }
    }

    public void HideReference()
    {
        _referenceHalo.Visibility = Visibility.Collapsed;
        _reference.Visibility = Visibility.Collapsed;
    }

    /// <summary>True while the reference edge is highlighted; for tests.</summary>
    public bool IsReferenceShown => _reference.Visibility == Visibility.Visible;

    /// <summary>True while a guide is drawn; for tests.</summary>
    public bool IsShown => _line.Visibility == Visibility.Visible;

    /// <summary>True while the diamond is drawn; for tests.</summary>
    public bool IsMarkShown => _mark.Visibility == Visibility.Visible;

    /// <summary>The drawn guide's two ends on the screen; for tests.</summary>
    public (Point From, Point To) Ends => (new Point(_line.X1, _line.Y1), new Point(_line.X2, _line.Y2));
}

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// Shows where the pointer snaps to, with one shape per kind and a small label beside it:
/// a filled square on a line end, an open square on a vertex, an X on an intersection, a triangle on a midpoint and a
/// circle on an edge. It lives on an overlay canvas above the map, takes no mouse input and draws in screen
/// coordinates. It does not decide where the point goes, nor what the label says.
/// </summary>
public sealed class SnapMarker
{
    private const double Size = 12;

    // from the marker's centre to the label's near corner
    private const double LabelOffset = 10;

    // the same pair as the text-on-map handles: a light fill and the theme's highlight, readable on any base map
    private const string LightBrush = "IRI.Maptor.Brushes.OnMap.HandleFill";
    private const string StrongBrush = "MahApps.Brushes.Highlight";

    private readonly Canvas _overlay;
    private readonly Dictionary<SnapKind, FrameworkElement> _shapes;
    private readonly Border _label;
    private readonly TextBlock _labelText;

    public SnapMarker(Canvas overlay)
    {
        _overlay = overlay;

        _shapes = new Dictionary<SnapKind, FrameworkElement>
        {
            [SnapKind.End] = Outlined(new Rectangle(), fill: StrongBrush, stroke: LightBrush, thickness: 1.5),
            [SnapKind.Vertex] = Outlined(new Rectangle(), fill: LightBrush, stroke: StrongBrush, thickness: 2),
            [SnapKind.Intersection] = Cross(),
            [SnapKind.Midpoint] = Outlined(Triangle(), fill: LightBrush, stroke: StrongBrush, thickness: 2),
            [SnapKind.Edge] = Outlined(new Ellipse(), fill: LightBrush, stroke: StrongBrush, thickness: 2),
        };

        foreach (var shape in _shapes.Values)
        {
            shape.Width = Size;
            shape.Height = Size;
            shape.IsHitTestVisible = false;
            shape.Visibility = Visibility.Collapsed;

            _overlay.Children.Add(shape);
        }

        _labelText = new TextBlock { FontSize = 11 };
        _labelText.SetResourceReference(TextBlock.ForegroundProperty, "IRI.Maptor.Brushes.OnMap.Text");
        _labelText.SetResourceReference(TextBlock.FontFamilyProperty, "iranSans");

        _label = new Border
        {
            Child = _labelText,
            Padding = new Thickness(4, 1, 4, 1),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        _label.SetResourceReference(Border.BackgroundProperty, "IRI.Maptor.Brushes.OnMap.Surface");
        _label.SetResourceReference(Border.BorderBrushProperty, "IRI.Maptor.Brushes.OnMap.Border");

        _overlay.Children.Add(_label);
    }

    /// <param name="screenPoint">The snap target on the overlay.</param>
    /// <param name="kind">Picks the shape.</param>
    /// <param name="label">Shown beside the marker; none when null or empty.</param>
    /// <param name="labelDirection">The reading direction of <paramref name="label"/>.</param>
    public void Show(Point screenPoint, SnapKind kind, string? label = null, FlowDirection labelDirection = FlowDirection.LeftToRight)
    {
        foreach (var pair in _shapes)
        {
            if (pair.Key != kind)
                pair.Value.Visibility = Visibility.Collapsed;
        }

        if (_shapes.TryGetValue(kind, out var shown))
        {
            Canvas.SetLeft(shown, screenPoint.X - Size / 2);
            Canvas.SetTop(shown, screenPoint.Y - Size / 2);

            shown.Visibility = Visibility.Visible;
        }

        if (string.IsNullOrEmpty(label))
        {
            _label.Visibility = Visibility.Collapsed;
            return;
        }

        _labelText.Text = label;
        _label.FlowDirection = labelDirection;
        _label.Visibility = Visibility.Visible;

        _label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var size = _label.DesiredSize;

        // to the lower right of the marker; flipped to the other side where it would leave the map
        var left = screenPoint.X + LabelOffset;
        var top = screenPoint.Y + LabelOffset;

        if (_overlay.ActualWidth > 0 && left + size.Width > _overlay.ActualWidth)
            left = screenPoint.X - LabelOffset - size.Width;

        if (_overlay.ActualHeight > 0 && top + size.Height > _overlay.ActualHeight)
            top = screenPoint.Y - LabelOffset - size.Height;

        Canvas.SetLeft(_label, left);
        Canvas.SetTop(_label, top);
    }

    public void Hide()
    {
        foreach (var shape in _shapes.Values)
            shape.Visibility = Visibility.Collapsed;

        _label.Visibility = Visibility.Collapsed;
    }

    /// <summary>The shape shown for <paramref name="kind"/>, for tests.</summary>
    public FrameworkElement? ShapeOf(SnapKind kind) => _shapes.TryGetValue(kind, out var shape) ? shape : null;

    /// <summary>The label's text while it is shown, else null; for tests.</summary>
    public string? VisibleLabel => _label.Visibility == Visibility.Visible ? _labelText.Text : null;

    private static Shape Outlined(Shape shape, string fill, string stroke, double thickness)
    {
        shape.StrokeThickness = thickness;
        shape.SetResourceReference(Shape.FillProperty, fill);
        shape.SetResourceReference(Shape.StrokeProperty, stroke);

        return shape;
    }

    private static Shape Triangle() => new Polygon
    {
        Points = new PointCollection { new Point(Size / 2, 1), new Point(Size - 1, Size - 1), new Point(1, Size - 1) },
        StrokeLineJoin = PenLineJoin.Round,
    };

    // an X with a light halo under it, so it shows on dark and on light maps
    private static FrameworkElement Cross()
    {
        var canvas = new Canvas();

        canvas.Children.Add(CrossPath(LightBrush, 5));
        canvas.Children.Add(CrossPath(StrongBrush, 2.5));

        return canvas;
    }

    private static Path CrossPath(string brush, double thickness)
    {
        var path = new Path
        {
            Data = Geometry.Parse($"M 1,1 L {Size - 1},{Size - 1} M {Size - 1},1 L 1,{Size - 1}"),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false,
        };

        path.SetResourceReference(Shape.StrokeProperty, brush);

        return path;
    }
}

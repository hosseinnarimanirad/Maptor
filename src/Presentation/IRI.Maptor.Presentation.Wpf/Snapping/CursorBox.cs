using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>How a value of the cursor box is shown.</summary>
public enum CursorBoxState
{
    /// <summary>Follows the pointer.</summary>
    Live = 0,

    /// <summary>Typed and in force.</summary>
    Fixed = 1,

    /// <summary>Being typed.</summary>
    Editing = 2,
}

/// <summary>One value of the cursor box: its text and how it is shown.</summary>
public readonly record struct CursorBoxValue(string Text, CursorBoxState State = CursorBoxState.Live);

/// <summary>
/// The small box beside the point being placed: the length of the segment, its turn from the previous segment and
/// its bearing, each after the key that starts typing it (L, A, B), under an optional heading (what the point
/// snapped to or the guide it is on). It lives on the overlay canvas above the map, takes no mouse input and draws
/// in screen coordinates. It only shows what it is given: it does not measure, read keys or decide the point.
/// </summary>
public sealed class CursorBox
{
    // from the point to the box's near corner
    private const double Offset = 12;

    private const string AccentBrush = "MahApps.Brushes.Highlight";
    private const string TextBrush = "IRI.Maptor.Brushes.OnMap.Text";

    // the key letters and the numbers are Latin in every language: a fixed Latin font, not the UI font (which
    // would draw the digits in the language's own shapes)
    private static readonly FontFamily LatinFont = new("Segoe UI");

    private readonly Canvas _overlay;
    private readonly Border _box;
    private readonly TextBlock _header;
    private readonly Row _length;
    private readonly Row _angle;
    private readonly Row _bearing;

    public CursorBox(Canvas overlay)
    {
        _overlay = overlay;

        _header = new TextBlock { FontSize = 11, Margin = new Thickness(0, 0, 0, 2) };
        _header.SetResourceReference(TextBlock.ForegroundProperty, TextBrush);
        _header.SetResourceReference(TextBlock.FontFamilyProperty, "iranSans");

        _length = new Row("L", fontSize: 12);
        _angle = new Row("A", fontSize: 12);
        _bearing = new Row("B", fontSize: 12);

        var rows = new StackPanel();

        rows.Children.Add(_header);
        rows.Children.Add(_length.Element);
        rows.Children.Add(_angle.Element);
        rows.Children.Add(_bearing.Element);

        _box = new Border
        {
            Child = rows,
            Padding = new Thickness(5, 2, 5, 3),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        _box.SetResourceReference(Border.BackgroundProperty, "IRI.Maptor.Brushes.OnMap.Surface");
        _box.SetResourceReference(Border.BorderBrushProperty, "IRI.Maptor.Brushes.OnMap.Border");

        _overlay.Children.Add(_box);
    }

    /// <param name="screenPoint">The point being placed, on the overlay.</param>
    /// <param name="header">What the point snapped to or the guide it is on; none when null or empty.</param>
    /// <param name="length">The segment's length.</param>
    /// <param name="angle">The turn from the previous segment; null hides the row (the first segment of a part).</param>
    /// <param name="bearing">The direction from north; null hides the row (a segment of no length).</param>
    /// <param name="headerDirection">The reading direction of <paramref name="header"/>; the values are always left to right.</param>
    public void Show(Point screenPoint, string? header, CursorBoxValue length, CursorBoxValue? angle, CursorBoxValue? bearing,
                     FlowDirection headerDirection = FlowDirection.LeftToRight)
    {
        _header.Text = header ?? string.Empty;
        _header.FlowDirection = headerDirection;
        _header.Visibility = string.IsNullOrEmpty(header) ? Visibility.Collapsed : Visibility.Visible;

        _length.Set(length);
        _angle.Set(angle);
        _bearing.Set(bearing);

        _box.Visibility = Visibility.Visible;

        _box.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var size = _box.DesiredSize;

        // to the lower right of the point; flipped to the other side where it would leave the map
        var left = screenPoint.X + Offset;
        var top = screenPoint.Y + Offset;

        if (_overlay.ActualWidth > 0 && left + size.Width > _overlay.ActualWidth)
            left = screenPoint.X - Offset - size.Width;

        if (_overlay.ActualHeight > 0 && top + size.Height > _overlay.ActualHeight)
            top = screenPoint.Y - Offset - size.Height;

        Canvas.SetLeft(_box, left);
        Canvas.SetTop(_box, top);
    }

    public void Hide() => _box.Visibility = Visibility.Collapsed;

    #region For tests

    public bool IsShown => _box.Visibility == Visibility.Visible;

    public string? VisibleHeader => IsShown && _header.Visibility == Visibility.Visible ? _header.Text : null;

    public string? VisibleLength => IsShown ? _length.VisibleText : null;

    public string? VisibleAngle => IsShown ? _angle.VisibleText : null;

    public string? VisibleBearing => IsShown ? _bearing.VisibleText : null;

    /// <summary>The top-left corner of the box on the overlay.</summary>
    public Point Location => new(Canvas.GetLeft(_box), Canvas.GetTop(_box));

    #endregion

    /// <summary>One line: the key that starts typing the value, then the value.</summary>
    private sealed class Row
    {
        private readonly TextBlock _key;
        private readonly TextBlock _value;
        private readonly Border _valueFrame;

        public Row(string key, double fontSize)
        {
            _key = new TextBlock { Text = key, FontSize = fontSize - 1.5, Opacity = .65, Width = 13, VerticalAlignment = VerticalAlignment.Center, FontFamily = LatinFont };
            _key.SetResourceReference(TextBlock.ForegroundProperty, TextBrush);

            _value = new TextBlock { FontSize = fontSize, FontFamily = LatinFont };

            _valueFrame = new Border { Child = _value, Padding = new Thickness(2, 0, 2, 0), CornerRadius = new CornerRadius(2), BorderThickness = new Thickness(1) };

            var panel = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };

            panel.Children.Add(_key);
            panel.Children.Add(_valueFrame);

            Element = panel;
        }

        public FrameworkElement Element { get; }

        public string? VisibleText => Element.Visibility == Visibility.Visible ? _value.Text : null;

        public void Set(CursorBoxValue? value)
        {
            if (value is not { } shown)
            {
                Element.Visibility = Visibility.Collapsed;
                return;
            }

            Element.Visibility = Visibility.Visible;

            // a field being typed looks like a text box with a caret; a typed value in force is bold in the accent colour
            _value.Text = shown.State == CursorBoxState.Editing ? shown.Text + "▏" : shown.Text;
            _value.FontWeight = shown.State == CursorBoxState.Live ? FontWeights.Normal : FontWeights.Bold;
            _value.SetResourceReference(TextBlock.ForegroundProperty, shown.State == CursorBoxState.Live ? TextBrush : AccentBrush);

            if (shown.State == CursorBoxState.Editing)
                _valueFrame.SetResourceReference(Border.BorderBrushProperty, AccentBrush);
            else
                _valueFrame.BorderBrush = Brushes.Transparent;
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>
/// A short note beside the pointer that goes away by itself, for a moment's answer to what the user just tried: why
/// a sketch cannot be finished yet (V9 of the drawing and editing plan). It lives on the overlay canvas and takes no
/// mouse input.
/// </summary>
public sealed class PointerHint
{
    private static readonly TimeSpan ShownFor = TimeSpan.FromSeconds(2.5);

    // from the pointer to the note's near corner: right of the pointer and above it, clear of the cursor's badge
    private const double OffsetX = 16;
    private const double OffsetY = -30;

    private readonly Canvas _overlay;
    private readonly Border _note;
    private readonly TextBlock _text;
    private readonly DispatcherTimer _timer;

    public PointerHint(Canvas overlay)
    {
        _overlay = overlay;

        _text = new TextBlock { FontSize = 11.5, TextWrapping = TextWrapping.NoWrap };
        _text.SetResourceReference(TextBlock.ForegroundProperty, "IRI.Maptor.Brushes.OnMap.Text");
        _text.SetResourceReference(TextBlock.FontFamilyProperty, "iranSans");

        _note = new Border
        {
            Child = _text,
            Padding = new Thickness(6, 2, 6, 3),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        _note.SetResourceReference(Border.BackgroundProperty, "IRI.Maptor.Brushes.OnMap.Surface");
        _note.SetResourceReference(Border.BorderBrushProperty, "IRI.Maptor.Brushes.OnMap.Border");

        _overlay.Children.Add(_note);

        _timer = new DispatcherTimer { Interval = ShownFor };
        _timer.Tick += (_, _) => Hide();
    }

    public bool IsShown => _note.Visibility == Visibility.Visible;

    public string Text => _text.Text;

    /// <param name="screenPoint">The pointer, on the overlay.</param>
    public void Show(Point screenPoint, string text, FlowDirection direction = FlowDirection.LeftToRight)
    {
        _text.Text = text;
        _text.FlowDirection = direction;

        _note.Visibility = Visibility.Visible;

        _note.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var left = screenPoint.X + OffsetX;
        var top = screenPoint.Y + OffsetY;

        // kept inside the map
        if (_overlay.ActualWidth > 0 && left + _note.DesiredSize.Width > _overlay.ActualWidth)
            left = screenPoint.X - OffsetX - _note.DesiredSize.Width;

        if (top < 0)
            top = screenPoint.Y + 16;

        Canvas.SetLeft(_note, left);
        Canvas.SetTop(_note, top);

        _timer.Stop();
        _timer.Start();
    }

    public void Hide()
    {
        _timer.Stop();

        _note.Visibility = Visibility.Collapsed;
    }
}

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// A thin rounded colour track with a round thumb, for one channel of <see cref="MaptorColorPicker"/>
/// (hue, opacity, red/green/blue, saturation, brightness). <see cref="Value"/> is [0, 1] along the
/// track. Drag or click to set; arrow keys move by 1 %, with Shift by 10 %; Home/End jump to the ends.
/// Always runs left to right, also inside right-to-left forms.
/// </summary>
public class ColorChannelSlider : FrameworkElement
{
    private const double TrackHeight = 12;
    private const double ThumbRadius = 8;

    static ColorChannelSlider()
    {
        FocusableProperty.OverrideMetadata(typeof(ColorChannelSlider), new FrameworkPropertyMetadata(true));
        CursorProperty.OverrideMetadata(typeof(ColorChannelSlider), new FrameworkPropertyMetadata(Cursors.Hand));
        HeightProperty.OverrideMetadata(typeof(ColorChannelSlider), new FrameworkPropertyMetadata(2 * ThumbRadius + 2));
    }

    // A local value, not a metadata default: an inherited RightToLeft from the form would beat a default.
    public ColorChannelSlider() => FlowDirection = FlowDirection.LeftToRight;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(ColorChannelSlider),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((ColorChannelSlider)d).OnValueChanged(), (_, v) => Math.Clamp((double)v, 0, 1)));

    /// <summary>Raised whenever <see cref="Value"/> changes, by the user or in code.</summary>
    public event EventHandler? ValueChanged;

    /// <summary>True while the user is dragging the thumb.</summary>
    public bool IsDragging => IsMouseCaptured;

    private void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>[0, 1].</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(ColorChannelSlider),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The track's fill, normally a horizontal gradient from the channel's minimum to its maximum.</summary>
    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public static readonly DependencyProperty ThumbColorProperty = DependencyProperty.Register(
        nameof(ThumbColor), typeof(Color), typeof(ColorChannelSlider),
        new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The colour shown inside the thumb (the picker's current colour, or the hue for the hue track).</summary>
    public Color ThumbColor
    {
        get => (Color)GetValue(ThumbColorProperty);
        set => SetValue(ThumbColorProperty, value);
    }

    public static readonly DependencyProperty ShowCheckerboardProperty = DependencyProperty.Register(
        nameof(ShowCheckerboard), typeof(bool), typeof(ColorChannelSlider),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Draw a checkerboard under the track (for the opacity channel, whose track is partly transparent).</summary>
    public bool ShowCheckerboard
    {
        get => (bool)GetValue(ShowCheckerboardProperty);
        set => SetValue(ShowCheckerboardProperty, value);
    }

    private static readonly Pen ThumbShadow = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), 1));

    private static readonly Pen ThumbRing = Freeze(new Pen(Brushes.White, 2.5));

    private static readonly Pen TrackEdge = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)), 1));

    protected override void OnRender(DrawingContext dc)
    {
        double width = ActualWidth;

        if (width <= 2 * ThumbRadius)
            return;

        double top = (ActualHeight - TrackHeight) / 2;
        var track = new Rect(0, top, width, TrackHeight);
        double r = TrackHeight / 2;

        if (ShowCheckerboard && TryFindResource("IRI.Maptor.Brushes.Checkerboard") is Brush checker)
            dc.DrawRoundedRectangle(checker, null, track, r, r);

        dc.DrawRoundedRectangle(TrackBrush, TrackEdge, track, r, r);

        // The thumb's centre travels between the two rounded ends so it never leaves the track.
        var centre = new Point(ThumbRadius + Value * (width - 2 * ThumbRadius), ActualHeight / 2);
        var fill = new SolidColorBrush(ThumbColor);
        fill.Freeze();

        dc.DrawEllipse(null, ThumbShadow, centre, ThumbRadius + 0.5, ThumbRadius + 0.5);
        dc.DrawEllipse(fill, ThumbRing, centre, ThumbRadius - 1.25, ThumbRadius - 1.25);

        if (IsKeyboardFocused)
        {
            var accent = (TryFindResource("MahApps.Brushes.Accent") as Brush) ?? Brushes.SteelBlue;
            dc.DrawEllipse(null, new Pen(accent, 1.5), centre, ThumbRadius + 2.5, ThumbRadius + 2.5);
        }
    }

    protected override void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnIsKeyboardFocusedChanged(e);
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        CaptureMouse();
        SetFrom(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (IsMouseCaptured)
            SetFrom(e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        double step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 0.10 : 0.01;

        switch (e.Key)
        {
            case Key.Left:
            case Key.Down: Value -= step; break;
            case Key.Right:
            case Key.Up: Value += step; break;
            case Key.Home: Value = 0; break;
            case Key.End: Value = 1; break;
            default: return;
        }

        e.Handled = true;
    }

    private void SetFrom(Point p)
    {
        double travel = ActualWidth - 2 * ThumbRadius;

        if (travel > 0)
            Value = (p.X - ThumbRadius) / travel;
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}

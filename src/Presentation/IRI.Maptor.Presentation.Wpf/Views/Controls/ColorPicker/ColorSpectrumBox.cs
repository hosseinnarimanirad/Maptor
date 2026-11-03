using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// The saturation × brightness square of the Spectrum tab, for a fixed <see cref="Hue"/>: saturation
/// grows to the right, brightness to the top. Drag (or click) to pick; arrow keys move by 1 %,
/// with Shift by 10 %. Always drawn left to right, also inside right-to-left forms.
/// </summary>
public class ColorSpectrumBox : FrameworkElement
{
    private const double ThumbRadius = 8;
    private const double CornerRadius = 8;

    static ColorSpectrumBox()
    {
        FocusableProperty.OverrideMetadata(typeof(ColorSpectrumBox), new FrameworkPropertyMetadata(true));
        CursorProperty.OverrideMetadata(typeof(ColorSpectrumBox), new FrameworkPropertyMetadata(Cursors.Cross));
    }

    // A local value, not a metadata default: an inherited RightToLeft from the form would beat a default.
    public ColorSpectrumBox() => FlowDirection = FlowDirection.LeftToRight;

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue), typeof(double), typeof(ColorSpectrumBox),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Degrees, [0, 360).</summary>
    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>Raised whenever <see cref="Saturation"/> or <see cref="Brightness"/> changes, by the user or in code.</summary>
    public event EventHandler? ValueChanged;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ColorSpectrumBox)d).ValueChanged?.Invoke(d, EventArgs.Empty);

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation), typeof(double), typeof(ColorSpectrumBox),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged));

    /// <summary>[0, 1], left to right.</summary>
    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(
        nameof(Brightness), typeof(double), typeof(ColorSpectrumBox),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged));

    /// <summary>[0, 1], bottom to top.</summary>
    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    private static readonly Brush DarkOverlay = Freeze(new LinearGradientBrush(
        Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 0, 0), new Point(0, 0), new Point(0, 1)));

    private static readonly Pen ThumbShadow = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), 1));

    private static readonly Pen ThumbRing = Freeze(new Pen(Brushes.White, 2.5));

    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(RenderSize);

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var pure = ColorMath.FromHsv(Hue, 1, 1);
        var horizontal = new LinearGradientBrush(Colors.White, pure, new Point(0, 0), new Point(1, 0));
        horizontal.Freeze();

        dc.PushClip(new RectangleGeometry(rect, CornerRadius, CornerRadius));
        dc.DrawRectangle(horizontal, null, rect);
        dc.DrawRectangle(DarkOverlay, null, rect);
        dc.Pop();

        // The thumb may overhang the edge so that the corners stay reachable.
        var centre = new Point(Saturation * rect.Width, (1 - Brightness) * rect.Height);
        var current = new SolidColorBrush(ColorMath.FromHsv(Hue, Saturation, Brightness));
        current.Freeze();

        dc.DrawEllipse(null, ThumbShadow, centre, ThumbRadius + 0.5, ThumbRadius + 0.5);
        dc.DrawEllipse(current, ThumbRing, centre, ThumbRadius - 1.25, ThumbRadius - 1.25);

        if (IsKeyboardFocused)
            dc.DrawRoundedRectangle(null, FocusPen, new Rect(-2, -2, rect.Width + 4, rect.Height + 4), CornerRadius + 2, CornerRadius + 2);
    }

    private Pen FocusPen => new((TryFindResource("MahApps.Brushes.Accent") as Brush) ?? Brushes.SteelBlue, 1.5);

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
        PickAt(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (IsMouseCaptured)
            PickAt(e.GetPosition(this));
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
            case Key.Left: Saturation = Math.Clamp(Saturation - step, 0, 1); break;
            case Key.Right: Saturation = Math.Clamp(Saturation + step, 0, 1); break;
            case Key.Up: Brightness = Math.Clamp(Brightness + step, 0, 1); break;
            case Key.Down: Brightness = Math.Clamp(Brightness - step, 0, 1); break;
            default: return;
        }

        e.Handled = true;
    }

    private void PickAt(Point p)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return;

        Saturation = Math.Clamp(p.X / ActualWidth, 0, 1);
        Brightness = Math.Clamp(1 - p.Y / ActualHeight, 0, 1);
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}

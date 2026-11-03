using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// A thin horizontal bar that resizes <see cref="Target"/> vertically when dragged. Put it directly
/// under the target, inside the same collapsible container (e.g. an expander's content), so it
/// disappears with the target. A GridSplitter cannot do this for an expander in an Auto row: once
/// dragged, the row keeps its pixel height and leaves a gap when the expander collapses.
/// </summary>
/// <remarks>
/// Until the first drag the target keeps its own sizing (auto height, its own MaxHeight), so a short
/// list stays short. The first drag fixes the height and lifts that MaxHeight; later drags are
/// limited to <see cref="MinTargetHeight"/>..<see cref="MaxTargetHeight"/>. Double-click goes back to
/// the target's own sizing. The look comes from the implicit style in Controls.Misc.xaml.
/// </remarks>
public class VerticalResizeGrip : Thumb
{
    private double? _originalMaxHeight;

    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(VerticalResizeGrip), new PropertyMetadata(null));

    public FrameworkElement? Target
    {
        get => (FrameworkElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public static readonly DependencyProperty MinTargetHeightProperty =
        DependencyProperty.Register(nameof(MinTargetHeight), typeof(double), typeof(VerticalResizeGrip), new PropertyMetadata(60d));

    public double MinTargetHeight
    {
        get => (double)GetValue(MinTargetHeightProperty);
        set => SetValue(MinTargetHeightProperty, value);
    }

    public static readonly DependencyProperty MaxTargetHeightProperty =
        DependencyProperty.Register(nameof(MaxTargetHeight), typeof(double), typeof(VerticalResizeGrip), new PropertyMetadata(800d));

    public double MaxTargetHeight
    {
        get => (double)GetValue(MaxTargetHeightProperty);
        set => SetValue(MaxTargetHeightProperty, value);
    }

    public VerticalResizeGrip()
    {
        DragDelta += OnDragDelta;
        MouseDoubleClick += OnMouseDoubleClick;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var target = Target;

        if (target is null)
            return;

        if (double.IsNaN(target.Height))
        {
            _originalMaxHeight ??= target.MaxHeight;
            target.MaxHeight = double.PositiveInfinity;
            target.Height = target.ActualHeight;
        }

        var max = Math.Max(MinTargetHeight, MaxTargetHeight);

        target.Height = Math.Clamp(target.Height + e.VerticalChange, MinTargetHeight, max);
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var target = Target;

        if (target is null)
            return;

        target.Height = double.NaN;

        if (_originalMaxHeight is double originalMaxHeight)
            target.MaxHeight = originalMaxHeight;

        e.Handled = true;
    }
}

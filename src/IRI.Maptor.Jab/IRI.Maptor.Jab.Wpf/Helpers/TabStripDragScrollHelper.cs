using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Wpf.Helpers;

/// <summary>
/// Drag-to-pan for a horizontally scrolling tab strip: grab the headers and push them sideways
/// instead of clicking the chevrons repeatedly. Attach to the strip's ScrollViewer, see
/// Assets/Styles/Controls.TabControl.xaml.
/// </summary>
/// <remarks>
/// WPF's own PanningMode is touch/stylus only, so mouse panning has to be handled here.
///
/// The awkward part is that a TabItem selects on mouse *down*, before anyone can tell a click from
/// the start of a drag. So the press is swallowed and, if the pointer never travelled far enough to
/// count as a drag, replayed as a selection on release. Anything clickable inside a header (the
/// attribute table's close button) is left alone entirely.
/// </remarks>
public static class TabStripDragScrollHelper
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TabStripDragScrollHelper),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    // Only one drag can be in flight at a time, so the gesture's state lives here rather than being
    // attached to every strip in the app.
    private static ScrollViewer? _target;
    private static Point _origin;
    private static double _originOffset;
    private static bool _isDragging;
    private static Cursor? _cursorBeforeDrag;

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        scrollViewer.PreviewMouseMove -= OnPreviewMouseMove;
        scrollViewer.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
        scrollViewer.LostMouseCapture -= OnLostMouseCapture;

        if (e.NewValue is true)
        {
            scrollViewer.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            scrollViewer.PreviewMouseMove += OnPreviewMouseMove;
            scrollViewer.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            scrollViewer.LostMouseCapture += OnLostMouseCapture;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var scrollViewer = (ScrollViewer)sender;

        // nothing overflows, so the headers behave exactly as they did before
        if (scrollViewer.ScrollableWidth <= 0)
        {
            return;
        }

        // a button inside a header keeps its own click
        if (IsWithinButton(e.OriginalSource as DependencyObject, scrollViewer))
        {
            return;
        }

        _target = scrollViewer;
        _origin = e.GetPosition(scrollViewer);
        _originOffset = scrollViewer.HorizontalOffset;
        _isDragging = false;

        scrollViewer.CaptureMouse();
        e.Handled = true;
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_target is null || !ReferenceEquals(sender, _target))
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var travelled = e.GetPosition(_target).X - _origin.X;

        if (!_isDragging)
        {
            // below the system's drag threshold this is still a click, not a pan
            if (Math.Abs(travelled) < SystemParameters.MinimumHorizontalDragDistance)
            {
                return;
            }

            _isDragging = true;
            _cursorBeforeDrag = _target.Cursor;
            _target.Cursor = Cursors.ScrollWE;
        }

        // Under an RTL FlowDirection both the position and the offset are measured in the same
        // mirrored space, so the content still follows the pointer without a special case.
        _target.ScrollToHorizontalOffset(_originOffset - travelled);
        e.Handled = true;
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_target is null || !ReferenceEquals(sender, _target))
        {
            return;
        }

        var scrollViewer = _target;
        var wasDragging = _isDragging;
        var position = e.GetPosition(scrollViewer);

        // resets the gesture state via LostMouseCapture
        scrollViewer.ReleaseMouseCapture();

        if (!wasDragging)
        {
            SelectTabUnder(scrollViewer, position);
        }

        e.Handled = true;
    }

    private static void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_target is null || !ReferenceEquals(sender, _target))
        {
            return;
        }

        if (_isDragging)
        {
            _target.Cursor = _cursorBeforeDrag;
        }

        _target = null;
        _cursorBeforeDrag = null;
        _isDragging = false;
    }

    /// <summary>Replays the swallowed press as a selection.</summary>
    private static void SelectTabUnder(ScrollViewer scrollViewer, Point position)
    {
        if (scrollViewer.InputHitTest(position) is not DependencyObject hit)
        {
            return;
        }

        if (FindAncestor<TabItem>(hit, scrollViewer) is { IsEnabled: true } tabItem)
        {
            tabItem.IsSelected = true;
            tabItem.Focus();
        }
    }

    private static bool IsWithinButton(DependencyObject? source, DependencyObject root)
        => FindAncestor<ButtonBase>(source, root) is not null;

    private static T? FindAncestor<T>(DependencyObject? node, DependencyObject root) where T : DependencyObject
    {
        while (node is not null && !ReferenceEquals(node, root))
        {
            if (node is T match)
            {
                return match;
            }

            node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        }

        return null;
    }
}

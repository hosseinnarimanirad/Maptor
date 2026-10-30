using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;

public partial class TextboxMarker : MapMarker
{
    // set by MapViewer.OnMapActionChanged (Pan or DrawText); that callback only runs on a change, so
    // the start value must match MapViewer.MapAction's default, Pan, or labels loaded from a project
    // show no options until the map action changes once
    public static bool IsOptionsEnabled { get; set; } = true;

    private readonly DispatcherTimer _popupCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    /// <summary>
    /// One-shot: focus the text box the next time the marker loads. Set by the create-text
    /// flow so the user can type immediately; it clears itself, so the re-adds that every
    /// map refresh performs never steal focus back.
    /// </summary>
    public bool FocusTextBoxOnLoad { get; set; }

    public TextboxMarker()
    {
        InitializeComponent();

        _popupCloseTimer.Tick += (_, _) =>
        {
            _popupCloseTimer.Stop();

            // a handle being dragged keeps the label "hovered"; releasing it outside the label
            // raises MouseLeave again, which restarts the timer
            if (IsDraggingHandle)
                return;

            formatPopup.IsOpen = false;
        };

        // subscribed here, not in Loaded: every map refresh re-adds the marker and raises
        // Loaded again, so subscribing there stacked one more handler per refresh
        formatPopup.Opened += OnPopupOpened;
        formatPopup.Closed += OnPopupClosed;
        popupBorder.SizeChanged += OnPopupBorderSizeChanged;

        // the popup content is a separate visual tree, so it reports its own focus-within
        IsKeyboardFocusWithinChanged += OnKeyboardFocusWithinChanged;
        popupBorder.IsKeyboardFocusWithinChanged += OnKeyboardFocusWithinChanged;

        // a Popup does not follow its target: the map moves the marker through this
        // TransformGroup (drag, pan, zoom) and the label resizes as the user types
        if (RenderTransform is TransformGroup transformGroup)
            transformGroup.Changed += (_, _) => UpdatePopupPlacement();
        SizeChanged += (_, _) => UpdatePopupPlacement();
        contentRotation.Changed += (_, _) => UpdatePopupPlacement();
        // the handles appear with the popup and the rotate handle reaches above the label; its
        // size goes 0 -> 18 once it is laid out (IsVisibleChanged comes before that)
        rotateThumb.SizeChanged += (_, _) => UpdatePopupPlacement();

        // a StaysOpen popup is a top-most window of its own: close it when the marker goes
        // away (deleted, or taken off the canvas by a refresh), is hidden, or the app is left
        Unloaded += (_, _) =>
        {
            formatPopup.IsOpen = false;
            SetHostWindow(null);
        };
        IsVisibleChanged += (_, e) =>
        {
            if (!(bool)e.NewValue)
                formatPopup.IsOpen = false;
        };

        Loaded += (_, _) =>
        {
            SetHostWindow(Window.GetWindow(this));

            if (FocusTextBoxOnLoad)
            {
                FocusTextBoxOnLoad = false;

                // deferred: focusing inline during Loaded is unreliable while the element
                // is still being connected to the canvas
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    labelBox.Focus();
                    Keyboard.Focus(labelBox);
                }), DispatcherPriority.Input);
            }
        };
    }

    // an empty label is removed once the user leaves it (clicks the map or another control);
    // without a frame it would be invisible and impossible to grab. Working in the format
    // popup never counts as leaving.
    private void OnKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue || !IsOnScreen())
            return;

        // deferred: let focus settle first, and skip it if the user came back, a map refresh
        // took the marker off the canvas (that drops focus too), or the app was only
        // deactivated (Alt+Tab drops keyboard focus but WPF restores it on return)
        var leftPopup = ReferenceEquals(sender, popupBorder);

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!IsOnScreen() || Window.GetWindow(this) is { IsActive: false } || ContainsKeyboardFocus())
                return;

            if (Keyboard.FocusedElement is null or Window)
            {
                // the popup closed (mouse moved away) while it held focus: not the user
                // leaving; OnPopupClosed normally hands focus back first, this is the fallback
                if (leftPopup && !formatPopup.IsOpen)
                {
                    FocusTextBox();
                    return;
                }

                // a click in the popup that could not keep focus, not the user leaving
                if (formatPopup.IsOpen && popupBorder.IsMouseOver)
                    return;
            }

            if (DataContext is TextboxMarkerViewModel viewModel && string.IsNullOrWhiteSpace(viewModel.LabelValue))
            {
                formatPopup.IsOpen = false;
                viewModel.DeleteCommand.Execute(null);
            }
        }), DispatcherPriority.Background);
    }

    private bool IsOnScreen() => PresentationSource.FromVisual(this) is not null;

    // after a choice in a popup combo the caret goes back to the text, so the user can type
    // straight away (and focus is not left inside the popup, which closes on mouse leave)
    private void OnPopupComboDropDownClosed(object? sender, EventArgs e)
    {
        FocusTextBox();
    }

    // the popup closes on mouse leave; focus still inside it would drop to nowhere
    private void OnPopupClosed(object? sender, EventArgs e)
    {
        for (var node = Keyboard.FocusedElement as DependencyObject; node is not null; node = GetParent(node))
        {
            if (ReferenceEquals(node, popupBorder))
            {
                FocusTextBox();
                return;
            }
        }
    }

    // keeps the user's caret/selection: the box has SelectAllOnFocus, and a select-all here
    // would make the next keystroke replace the whole label
    private void FocusTextBox()
    {
        if (!IsOnScreen() || labelBox.IsKeyboardFocused)
            return;

        var selectionStart = labelBox.SelectionStart;
        var selectionLength = labelBox.SelectionLength;
        var text = labelBox.Text;

        labelBox.Focus();
        Keyboard.Focus(labelBox);

        labelBox.Select(selectionStart, selectionLength);

        // again after any deferred select-all from the focus handlers, unless the user has
        // already typed
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (labelBox.IsKeyboardFocused && labelBox.Text == text)
                labelBox.Select(selectionStart, selectionLength);
        }), DispatcherPriority.Background);
    }

    // the label itself, its format popup, or a drop-down opened from the popup; the walk
    // crosses popup boundaries through the logical tree (popup content → Popup → marker),
    // like MapViewer's FindAncestor<TextboxMarker>
    private bool ContainsKeyboardFocus()
    {
        if (IsKeyboardFocusWithin)
            return true;

        for (var node = Keyboard.FocusedElement as DependencyObject; node is not null; node = GetParent(node))
        {
            if (ReferenceEquals(node, this))
                return true;
        }

        return false;
    }

    // logical first, then visual; a template part with neither (the root of a ComboBox
    // drop-down) continues through its templated parent
    private static DependencyObject? GetParent(DependencyObject node)
        => LogicalTreeHelper.GetParent(node)
            ?? (node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : null)
            ?? (node as FrameworkElement)?.TemplatedParent;

    private void OnPopupBorderSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border border)
        {
            var clip = new System.Windows.Media.RectangleGeometry
            {
                Rect = new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                RadiusX = 5,
                RadiusY = 5
            };
            border.Clip = clip;
        }

        // the popup's width changes with its content (e.g. the angle box text), and the centring
        // offset depends on it
        UpdatePopupPlacement();
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        // Wait for the popup to render to get accurate size
        (formatPopup.Child as FrameworkElement)?.UpdateLayout();

        UpdatePopupPlacement();
    }

    // centres the popup above the label (MS Office style) and makes it re-place itself; an
    // unchanged offset is nudged, since only a change makes the Popup re-position. It is placed
    // against the rotated content's bounds, so a turned label never sits under its own popup
    private void UpdatePopupPlacement()
    {
        if (!formatPopup.IsOpen || formatPopup.Child is not FrameworkElement popupChild)
            return;

        var contentBounds = mainBorder.TransformToAncestor(this).TransformBounds(new Rect(mainBorder.RenderSize));

        // it also clears the rotate handle, which reaches above the label, or it would cover it;
        // it stays centred on the text
        var placementBounds = contentBounds;

        if (rotateThumb.IsVisible)
            placementBounds.Union(rotateThumb.TransformToAncestor(this).TransformBounds(new Rect(rotateThumb.RenderSize)));

        formatPopup.PlacementRectangle = placementBounds;

        // the offset counts from the placement rectangle's left edge
        var offset = contentBounds.Left - placementBounds.Left + (contentBounds.Width - popupChild.ActualWidth) / 2;

        if (formatPopup.HorizontalOffset == offset)
            formatPopup.HorizontalOffset = offset + 0.01;

        formatPopup.HorizontalOffset = offset;
    }

    private Window? _hostWindow;

    private void SetHostWindow(Window? window)
    {
        if (ReferenceEquals(_hostWindow, window))
            return;

        if (_hostWindow is not null)
            _hostWindow.Deactivated -= OnHostWindowDeactivated;

        _hostWindow = window;

        if (_hostWindow is not null)
            _hostWindow.Deactivated += OnHostWindowDeactivated;
    }

    private void OnHostWindowDeactivated(object? sender, EventArgs e)
    {
        formatPopup.IsOpen = false;
    }

    // Esc or Ctrl+Enter finishes editing, the same as clicking away (an empty label goes);
    // plain Enter still starts a new line
    private void OnLabelBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var finish = e.Key == Key.Escape
            || (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control));

        if (!finish)
            return;

        e.Handled = true;

        formatPopup.IsOpen = false;
        Keyboard.ClearFocus();
    }

    private void OnMarkerMouseEnter(object sender, MouseEventArgs e)
    {
        if (!IsOptionsEnabled) return;

        _popupCloseTimer.Stop();

        formatPopup.IsOpen = true;
    }

    private void OnMarkerMouseLeave(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        _popupCloseTimer.Start();
    }

    private void OnPopupContentMouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
    }

    private void OnPopupContentMouseLeave(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        _popupCloseTimer.Start();
    }

    #region Resize and rotate handles

    // Shift while rotating snaps to steps of this many degrees
    private const double RotationSnapDegrees = 15;

    private double _rotateStartMouseAngle;
    private double _rotateStartRotation;

    private bool IsDraggingHandle => leftWidthThumb.IsDragging || rightWidthThumb.IsDragging || rotateThumb.IsDragging;

    // the width changes on both sides, so the label stays centred on its anchor; a thumb's own
    // coordinates turn with the label, so the drag follows its rotated edge. A drag starting from
    // the automatic width starts from the width shown
    private void OnWidthThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not TextboxMarkerViewModel viewModel)
            return;

        var direction = ReferenceEquals(sender, rightWidthThumb) ? 1 : -1;

        var width = (viewModel.Formatting_TextWidth ?? mainBorder.ActualWidth) + 2 * direction * e.HorizontalChange;

        // the Box frame's own minimum is larger than the view model's
        viewModel.Formatting_TextWidth = Math.Max(width, mainBorder.MinWidth);
    }

    // double-click: back to the automatic width (grow with the text)
    private void OnWidthThumbDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (DataContext is TextboxMarkerViewModel viewModel)
            viewModel.Formatting_TextWidth = null;
    }

    private void OnRotateThumbDragStarted(object sender, DragStartedEventArgs e)
        => BeginRotation(Mouse.GetPosition(this));

    private void OnRotateThumbDragDelta(object sender, DragDeltaEventArgs e)
        => RotateTo(Mouse.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));

    // mouse positions are in marker coordinates, which stay upright (the rotation is inside)
    private void BeginRotation(Point mouse)
    {
        if (DataContext is not TextboxMarkerViewModel viewModel)
            return;

        _rotateStartMouseAngle = GetAngleAroundAnchor(mouse);
        _rotateStartRotation = viewModel.Formatting_Rotation;
    }

    // turns by the angle the mouse moved around the anchor, so grabbing the handle does not make
    // the label jump; the view model keeps the result in (-180, 180]
    private void RotateTo(Point mouse, bool snap)
    {
        if (DataContext is not TextboxMarkerViewModel viewModel)
            return;

        var rotation = _rotateStartRotation + GetAngleAroundAnchor(mouse) - _rotateStartMouseAngle;

        if (snap)
            rotation = Math.Round(rotation / RotationSnapDegrees) * RotationSnapDegrees;

        viewModel.Formatting_Rotation = rotation;
    }

    // double-click: upright again
    private void OnRotateThumbDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (DataContext is TextboxMarkerViewModel viewModel)
            viewModel.Formatting_Rotation = 0;
    }

    // the direction of a point from the anchor (the label's bottom-centre, the rotation pivot), in
    // degrees clockwise from straight up
    private double GetAngleAroundAnchor(Point position)
        => Math.Atan2(position.X - ActualWidth / 2, ActualHeight - position.Y) * 180 / Math.PI;

    #endregion

    private void root_MouseUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}

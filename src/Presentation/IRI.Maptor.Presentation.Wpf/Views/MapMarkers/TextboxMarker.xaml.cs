using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;

public partial class TextboxMarker : MapMarker
{
    public static bool IsOptionsEnabled { get; set; } = false;

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
            formatPopup.IsOpen = false;
        };

        // subscribed here, not in Loaded: every map refresh re-adds the marker and raises
        // Loaded again, so subscribing there stacked one more handler per refresh
        formatPopup.Opened += OnPopupOpened;
        popupBorder.SizeChanged += OnPopupBorderSizeChanged;

        Loaded += (_, _) =>
        {
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
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        // Center the popup horizontally above the TextBox (MS Office style)
        if (formatPopup.PlacementTarget is FrameworkElement target && formatPopup.Child is FrameworkElement popupChild)
        {
            // Wait for the popup to render to get accurate size
            popupChild.UpdateLayout();
            formatPopup.HorizontalOffset = (target.ActualWidth - popupChild.ActualWidth) / 2;
        }
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

    private void root_MouseUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}

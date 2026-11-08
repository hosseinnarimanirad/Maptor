using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Controls.Dialogs;

/// <summary>
/// A short, non-blocking message shown at the bottom centre of its owner window, with an optional title. It takes
/// no focus and closes on click; unless told otherwise it also closes itself after <see cref="DisplayTime"/> (the
/// timer pauses while the mouse is over it). Opened through <see cref="Services.IDialogService.ShowNotification"/>.
/// </summary>
public partial class NotificationToastView : Window
{
    public static readonly TimeSpan DisplayTime = TimeSpan.FromSeconds(8);

    // distance between the toast and the owner's bottom edge (device-independent pixels)
    private const double BottomOffset = 48;

    // null when the toast stays until the user closes it
    private readonly DispatcherTimer? _timer;

    public NotificationToastView(string message, string? title = null, bool closesAutomatically = true)
    {
        InitializeComponent();

        messageText.Text = message;

        if (!string.IsNullOrWhiteSpace(title))
        {
            titleText.Text = title;
            titleText.Visibility = Visibility.Visible;
        }

        FlowDirection = LocalizationManager.Instance.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        if (closesAutomatically)
        {
            _timer = new DispatcherTimer { Interval = DisplayTime };
            _timer.Tick += (_, _) => Close();

            MouseEnter += (_, _) => _timer.Stop();
            MouseLeave += (_, _) => _timer.Start();
            Closed += (_, _) => _timer.Stop();
        }

        Loaded += (_, _) =>
        {
            PlaceOverOwner();
            _timer?.Start();
        };

        MouseLeftButtonUp += (_, _) => Close();
    }

    /// <summary>True when the toast closes itself after <see cref="DisplayTime"/>.</summary>
    public bool ClosesAutomatically => _timer is not null;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Bottom centre of the owner's client area. Uses the owner's on-screen position rather than Left/Top, which
    /// keep the restore position while the owner is maximized.
    /// </summary>
    private void PlaceOverOwner()
    {
        if (Owner is null || !Owner.IsVisible)
        {
            var area = SystemParameters.WorkArea;

            Left = area.Left + (area.Width - ActualWidth) / 2;
            Top = area.Bottom - ActualHeight - BottomOffset;

            return;
        }

        var source = PresentationSource.FromVisual(Owner);

        var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

        var ownerContent = Owner.Content as FrameworkElement ?? (FrameworkElement)Owner;

        var topLeft = fromDevice.Transform(ownerContent.PointToScreen(new Point(0, 0)));

        var bottomRight = fromDevice.Transform(ownerContent.PointToScreen(new Point(ownerContent.ActualWidth, ownerContent.ActualHeight)));

        // under a right-to-left owner PointToScreen(0,0) is the right edge
        var left = Math.Min(topLeft.X, bottomRight.X);
        var width = Math.Abs(bottomRight.X - topLeft.X);

        Left = left + (width - ActualWidth) / 2;
        Top = Math.Max(topLeft.Y, bottomRight.Y) - ActualHeight - BottomOffset;
    }
}

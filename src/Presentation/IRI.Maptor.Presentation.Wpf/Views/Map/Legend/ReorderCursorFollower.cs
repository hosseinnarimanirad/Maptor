using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Moves the OS mouse cursor to a reorder button's new location after its row has been
/// repositioned by the live-sorted legend, so repeated clicks keep moving the same layer
/// without chasing the row with the mouse.
/// </summary>
internal static class ReorderCursorFollower
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    public static void FollowAfterReorder(Button button, FrameworkElement rowAnchor)
    {
        // only meaningful for mouse-driven clicks; keyboard invocation must not warp the cursor
        if (InputManager.Current.MostRecentInputDevice is not MouseDevice)
            return;

        if (rowAnchor is null || !rowAnchor.IsAncestorOf(button))
            return;

        // captured before the move runs: the chevrons are visible only while their row is
        // hovered, so after the reorder the button itself may be collapsed and cannot be
        // measured — its offset inside the row stays valid though
        var target = button.TranslatePoint(new Point(button.ActualWidth / 2, button.ActualHeight / 2), rowAnchor);

        // deferred so it runs after the move command has executed and the live-sort layout
        // pass has settled; BringIntoView keeps multi-step moves working at the viewport edge
        rowAnchor.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!rowAnchor.IsVisible)
                return;

            rowAnchor.BringIntoView();

            // second dispatch: the scroll requested by BringIntoView is applied in the next
            // layout pass, and PointToScreen is only correct after it
            rowAnchor.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!rowAnchor.IsVisible)
                    return;

                // PointToScreen returns physical pixels, which is what SetCursorPos expects
                var screen = rowAnchor.PointToScreen(target);

                SetCursorPos((int)Math.Round(screen.X), (int)Math.Round(screen.Y));
            }), DispatcherPriority.Loaded);
        }), DispatcherPriority.Loaded);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace IRI.Maptor.Presentation.Wpf.Behaviors;

/// <summary>
/// Makes a <see cref="Popup"/> of buttons behave like a menu, without code in the view or view model:
/// <list type="bullet">
/// <item>opened from the keyboard, the first row takes focus; opened with the mouse, only the popup
/// content does, so keys work but no row looks pre-selected;</item>
/// <item>clicking a row closes the popup before the row's command runs (Click bubbles first), so a
/// dialog opened by that command never appears behind it; turned off by <c>CloseOnClick="False"</c>;</item>
/// <item>Up/Down (wrapping), Home/End move between rows, Tab cycles inside, Esc closes and returns
/// focus to <see cref="Popup.PlacementTarget"/>.</item>
/// </list>
/// Rows are the visible, enabled <see cref="ButtonBase"/>s inside the popup, in visual order; anything
/// else (group headers, separators) is skipped. Closing uses SetCurrentValue so an IsOpen binding to
/// the opening ToggleButton survives and unchecks it.
/// </summary>
public static class PopupMenuBehavior
{
    public static readonly DependencyProperty IsMenuProperty =
        DependencyProperty.RegisterAttached(
            "IsMenu",
            typeof(bool),
            typeof(PopupMenuBehavior),
            new PropertyMetadata(false, OnIsMenuChanged));

    public static bool GetIsMenu(DependencyObject obj) =>
        (bool)obj.GetValue(IsMenuProperty);

    public static void SetIsMenu(DependencyObject obj, bool value) =>
        obj.SetValue(IsMenuProperty, value);

    /// <summary>
    /// True (default): a row click closes the popup, as in a command menu. False for a popup of
    /// toggles (e.g. a multi-select filter) that must stay open while the user checks rows.
    /// </summary>
    public static readonly DependencyProperty CloseOnClickProperty =
        DependencyProperty.RegisterAttached(
            "CloseOnClick",
            typeof(bool),
            typeof(PopupMenuBehavior),
            new PropertyMetadata(true));

    public static bool GetCloseOnClick(DependencyObject obj) =>
        (bool)obj.GetValue(CloseOnClickProperty);

    public static void SetCloseOnClick(DependencyObject obj, bool value) =>
        obj.SetValue(CloseOnClickProperty, value);

    private static void OnIsMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup popup)
            return;

        popup.Opened -= Popup_Opened;
        popup.PreviewKeyDown -= Popup_PreviewKeyDown;
        popup.RemoveHandler(ButtonBase.ClickEvent, (RoutedEventHandler)Row_Click);

        if ((bool)e.NewValue)
        {
            popup.Opened += Popup_Opened;
            popup.PreviewKeyDown += Popup_PreviewKeyDown;
            popup.AddHandler(ButtonBase.ClickEvent, (RoutedEventHandler)Row_Click);
        }
    }

    private static void Popup_Opened(object? sender, EventArgs e)
    {
        if (sender is not Popup { Child: FrameworkElement content } popup)
            return;

        // the content has to be focusable for the mouse-open case; it carries no focus rectangle
        content.Focusable = true;
        content.FocusVisualStyle = null;
        KeyboardNavigation.SetTabNavigation(content, KeyboardNavigationMode.Cycle);

        var openedByKeyboard = InputManager.Current.MostRecentInputDevice is KeyboardDevice;

        // Input priority runs after layout, so the rows exist in the visual tree by then
        popup.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (openedByKeyboard)
                (GetRows(content).FirstOrDefault() ?? (IInputElement)content).Focus();
            else
                content.Focus();
        });
    }

    private static void Row_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Popup popup && GetCloseOnClick(popup))
            popup.SetCurrentValue(Popup.IsOpenProperty, false);
    }

    private static void Popup_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not Popup { Child: FrameworkElement content } popup)
            return;

        if (e.Key == Key.Escape)
        {
            popup.SetCurrentValue(Popup.IsOpenProperty, false);
            popup.PlacementTarget?.Focus();
            e.Handled = true;
            return;
        }

        var rows = GetRows(content);

        if (rows.Count == 0)
            return;

        var index = IndexOfRowWithFocus(rows);

        ButtonBase? target = e.Key switch
        {
            Key.Down => rows[index < 0 ? 0 : (index + 1) % rows.Count],
            Key.Up => rows[index <= 0 ? rows.Count - 1 : index - 1],
            Key.Home => rows[0],
            Key.End => rows[^1],
            _ => null
        };

        if (target is not null)
        {
            target.Focus();
            target.BringIntoView();
            e.Handled = true;
        }
    }

    // the row that is focused or contains the focused element (e.g. the "Only" link of a filter row)
    private static int IndexOfRowWithFocus(List<ButtonBase> rows)
    {
        var current = Keyboard.FocusedElement as DependencyObject;

        while (current is not null)
        {
            if (current is ButtonBase button && rows.IndexOf(button) is var index and >= 0)
                return index;

            current = VisualTreeHelper.GetParent(current);
        }

        return -1;
    }

    private static List<ButtonBase> GetRows(DependencyObject root)
    {
        var result = new List<ButtonBase>();

        Collect(root, result);

        return result;
    }

    // depth-first in visual order; a row's own content is not searched for nested buttons
    private static void Collect(DependencyObject parent, List<ButtonBase> result)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is ButtonBase button)
            {
                if (button.IsVisible && button.IsEnabled)
                    result.Add(button);
            }
            else
            {
                Collect(child, result);
            }
        }
    }
}

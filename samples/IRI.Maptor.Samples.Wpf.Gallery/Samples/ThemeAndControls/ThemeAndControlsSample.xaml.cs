using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.ThemeAndControls;

/// <summary>
/// Drives <see cref="ThemeHelper"/> from three controls and renders the whole token set underneath,
/// so a theme change is visible on every style at once.
/// </summary>
public partial class ThemeAndControlsSample : UserControl
{
    /// <summary>
    /// Set while the UI is being brought in line with <see cref="ThemeHelper.Current"/>, so the
    /// change handlers below do not call back into ThemeHelper and start a loop. The three controls
    /// are inputs *and* outputs: SetMode switches FollowWindowsMode off, and Windows can change the
    /// mode with nobody touching the UI at all.
    /// </summary>
    private bool _syncing;

    public ThemeAndControlsSample()
    {
        InitializeComponent();

        accentCombo.ItemsSource = ThemeHelper.AvailableThemes;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // subscribe here rather than in the constructor: ThemeChanged is a static event, so a view
        // that never unsubscribes keeps itself alive for the life of the process
        ThemeHelper.ThemeChanged += OnThemeChanged;
        Sync(ThemeHelper.Current);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeHelper.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(AppliedTheme theme) => Dispatcher.Invoke(() => Sync(theme));

    private void Sync(AppliedTheme theme)
    {
        _syncing = true;

        try
        {
            accentCombo.SelectedItem = ThemeHelper.AvailableThemes.FirstOrDefault(t => t.Color == theme.Color);
            darkSwitch.IsOn = theme.Mode == ThemeMode.Dark;
            followWindows.IsChecked = ThemeHelper.FollowWindowsMode;

            currentTheme.Text = theme.ToString();
            windowsMode.Text = $"Windows is set to {ThemeHelper.WindowsMode}";
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnAccentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || accentCombo.SelectedItem is not ThemeInfoModel selected)
            return;

        // SetAccent, not ApplyTheme: the mode is none of this control's business, and a bare
        // ApplyTheme(colour) used to reset the whole application to light
        ThemeHelper.SetAccent(selected.Color);
    }

    private void OnDarkToggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;

        // this also switches FollowWindowsMode off, which is why Sync re-reads the checkbox
        ThemeHelper.SetMode(darkSwitch.IsOn ? ThemeMode.Dark : ThemeMode.Light);
    }

    private void OnFollowWindowsChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;

        ThemeHelper.FollowWindowsMode = followWindows.IsChecked == true;

        // turning it ON applies the Windows mode and raises ThemeChanged, so Sync runs by itself;
        // turning it OFF changes nothing visible, so refresh the readout here
        if (followWindows.IsChecked != true)
            Sync(ThemeHelper.Current);
    }
}

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;

namespace IRI.Maptor.Samples.Wpf.Gallery;

public partial class MainWindow : Window
{
    private readonly ListCollectionView _samples;

    private SampleInfo? _current;

    private bool _syncingTheme;

    public MainWindow()
    {
        InitializeComponent();

        _samples = new ListCollectionView((System.Collections.IList)SampleCatalog.All);
        _samples.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleInfo.Category)));
        _samples.Filter = item => item is SampleInfo sample && Matches(sample, searchBox.Text);

        samplesList.ItemsSource = _samples;
        samplesList.SelectedIndex = 0;

        // ThemeChanged is static: subscribe while the window lives, and show the current mode
        ThemeHelper.ThemeChanged += OnThemeChanged;
        Closed += (_, _) => ThemeHelper.ThemeChanged -= OnThemeChanged;
        SyncTheme(ThemeHelper.Current);
    }

    private static bool Matches(SampleInfo sample, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var term = text.Trim();

        return sample.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || sample.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
            || sample.Description.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSampleSelected(object sender, SelectionChangedEventArgs e)
    {
        // A search that filters the shown sample out leaves it on screen; only a new choice replaces it
        if (samplesList.SelectedItem is not SampleInfo sample || sample == _current)
            return;

        _current = sample;

        sampleIcon.Kind = sample.Icon;
        sampleCategory.Text = sample.Category;
        sampleTitle.Text = sample.Title;
        sampleDescription.Text = sample.Description;
        sampleHost.Content = sample.View;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        _samples.Refresh();

        noResults.Visibility = _samples.IsEmpty ? Visibility.Visible : Visibility.Collapsed;

        // keep the shown sample highlighted while it still matches
        if (_current is not null && _samples.Contains(_current))
            samplesList.SelectedItem = _current;
    }

    /// <summary>Esc clears the search; Down and Enter move into the list.</summary>
    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && searchBox.Text.Length > 0)
        {
            searchBox.Clear();
            e.Handled = true;
        }
        else if (e.Key is Key.Down or Key.Enter && !_samples.IsEmpty)
        {
            if (samplesList.SelectedItem is null || !_samples.Contains(samplesList.SelectedItem))
                samplesList.SelectedIndex = 0;

            (samplesList.ItemContainerGenerator.ContainerFromItem(samplesList.SelectedItem) as ListBoxItem)?.Focus();
            e.Handled = true;
        }
    }

    private void OnFind(object sender, ExecutedRoutedEventArgs e)
    {
        searchBox.Focus();
        searchBox.SelectAll();
    }

    private void OnOpenSource(object sender, RoutedEventArgs e)
    {
        if (_current is null)
            return;

        Process.Start(new ProcessStartInfo(_current.SourceUrl) { UseShellExecute = true });
    }

    private void OnThemeChanged(AppliedTheme theme) => Dispatcher.Invoke(() => SyncTheme(theme));

    private void SyncTheme(AppliedTheme theme)
    {
        _syncingTheme = true;

        darkSwitch.IsOn = theme.Mode == ThemeMode.Dark;

        _syncingTheme = false;
    }

    private void OnDarkToggled(object sender, RoutedEventArgs e)
    {
        if (!_syncingTheme)
            ThemeHelper.SetMode(darkSwitch.IsOn ? ThemeMode.Dark : ThemeMode.Light);
    }
}

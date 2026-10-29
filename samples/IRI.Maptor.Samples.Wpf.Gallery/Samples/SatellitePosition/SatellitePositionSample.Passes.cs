using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Core.Satellite.Passes;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using MahApps.Metro.IconPacks;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.SatellitePosition;

/// <summary>
/// The "Passes over a region" view: pick a point or draw an area, then list when each satellite is
/// visible from it, or when an imager's swath reaches it, with <see cref="PassFinder"/>.
/// </summary>
public partial class SatellitePositionSample
{
    /// <summary>Above this many satellites the search can take minutes, so it asks first.</summary>
    private const int LargeSearch = 100;

    private CancellationTokenSource? _passSearch;

    private void OnViewChanged(object sender, RoutedEventArgs e)
    {
        if (satellitesPanel is null || passesPanel is null)
            return;

        var passes = passesTab.IsChecked == true;

        satellitesPanel.Visibility = passes ? Visibility.Collapsed : Visibility.Visible;
        passesPanel.Visibility = passes ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPassKindChanged(object sender, RoutedEventArgs e)
    {
        if (elevationRow is null || imagedHint is null)
            return;

        var imaged = imagedRadio.IsChecked == true;

        elevationRow.Visibility = imaged ? Visibility.Collapsed : Visibility.Visible;
        imagedHint.Visibility = imaged ? Visibility.Visible : Visibility.Collapsed;

        ClearPasses();
    }

    // ------------------------------------------------------------------ the region

    private async void OnPickPoint(object sender, RoutedEventArgs e) => await PickRegionAsync(DrawMode.Point);

    private async void OnPickRectangle(object sender, RoutedEventArgs e) => await PickRegionAsync(DrawMode.Rectangle);

    private async void OnPickPolygon(object sender, RoutedEventArgs e) => await PickRegionAsync(DrawMode.Polygon);

    private async Task PickRegionAsync(DrawMode mode)
    {
        if (DataContext is not MapViewModelBase presenter)
            return;

        var drawn = await presenter.GetDrawingAsync(mode);

        if (drawn?.HasNotNullResult() != true)
            return;

        var geometry = drawn.Result!;

        // the map draws in Web Mercator; the search wants longitude/latitude
        Point ToGeographic(Point p) => geometry.Srid == SridHelper.WebMercator ? MapProjects.WebMercatorToGeodeticWgs84(p) : p;

        try
        {
            if (mode == DrawMode.Point)
            {
                var point = geometry.Points?.FirstOrDefault() ?? geometry.Geometries?.FirstOrDefault()?.Points?.FirstOrDefault();

                if (point is not null)
                    SetRegion(PassRegion.FromPoint(ToGeographic(point)));

                return;
            }

            var ring = geometry.Geometries?.FirstOrDefault()?.Points ?? geometry.Points;

            if (ring is not null)
                SetRegion(PassRegion.FromPolygon(ring.Select(ToGeographic)));
        }
        catch (ArgumentException ex)
        {
            // fewer than three corners, or an area across ±180°
            regionText.Text = ex.Message.Split(new[] { " (Parameter" }, StringSplitOptions.None)[0];
        }
    }

    /// <summary>Sets (or with null clears) the region, draws it, and drops passes found for another one.</summary>
    internal void SetRegion(PassRegion? region)
    {
        _source.Region = region;

        ClearPasses();

        clearRegionButton.IsEnabled = region is not null;
        findPassesButton.IsEnabled = region is not null;

        var inv = CultureInfo.InvariantCulture;

        regionText.Text = region switch
        {
            null => "No region yet: pick a point, or draw an area on the map.",
            { IsPoint: true } => string.Format(inv, "Point at {0:F4}° {1}, {2:F4}° {3}",
                Math.Abs(region.Center.Y), region.Center.Y >= 0 ? "N" : "S", Math.Abs(region.Center.X), region.Center.X >= 0 ? "E" : "W"),
            _ => string.Format(inv, "Area of {0} corners around {1:F2}° {2}, {3:F2}° {4}", region.Vertices.Count,
                Math.Abs(region.Center.Y), region.Center.Y >= 0 ? "N" : "S", Math.Abs(region.Center.X), region.Center.X >= 0 ? "E" : "W"),
        };

        _binding?.Refresh();
    }

    private void OnClearRegion(object sender, RoutedEventArgs e) => SetRegion(null);

    // ------------------------------------------------------------------ the search

    private async void OnFindPasses(object sender, RoutedEventArgs e) => await FindPassesAsync();

    private void OnCancelPasses(object sender, RoutedEventArgs e) => _passSearch?.Cancel();

    /// <summary>Searches every listed satellite (for Imaged, those with a known sensor) from the time bar's time.</summary>
    internal async Task FindPassesAsync()
    {
        if (_source.Region is not PassRegion region || _clock is null)
            return;

        var imaged = imagedRadio.IsChecked == true;

        var satellites = _source.Satellites
            .Where(s => !imaged || _source.Sensors.TryGet(s.Elements.CatalogNumberValue, out _))
            .ToList();

        if (satellites.Count == 0)
        {
            passesSummary.Text = imaged ? "None of the listed satellites has a known imager." : "No satellites are listed.";
            return;
        }

        if (satellites.Count > LargeSearch
            && MessageBox.Show(Window.GetWindow(this)!, $"Search {satellites.Count} satellites? This can take a few minutes.", "Passes",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;

        var start = _clock.Time;
        var hours = windowCombo.SelectedItem is ComboBoxItem { Tag: string h } ? double.Parse(h, CultureInfo.InvariantCulture) : 168;
        var end = start.AddHours(hours);
        var minimumElevation = elevationCombo.SelectedItem is ComboBoxItem { Tag: string el } ? double.Parse(el, CultureInfo.InvariantCulture) : 10;
        var sensors = _source.Sensors;

        ClearPasses();

        _passSearch = new CancellationTokenSource();
        var token = _passSearch.Token;

        findPassesButton.IsEnabled = false;
        cancelPassesButton.Visibility = Visibility.Visible;
        passesProgress.Visibility = Visibility.Visible;
        passesProgress.Value = 0;
        passesSummary.Text = $"Searching {satellites.Count} satellite{(satellites.Count == 1 ? "" : "s")}…";

        var progress = new Progress<double>(v => passesProgress.Value = v);

        var search = _passSearch;

        try
        {
            // a cancelled search comes back as null: the cancellation is handled inside the task, so it
            // never travels out of it as an exception (the debugger would stop on it there)
            var passes = await Task.Run(() =>
            {
                var found = new List<SatellitePass>();

                try
                {
                    for (var i = 0; i < satellites.Count; i++)
                    {
                        var satellite = satellites[i];

                        found.AddRange(imaged && sensors.TryGet(satellite.Elements.CatalogNumberValue, out var sensor)
                            ? PassFinder.Imaged(satellite, sensor, region, start, end, token)
                            : PassFinder.Visible(satellite, region, start, end, minimumElevation, token));

                        ((IProgress<double>)progress).Report((i + 1.0) / satellites.Count);
                    }
                }
                catch (OperationCanceledException)
                {
                    return null;
                }

                return found.OrderBy(p => p.Start).ToList();
            });

            // the region or the kind may have changed meanwhile; then this result is no longer wanted
            if (passes is null || token.IsCancellationRequested)
            {
                if (_passSearch == search)
                    passesSummary.Text = "Search cancelled.";
            }
            else
            {
                passesList.ItemsSource = passes.Select(p => new PassRow(p)).ToList();

                passesSummary.Text = Summarize(passes, satellites.Count, hours, imaged);
            }
        }
        finally
        {
            search.Dispose();

            // a newer search may have started meanwhile: leave its state alone
            if (_passSearch == search)
            {
                _passSearch = null;

                findPassesButton.IsEnabled = _source.Region is not null;
                cancelPassesButton.Visibility = Visibility.Collapsed;
                passesProgress.Visibility = Visibility.Collapsed;
            }
        }
    }

    internal static string Summarize(IReadOnlyList<SatellitePass> passes, int satellites, double hours, bool imaged)
    {
        var window = hours < 48 ? $"{hours:0} hours" : $"{hours / 24:0} days";

        var of = $"{satellites} satellite{(satellites == 1 ? "" : "s")}";

        if (passes.Count == 0)
            return $"No {(imaged ? "imaged" : "visible")} passes of {of} in the next {window}.";

        var text = $"{passes.Count} {(imaged ? "imaged" : "visible")} pass{(passes.Count == 1 ? "" : "es")} of {of} in the next {window}";

        text += imaged
            ? $", {passes.Count(p => p.IsDaylight)} in daylight."
            : $", {passes.Count(p => p.IsNakedEyeVisible)} visible to the naked eye.";

        return text + " Times are UTC; choose one to go there.";
    }

    /// <summary>Goes to the pass: the time bar jumps to its peak, its satellite is chosen and the pass is drawn bold.</summary>
    private void OnPassSelected(object sender, SelectionChangedEventArgs e)
    {
        if (passesList.SelectedItem is not PassRow row || _clock is null)
            return;

        _clock.IsLive = false;
        _clock.IsPlaying = false;

        _source.HighlightedPass = row.Pass;

        satelliteList.SelectedItem = (satelliteList.ItemsSource as IEnumerable<SatelliteRow>)?.FirstOrDefault(r => r.Propagator == row.Pass.Satellite);

        _clock.Time = row.Pass.Peak;

        _binding?.Refresh();
    }

    /// <summary>Drops the listed passes and the bold one, e.g. when the region or the satellites change.</summary>
    private void ClearPasses()
    {
        _passSearch?.Cancel();

        if (passesList is null)
            return;

        passesList.ItemsSource = null;
        passesSummary.Text = string.Empty;

        if (_source.HighlightedPass is not null)
        {
            _source.HighlightedPass = null;
            _binding?.Refresh();
        }
    }

    /// <summary>One entry of the pass list.</summary>
    public sealed class PassRow
    {
        public PassRow(SatellitePass pass)
        {
            Pass = pass;

            var inv = CultureInfo.InvariantCulture;

            Name = pass.Name;

            var duration = pass.Duration.TotalHours >= 2
                ? string.Format(inv, "{0:F0} h", pass.Duration.TotalHours)
                : pass.Duration.TotalMinutes >= 1
                    ? string.Format(inv, "{0:F0} min", pass.Duration.TotalMinutes)
                    : string.Format(inv, "{0:F0} s", pass.Duration.TotalSeconds);

            var direction = pass.RiseAzimuth is double rise && pass.SetAzimuth is double set
                ? $"{Compass(rise)} → {Compass(set)}"
                : pass.IsNorthbound ? "northbound" : "southbound";

            Details = pass.CoversWholeWindow
                ? pass.PeakAzimuth is double azimuth
                    ? string.Format(inv, "In view all the time  ·  {0}, {1:F0}°", Compass(azimuth), azimuth)
                    : "In view all the time"
                : string.Format(inv, "{0}{1:MMM d  HH:mm}  ·  {2}  ·  {3}",
                    pass.StartsBeforeWindow ? "under way · " : "", pass.Start, duration, direction);

            Value = pass.Kind == PassKind.Imaged
                ? string.Format(inv, "{0:P0}", pass.CoveredFraction ?? 0).Replace(" ", "")
                : string.Format(inv, "{0:F0}°", pass.PeakElevation);

            SunIcon = pass.IsDaylight ? PackIconMaterialKind.WhiteBalanceSunny : PackIconMaterialKind.WeatherNight;

            SunText = string.Format(inv, "Sun {0:F0}° at the peak: {1}", pass.SunElevation, pass.IsDaylight ? "daylight" : "dark");

            NakedEyeVisibility = pass.IsNakedEyeVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public SatellitePass Pass { get; }

        public string Name { get; }

        public string Details { get; }

        /// <summary>The highest elevation, or the share of the region imaged.</summary>
        public string Value { get; }

        public PackIconMaterialKind SunIcon { get; }

        public string SunText { get; }

        public Visibility NakedEyeVisibility { get; }

        /// <summary>Eight-point compass direction for an azimuth.</summary>
        internal static string Compass(double azimuth)
            => new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[(int)Math.Round(((azimuth % 360) + 360) % 360 / 45.0) % 8];
    }
}

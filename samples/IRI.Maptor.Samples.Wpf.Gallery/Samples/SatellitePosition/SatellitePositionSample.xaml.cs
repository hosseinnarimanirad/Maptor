using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Sources;
using IRI.Maptor.Core.Satellite.Tracks;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.SatellitePosition;

/// <summary>
/// Reads TLEs (or OMM JSON), propagates every satellite with SGP4 and shows where each one is, on a
/// <see cref="MapClock"/> driven by the shared <see cref="MapTimeBar"/>. The chosen satellite also
/// gets its ground track, the area that sees it and, for known imagers, its swath.
/// </summary>
public partial class SatellitePositionSample : UserControl
{
    private const string LayerName = "Satellites";

    private static readonly BoundingBox World = new(-20_000_000, -14_000_000, 20_000_000, 16_000_000);

    /// <summary>Where downloaded elements are kept for offline use.</summary>
    private static readonly string CacheFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IRI.Maptor", "Gallery", "celestrak");

    private readonly SatellitesDataSource _source = new();

    private MapClock? _clock;

    private VectorLayer? _layer;

    private MapClockBinding? _binding;

    private ElementStore? _store;

    private bool _initialized;

    /// <summary>A press that moves further than this is a pan, not a click.</summary>
    private const double ClickTolerance = 4;

    private System.Windows.Point? _mouseDown;

    private bool _resumeLive;

    private bool _resumePlaying;

    public SatellitePositionSample()
    {
        InitializeComponent();

        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>The element store; replaceable so the sample can be exercised without a network.</summary>
    internal ElementStore Store
    {
        get => _store ??= new ElementStore(CacheFolder);
        set => _store = value;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            _clock!.IsLive = _resumeLive;
            _clock.IsPlaying = _resumePlaying;
            return;
        }

        _initialized = true;

        var presenter = new GalleryMapViewModel();

        presenter.InitializeSettings(ProxySettings.Default, BaseMapSettings.Default, new MapSettings { InitialExtent = World }, GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(map, Window.GetWindow(this)!, presenter);

        DataContext = presenter;

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleSatellite;

        _layer = SatelliteLayers.Create(_source);

        presenter.AddLayer(_layer);

        _clock = new MapClock();

        // sets the source's time and redraws the layer in place on every tick, a burst of ticks once
        _binding = new MapClockBinding(presenter, _layer, _clock, time => _source.Time = time);

        _binding.Redrawn += (_, _) => UpdateReadout();

        timeBar.LiveTickInterval = SelectedInterval();

        timeBar.Clock = _clock;

        _clock.IsLive = true;

        LoadSample();
    }

    private void LoadSample()
    {
        _currentGroup = null;

        LoadText(ReadSampleTles(), "the sample file");

        sourceText.Text = "Sample elements downloaded from CelesTrak on 2026-10-28 and 29.";
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_clock is null)
            return;

        _resumeLive = _clock.IsLive;
        _resumePlaying = _clock.IsPlaying;

        _clock.IsLive = false;
        _clock.IsPlaying = false;
    }

    /// <summary>Space plays and pauses unless a control that uses Space has the focus; Esc clears the choice.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && satelliteList.SelectedItem is not null)
        {
            satelliteList.SelectedItem = null;
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Space || _clock is null)
            return;

        if (Keyboard.FocusedElement is TextBox or ComboBox or ComboBoxItem or ButtonBase or DatePicker or ListBoxItem)
            return;

        _clock.IsPlaying = !_clock.IsPlaying;

        e.Handled = true;
    }

    private static string ReadSampleTles()
    {
        using var stream = typeof(SatellitePositionSample).Assembly.GetManifestResourceStream("SatellitePosition.sample.tle")!;

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    // ------------------------------------------------------------------ loading

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open satellite elements",
            Filter = "TLE or OMM JSON (*.tle;*.txt;*.json)|*.tle;*.txt;*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;

        // kept in the store too, so it is there next time (and on a network without internet access)
        var result = Store.Import(dialog.FileName);

        if (Show(result.Elements, result.Errors, Path.GetFileName(dialog.FileName)))
        {
            _currentGroup = null;
            sourceText.Text = result.Message;
        }
    }

    private void OnShowPaste(object sender, RoutedEventArgs e)
    {
        pastePanel.Visibility = Visibility.Visible;
        pasteBox.Focus();
    }

    private void OnCancelPaste(object sender, RoutedEventArgs e)
    {
        pasteBox.Clear();
        pastePanel.Visibility = Visibility.Collapsed;
    }

    private void OnLoadPaste(object sender, RoutedEventArgs e)
    {
        if (LoadText(pasteBox.Text, "the pasted text"))
        {
            _currentGroup = null;
            sourceText.Text = "Pasted elements.";
            OnCancelPaste(sender, e);
        }
    }

    /// <summary>Downloads (or takes from the store) current elements for every listed satellite.</summary>
    private async void OnUpdateFromCelesTrak(object sender, RoutedEventArgs e) => await UpdateFromCelesTrakAsync();

    internal async Task UpdateFromCelesTrakAsync()
    {
        // a group is refreshed as a group: one request, not one per satellite
        if (_currentGroup is not null)
        {
            await LoadGroupAsync(_currentGroup);
            return;
        }

        var numbers = (satelliteList.ItemsSource as IEnumerable<SatelliteRow>)?.Select(r => r.Elements.CatalogNumberValue).Distinct().ToList();

        if (numbers is null || numbers.Count == 0)
            return;

        celestrakButton.IsEnabled = false;

        sourceText.Text = $"Asking CelesTrak for {numbers.Count} satellites…";

        try
        {
            var results = new List<ElementSetResult>();

            foreach (var number in numbers)
                results.Add(await Store.GetAsync(CelesTrakQuery.CatalogNumber(number)));

            var elements = results.SelectMany(r => r.Elements).ToList();

            var errors = results.SelectMany(r => r.Errors).Distinct().ToList();

            sourceText.Text = Summarize(results);

            if (elements.Count > 0)
                Show(elements, errors, "CelesTrak");
        }
        finally
        {
            celestrakButton.IsEnabled = true;
        }
    }

    /// <summary>"7 downloaded at 14:32 UTC" / "7 from the local copy …" / "offline …", in one line.</summary>
    internal static string Summarize(IReadOnlyList<ElementSetResult> results)
    {
        var inv = CultureInfo.InvariantCulture;

        var parts = new List<string>();

        int Count(ElementSource s) => results.Count(r => r.Source == s);

        string When(ElementSource s) => results.Where(r => r.Source == s && r.RetrievedUtc.HasValue)
            .Select(r => r.RetrievedUtc!.Value).DefaultIfEmpty().Min().ToString("yyyy-MM-dd HH:mm 'UTC'", inv);

        if (Count(ElementSource.Downloaded) > 0)
            parts.Add($"{Count(ElementSource.Downloaded)} downloaded from CelesTrak at {When(ElementSource.Downloaded)}");

        if (Count(ElementSource.Cache) > 0)
            parts.Add($"{Count(ElementSource.Cache)} from the local copy of {When(ElementSource.Cache)} (CelesTrak allows one download every 2 hours)");

        if (Count(ElementSource.CacheAfterFailedDownload) > 0)
            parts.Add($"{Count(ElementSource.CacheAfterFailedDownload)} offline: using the copy of {When(ElementSource.CacheAfterFailedDownload)}");

        if (Count(ElementSource.None) > 0)
            parts.Add($"{Count(ElementSource.None)} could not be fetched and have no local copy");

        return string.Join("; ", parts) + ".";
    }

    /// <summary>Reads TLEs or OMM JSON; sets that cannot be read are listed in the banner, the rest are shown.</summary>
    private bool LoadText(string text, string origin)
    {
        var (elements, errors) = ElementStore.Parse(text);

        return Show(elements, errors, origin);
    }

    private bool Show(IReadOnlyList<OrbitalElements> sets, IReadOnlyList<string> errors, string origin)
    {
        errorBanner.Visibility = errors.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        errorText.Text = errors.Count == 0 ? string.Empty
            : $"{errors.Count} problem{(errors.Count == 1 ? "" : "s")} with {origin}. First: {errors[0]}";

        if (sets.Count == 0)
            return false;

        var previous = (satelliteList.SelectedItem as SatelliteRow)?.Elements.CatalogNumberValue;

        var rows = sets.Select(s => new SatelliteRow(s, DateTime.UtcNow)).ToList();

        satelliteList.ItemsSource = rows;

        _source.Satellites = rows.Where(r => r.Propagator is not null).Select(r => r.Propagator!).ToList();
        _source.Selected = null;

        // passes found for the old elements no longer match
        ClearPasses();

        var deepSpace = _source.Satellites.Count(s => s.IsDeepSpace);

        countText.Text = $"{_source.Satellites.Count} shown" + (deepSpace > 0 ? $" · {deepSpace} deep space" : "");

        // keep the same satellite chosen after a refresh
        satelliteList.SelectedItem = rows.FirstOrDefault(r => r.Propagator is not null && r.Elements.CatalogNumberValue == previous)
                                     ?? rows.FirstOrDefault(r => r.Propagator is not null);

        _binding?.Refresh();

        return true;
    }

    // ------------------------------------------------------------------ drawing

    private void OnSatelliteSelected(object sender, SelectionChangedEventArgs e)
    {
        var row = satelliteList.SelectedItem as SatelliteRow;

        _source.Selected = row?.Propagator;

        // another satellite: the bold pass belonged to the previous one
        if (_source.HighlightedPass is not null && _source.HighlightedPass.Satellite != row?.Propagator)
        {
            _source.HighlightedPass = null;
            passesList.SelectedItem = null;
        }

        var hasSensor = row is not null && _source.Sensors.TryGet(row.Elements.CatalogNumberValue, out _);

        swathCheck.IsEnabled = hasSensor;
        swathCheck.ToolTip = hasSensor
            ? "The strip the satellite's imager covers along the track"
            : "No imaging sensor is known for this satellite (see SensorCatalog)";

        if (row is not null)
            satelliteList.ScrollIntoView(row);

        _binding?.Refresh();
    }

    private void OnMapMouseDown(object sender, MouseButtonEventArgs e) => _mouseDown = e.GetPosition(map);

    /// <summary>A click on a satellite chooses it; a click on empty map keeps the choice.</summary>
    private void OnMapMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_mouseDown is not System.Windows.Point down || (e.GetPosition(map) - down).Length > ClickTolerance)
            return;

        _mouseDown = null;

        ChooseOnMap(SatelliteLayers.HitTest(map, _source, e));
    }

    /// <summary>Chooses the satellite that was clicked, and its row in the list; nothing (empty map) keeps the choice.</summary>
    internal void ChooseOnMap(Sgp4Propagator? hit)
    {
        if (hit is null)
            return;

        satelliteList.SelectedItem = (satelliteList.ItemsSource as IEnumerable<SatelliteRow>)?.FirstOrDefault(r => r.Propagator == hit);
    }

    /// <summary>A hand and the satellite's name over a satellite, so it is clear it can be clicked.</summary>
    private void OnMapMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            return;

        var hit = SatelliteLayers.HitTest(map, _source, e);

        var name = hit is null ? null : hit.Elements.Name ?? $"NORAD {hit.Elements.CatalogNumberValue}";

        if (Equals(map.ToolTip, name))
            return;

        // only our own cursor is replaced: the map keeps its tool cursors (identify, measure …)
        if (hit is not null)
            map.Cursor = Cursors.Hand;
        else if (map.Cursor == Cursors.Hand)
            map.ClearValue(CursorProperty);

        map.ToolTip = name;
    }

    private void OnOverlayOptionChanged(object sender, RoutedEventArgs e)
    {
        if (trackCheck is null || footprintCheck is null || swathCheck is null || orbitsCombo is null)
            return;

        _source.ShowTrack = trackCheck.IsChecked == true;
        _source.ShowFootprint = footprintCheck.IsChecked == true;
        _source.ShowSwath = swathCheck.IsChecked == true;
        _source.FutureOrbits = orbitsCombo.SelectedItem is ComboBoxItem { Tag: string orbits } ? double.Parse(orbits, CultureInfo.InvariantCulture) : 2;

        if (labelsCombo?.SelectedItem is ComboBoxItem { Tag: string mode })
            _source.LabelMode = Enum.Parse<SatelliteLabelMode>(mode);

        orbitsCombo.IsEnabled = _source.ShowTrack;

        _binding?.Refresh();
    }

    private void OnIntervalChanged(object sender, SelectionChangedEventArgs e)
    {
        if (timeBar is not null)
            timeBar.LiveTickInterval = SelectedInterval();
    }

    private TimeSpan SelectedInterval()
        => intervalCombo?.SelectedItem is ComboBoxItem { Tag: string seconds }
            ? TimeSpan.FromSeconds(double.Parse(seconds, CultureInfo.InvariantCulture))
            : TimeSpan.FromSeconds(1);

    private void UpdateReadout()
    {
        var row = satelliteList.SelectedItem as SatelliteRow;

        var position = row?.Propagator?.GetPosition(_source.Time);

        readout.Visibility = row is null ? Visibility.Collapsed : Visibility.Visible;

        if (row is null)
            return;

        var inv = CultureInfo.InvariantCulture;

        readoutName.Text = row.Name;
        readoutId.Text = $"NORAD {row.Elements.CatalogNumberValue}" + (row.Elements.InternationalDesignator is string id ? $" · {id}" : "");
        timeValue.Text = _source.Time.ToString("yyyy-MM-dd  HH:mm:ss", inv);

        if (position is null)
        {
            latitudeValue.Text = longitudeValue.Text = altitudeValue.Text = speedValue.Text = groundSpeedValue.Text = footprintValue.Text = "—";
        }
        else
        {
            var p = position.SubSatellitePoint;
            latitudeValue.Text = string.Format(inv, "{0:F4}° {1}", Math.Abs(p.Y), p.Y >= 0 ? "N" : "S");
            longitudeValue.Text = string.Format(inv, "{0:F4}° {1}", Math.Abs(p.X), p.X >= 0 ? "E" : "W");
            altitudeValue.Text = string.Format(inv, "{0:N1} km", position.AltitudeKm);
            speedValue.Text = string.Format(inv, "{0:F2} km/s", position.SpeedKmS);
            groundSpeedValue.Text = string.Format(inv, "{0:F2} km/s", position.GroundSpeedKmS);
            footprintValue.Text = string.Format(inv, "{0:N0} km", Footprint.Of(position, _source.MinimumElevation).RadiusKm);
        }

        periodValue.Text = string.Format(inv, "{0:F1} min", row.Elements.PeriodMinutes);

        sensorValue.Text = _source.Sensors.TryGet(row.Elements.CatalogNumberValue, out var sensor)
            ? string.Format(inv, "{0}, {1:N0} km", sensor.Name, sensor.SwathWidthKm)
            : "—";

        var (text, background, foreground) = AgeLook(row.Elements.Age(_source.Time));

        ageText.Text = text;
        ageText.Foreground = foreground;
        agePill.Background = background;
    }

    /// <summary>SGP4 drifts by roughly 1–3 km a day for a low orbit; past two weeks the position is only a sketch.</summary>
    internal static (string Text, Brush Background, Brush Foreground) AgeLook(TimeSpan age)
    {
        var days = age.TotalDays;

        var text = days < 0
            ? string.Format(CultureInfo.InvariantCulture, "Elements from {0:F1} days in the future", -days)
            : string.Format(CultureInfo.InvariantCulture, "Elements {0:F1} days old", days);

        if (Math.Abs(days) < 3)
            return (text + " · accurate to a few km", Frozen("#DCFCE7"), Frozen("#166534"));

        if (Math.Abs(days) < 14)
            return (text + " · tens of km off", Frozen("#FEF3C7"), Frozen("#92400E"));

        return (text + " · refresh them", Frozen("#FEE2E2"), Frozen("#991B1B"));
    }

    internal static SolidColorBrush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    /// <summary>One entry of the satellite list.</summary>
    public sealed class SatelliteRow
    {
        public SatelliteRow(OrbitalElements elements, DateTime now)
        {
            Elements = elements;

            Propagator = Sgp4Propagator.Create(elements);

            var age = elements.Age(now).TotalDays;

            Name = elements.Name ?? $"NORAD {elements.CatalogNumberValue}";

            Details = string.Format(CultureInfo.InvariantCulture, "{0} · {1:F1} days old", Describe(elements), age);

            ToolTip = string.Format(CultureInfo.InvariantCulture, "NORAD {0} · {1}", elements.CatalogNumberValue, AgeLook(TimeSpan.FromDays(age)).Text);

            AgeBrush = AgeLook(TimeSpan.FromDays(age)).Foreground;
        }

        /// <summary>"GPS · medium orbit, 12 h", "Geostationary", "Low orbit, 93 min": what kind of satellite this is.</summary>
        internal static string Describe(OrbitalElements elements)
        {
            var inv = CultureInfo.InvariantCulture;

            var period = elements.PeriodMinutes < 120
                ? string.Format(inv, "{0:F0} min", elements.PeriodMinutes)
                : string.Format(inv, "{0:F0} h", elements.PeriodMinutes / 60);

            var orbit = OrbitClasses.Of(elements) switch
            {
                OrbitClass.LowEarth => $"low orbit, {period}",
                OrbitClass.MediumEarth => $"medium orbit, {period}",
                OrbitClass.Geostationary => "geostationary",
                OrbitClass.Geosynchronous => "geosynchronous, 24 h",
                _ => $"highly elliptical, {period}",
            };

            var system = Constellation(elements.Name);

            return system is null ? char.ToUpperInvariant(orbit[0]) + orbit.Substring(1) : $"{system} · {orbit}";
        }

        /// <summary>The navigation system a satellite belongs to, from the names CelesTrak uses; null if none.</summary>
        internal static string? Constellation(string? name)
        {
            if (name is null)
                return null;

            if (name.StartsWith("NAVSTAR", StringComparison.OrdinalIgnoreCase) || name.StartsWith("GPS", StringComparison.OrdinalIgnoreCase))
                return "GPS";

            if (name.StartsWith("GSAT0", StringComparison.OrdinalIgnoreCase) || name.Contains("GALILEO", StringComparison.OrdinalIgnoreCase))
                return "Galileo";

            if (name.StartsWith("BEIDOU", StringComparison.OrdinalIgnoreCase))
                return "BeiDou";

            return null;
        }

        public OrbitalElements Elements { get; }

        public Sgp4Propagator Propagator { get; }

        /// <summary>Every orbit can be shown; kept for the list's item style.</summary>
        public bool IsSupported => true;

        public string Name { get; }

        public string Details { get; }

        public string ToolTip { get; }

        public Brush AgeBrush { get; }
    }
}

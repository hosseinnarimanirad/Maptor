using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.DayNight;

/// <summary>
/// Shades the night side of the Earth, with its civil, nautical and astronomical twilight bands, for
/// any date and time. A <see cref="MapClock"/> holds the time; the shared <see cref="MapTimeBar"/> sets it, follows the
/// clock (Live) or runs it forward (Play), and every change redraws the one overlay layer in place.
/// </summary>
public partial class DayNightSample : UserControl
{
    /// <summary>The whole Web Mercator world, a little short of the ±85° edge.</summary>
    private static readonly BoundingBox World = new BoundingBox(-20_000_000, -14_000_000, 20_000_000, 16_000_000);

    private readonly DayNightDataSource _source = new() { IncludeSubSolarPoint = true };

    private MapClock? _clock;

    private VectorLayer? _layer;

    private bool _initialized;

    private MapClockBinding? _binding;

    private bool _resumeLive;

    private bool _resumePlaying;

    private const string PlaceLayerName = "Selected place";

    /// <summary>A press that moves further than this is a pan, not a click.</summary>
    private const double ClickTolerance = 4;

    private System.Windows.Point? _mouseDown;

    public DayNightSample()
    {
        InitializeComponent();

        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>Space plays and pauses, unless a control that uses Space itself has the focus; Esc closes the place panel.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && sunPanel.Visibility == Visibility.Visible)
        {
            ClosePlace();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Space || _clock is null)
            return;

        if (Keyboard.FocusedElement is TextBox or ComboBox or ComboBoxItem or ButtonBase or DatePicker)
            return;

        _clock.IsPlaying = !_clock.IsPlaying;

        e.Handled = true;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            // Loaded fires again each time the gallery re-attaches this view; pick up where we were
            _clock!.IsLive = _resumeLive;
            _clock.IsPlaying = _resumePlaying;
            return;
        }

        _initialized = true;

        var presenter = new GalleryMapViewModel();

        presenter.InitializeSettings(
            ProxySettings.Default,
            BaseMapSettings.Default,
            new MapSettings { InitialExtent = World },
            GeneralSettings.Default);

        await MapInitializationHelper.InitializeMapAsync(map, Window.GetWindow(this)!, presenter);

        DataContext = presenter;

        presenter.SelectedMapProvider = TileMapProviderFactory.GoogleSatellite;

        _layer = DayNightLayers.Create(_source);

        presenter.AddLayer(_layer);

        // Created here, on the UI thread, so its ticks are posted back to this thread; the time bar
        // shows and drives it (and keeps live ticks at 10 s: the shading only creeps along)
        _clock = new MapClock();

        _clock.TimeChanged += (_, _) => OnClockTimeChanged();

        // sets the source's time and redraws the layer in place on every tick, a burst of ticks once
        _binding = new MapClockBinding(presenter, _layer, _clock, time => _source.Time = time);

        _binding.Redrawn += (_, _) => UpdateSubSolarText();

        timeBar.Clock = _clock;

        _clock.IsLive = true;

        _binding.Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_clock is null)
            return;

        // No ticking while the sample is off screen
        _resumeLive = _clock.IsLive;
        _resumePlaying = _clock.IsPlaying;

        _clock.IsLive = false;
        _clock.IsPlaying = false;
    }

    private void OnClockTimeChanged()
    {
        if (_clock is not null && sunPanel.Visibility == Visibility.Visible)
            sunPanel.UpdateTime(_clock.Time);
    }

    private void UpdateSubSolarText()
    {
        var subSolar = Sun.GetSubSolarPoint(_source.Time);

        subSolarText.Text = string.Format(CultureInfo.InvariantCulture,
            "{0:F2}°{1}  {2:F2}°{3}",
            Math.Abs(subSolar.Y), subSolar.Y >= 0 ? "N" : "S",
            Math.Abs(subSolar.X), subSolar.X >= 0 ? "E" : "W");
    }

    private void OnSubSolarChanged(object sender, RoutedEventArgs e)
    {
        _source.IncludeSubSolarPoint = subSolarCheck.IsChecked == true;

        _binding?.Refresh();
    }

    private void OnMapMouseDown(object sender, MouseButtonEventArgs e) => _mouseDown = e.GetPosition(map);

    private void OnMapMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_mouseDown is not System.Windows.Point down || (e.GetPosition(map) - down).Length > ClickTolerance)
            return;

        _mouseDown = null;

        // the map's CurrentPoint is the pointer in longitude/latitude, kept up to date as it moves
        var position = map.CurrentPoint;

        if (double.IsNaN(position.X) || double.IsNaN(position.Y) || Math.Abs(position.Y) > 85.05)
            return;

        SelectPlace(new Point(((position.X + 540) % 360) - 180, position.Y));
    }

    private void SelectPlace(Point place)
    {
        if (_clock is null || DataContext is not MapViewModelBase presenter)
            return;

        presenter.ClearLayer(PlaceLayerName);

        presenter.AddLayer(new SpecialPointLayer(PlaceLayerName,
            new List<Locateable> { new(place, AnchorFunctionHandlers.BottomCenter) { Element = new LocationMarker("☀") } },
            opacity: 1, visibleRange: ScaleInterval.All, type: LayerType.Complex));

        sunPanel.Show(place, _clock.Time);

        sunPanel.Visibility = Visibility.Visible;

        clickHint.Visibility = Visibility.Collapsed;
    }

    private void OnSunPanelClosed(object? sender, EventArgs e) => ClosePlace();

    private void ClosePlace()
    {
        if (DataContext is MapViewModelBase presenter)
            presenter.ClearLayer(PlaceLayerName);

        sunPanel.Visibility = Visibility.Collapsed;

        clickHint.Visibility = Visibility.Visible;
    }
}

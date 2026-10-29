using System;
using System.Windows.Threading;

using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// Keeps a time-dependent layer (day/night, satellites) in step with a <see cref="MapClock"/>: on each
/// tick it sets the source's time and redraws the layer in place, keeping its legend row and settings.
/// </summary>
/// <remarks>
/// Many ticks can arrive between two frames (dragging the time slider, a fast play speed). They are
/// folded into one redraw, run on the dispatcher it was created on once input has been handled.
/// Create it on the UI thread, after the layer has been added to the map.
/// </remarks>
/// <example>
/// <code>
/// var binding = new MapClockBinding(presenter, layer, clock, time => source.Time = time);
/// binding.Redrawn += (_, _) => UpdateReadout();
/// // after changing an option of the source:
/// binding.Refresh();
/// </code>
/// </example>
public sealed class MapClockBinding : IDisposable
{
    private readonly MapViewModelBase _presenter;

    private readonly ILayer _layer;

    private readonly MapClock _clock;

    private readonly Action<DateTime> _setTime;

    private readonly Dispatcher _dispatcher;

    private bool _pending;

    private bool _disposed;

    /// <param name="presenter">The map the layer is on.</param>
    /// <param name="layer">The layer to redraw.</param>
    /// <param name="clock">The clock to follow.</param>
    /// <param name="setTime">Hands the clock's time to the layer's data source.</param>
    public MapClockBinding(MapViewModelBase presenter, ILayer layer, MapClock clock, Action<DateTime> setTime)
    {
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        _layer = layer ?? throw new ArgumentNullException(nameof(layer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _setTime = setTime ?? throw new ArgumentNullException(nameof(setTime));

        _dispatcher = Dispatcher.CurrentDispatcher;

        _clock.TimeChanged += OnTimeChanged;
    }

    /// <summary>Raised after each redraw, with the source already at the clock's time; for read-outs beside the map.</summary>
    public event EventHandler? Redrawn;

    /// <summary>How many redraws have run; ticks folded together count once.</summary>
    public int RedrawCount { get; private set; }

    /// <summary>Redraws soon, without waiting for a tick; call it after changing the source's options.</summary>
    public void Refresh()
    {
        if (_disposed || _pending)
            return;

        _pending = true;

        _dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _pending = false;

            if (!_disposed)
                Redraw();
        }));
    }

    /// <summary>Stops following the clock; the layer stays on the map as last drawn.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _clock.TimeChanged -= OnTimeChanged;
    }

    private void OnTimeChanged(object? sender, EventArgs e) => Refresh();

    private void Redraw()
    {
        _setTime(_clock.Time);

        _presenter.ClearLayer(_layer, remove: false);

        _presenter.AddLayer(_layer);

        RedrawCount++;

        Redrawn?.Invoke(this, EventArgs.Empty);
    }
}

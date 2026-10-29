using System;
using System.Threading;

namespace IRI.Maptor.Presentation.Core.Models;

/// <summary>
/// The instant a map shows time-dependent layers at — day and night, moving satellites — shared so
/// they all agree. It can follow the wall clock (<see cref="IsLive"/>), play forward at a chosen
/// <see cref="Speed"/> (<see cref="IsPlaying"/>), or stand still at a <see cref="Time"/> set by hand.
/// </summary>
/// <remarks>
/// Free of any UI framework: ticks come from a thread-pool timer and are posted to the
/// <see cref="SynchronizationContext"/> the clock was created on, so <see cref="TimeChanged"/> and
/// <see cref="Notifier.PropertyChanged"/> are raised on the UI thread in WPF, MAUI and Blazor alike.
/// A tick that arrives while the previous one is still waiting to run is dropped rather than queued,
/// so a busy UI never falls behind.
/// </remarks>
public sealed class MapClock : Notifier, IDisposable
{
    private static readonly TimeSpan MinimumTickInterval = TimeSpan.FromMilliseconds(50);

    private readonly SynchronizationContext? _context;

    private readonly Func<DateTime> _utcNow;

    private Timer? _timer;

    private int _tickPending;

    private bool _isDisposed;

    public MapClock() : this(() => DateTime.UtcNow)
    {
    }

    /// <param name="utcNow">The wall clock; replaceable for tests.</param>
    public MapClock(Func<DateTime> utcNow)
    {
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));

        _context = SynchronizationContext.Current;

        _time = _utcNow();
    }

    /// <summary>Raised whenever <see cref="Time"/> changes, by hand or by a tick.</summary>
    public event EventHandler? TimeChanged;

    private DateTime _time;

    /// <summary>
    /// The instant shown, in UTC (an Unspecified kind is taken as UTC, a Local one converted). Setting
    /// it by hand leaves live mode, since the map no longer shows "now"; playing continues from it.
    /// </summary>
    public DateTime Time
    {
        get => _time;
        set
        {
            IsLive = false;

            SetTime(value);
        }
    }

    private bool _isLive;

    /// <summary>Follow the wall clock, refreshing every <see cref="TickInterval"/>. Turning it on stops playing.</summary>
    public bool IsLive
    {
        get => _isLive;
        set
        {
            if (_isLive == value)
                return;

            _isLive = value;
            RaisePropertyChanged();

            if (value)
            {
                IsPlaying = false;

                SetTime(_utcNow());
            }

            UpdateTimer();
        }
    }

    private bool _isPlaying;

    /// <summary>Advance <see cref="Time"/> by <see cref="TickInterval"/> × <see cref="Speed"/> on every tick. Turning it on leaves live mode.</summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value)
                return;

            _isPlaying = value;
            RaisePropertyChanged();

            if (value)
                IsLive = false;

            UpdateTimer();
        }
    }

    private double _speed = 60;

    /// <summary>Map seconds per real second while playing; negative plays backwards. 60 by default (a minute a second).</summary>
    public double Speed
    {
        get => _speed;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "The speed must be a finite number.");

            _speed = value;
            RaisePropertyChanged();
        }
    }

    private TimeSpan _tickInterval = TimeSpan.FromSeconds(1);

    /// <summary>How often a live or playing clock ticks: the refresh rate of whatever follows it. One second by default, at least 50 ms.</summary>
    public TimeSpan TickInterval
    {
        get => _tickInterval;
        set
        {
            if (value < MinimumTickInterval)
                throw new ArgumentOutOfRangeException(nameof(value), $"The tick interval must be at least {MinimumTickInterval.TotalMilliseconds} ms.");

            _tickInterval = value;
            RaisePropertyChanged();

            UpdateTimer();
        }
    }

    /// <summary>One tick, as the timer would run it: exposed so the behaviour can be tested without waiting.</summary>
    public void Tick()
    {
        if (_isLive)
            SetTime(_utcNow());
        else if (_isPlaying)
            SetTime(_time + TimeSpan.FromTicks((long)(_tickInterval.Ticks * _speed)));
    }

    public void Dispose()
    {
        _isDisposed = true;

        _timer?.Dispose();

        _timer = null;
    }

    private void SetTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        if (utc == _time)
            return;

        _time = utc;

        RaisePropertyChanged(nameof(Time));

        TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateTimer()
    {
        if (_isDisposed)
            return;

        if (_isLive || _isPlaying)
        {
            if (_timer is null)
                _timer = new Timer(OnTimer, null, _tickInterval, _tickInterval);
            else
                _timer.Change(_tickInterval, _tickInterval);
        }
        else
        {
            _timer?.Dispose();

            _timer = null;
        }
    }

    private void OnTimer(object? state)
    {
        if (Interlocked.Exchange(ref _tickPending, 1) == 1)
            return;

        void Run(object? _)
        {
            try
            {
                if (!_isDisposed)
                    Tick();
            }
            finally
            {
                Interlocked.Exchange(ref _tickPending, 0);
            }
        }

        if (_context is null)
            Run(null);
        else
            _context.Post(Run, null);
    }
}

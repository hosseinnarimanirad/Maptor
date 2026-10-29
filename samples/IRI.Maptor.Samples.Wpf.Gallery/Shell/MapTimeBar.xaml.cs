using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Presentation.Core.Models;
using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Samples.Wpf.Gallery.Shell;

/// <summary>
/// The time controls of a <see cref="MapClock"/>: a large readout with the date, a date picker,
/// month chips, Now, Live and Play with a speed, and a 24-hour timeline. Set <see cref="Clock"/> and
/// the bar both shows and drives it.
/// </summary>
/// <remarks>
/// The bar also sets the clock's tick interval: <see cref="LiveTickInterval"/> while live (how often
/// the map follows the wall clock) and <see cref="PlayTickInterval"/> while playing (how smoothly it
/// animates).
/// </remarks>
public partial class MapTimeBar : UserControl
{
    private readonly RadioButton[] _monthChips = new RadioButton[12];

    /// <summary>Months that hold an equinox or a solstice, worth a hint: that is where the seasons turn.</summary>
    private static readonly Dictionary<int, string> SeasonMarks = new()
    {
        [3] = "March equinox, around the 20th: day and night equal everywhere",
        [6] = "June solstice, around the 21st: midnight sun in the Arctic",
        [9] = "September equinox, around the 22nd: day and night equal everywhere",
        [12] = "December solstice, around the 21st: midnight sun in the Antarctic",
    };

    private MapClock? _clock;

    private bool _updatingControls;

    private TimeSpan _liveTickInterval = TimeSpan.FromSeconds(10);

    public MapTimeBar()
    {
        InitializeComponent();

        CreateMonthChips();
    }

    /// <summary>The clock shown and driven; set it once, on the UI thread.</summary>
    public MapClock? Clock
    {
        get => _clock;
        set
        {
            if (_clock is not null)
            {
                _clock.TimeChanged -= OnClockTimeChanged;
                _clock.PropertyChanged -= OnClockPropertyChanged;
            }

            _clock = value;

            if (_clock is null)
                return;

            _clock.Speed = SelectedSpeed();
            _clock.TimeChanged += OnClockTimeChanged;
            _clock.PropertyChanged += OnClockPropertyChanged;

            ApplyTickInterval();
            SyncModes();
            UpdateTimeControls();
        }
    }

    /// <summary>How often a live clock ticks: the refresh rate of the layers that follow it. 10 s by default.</summary>
    public TimeSpan LiveTickInterval
    {
        get => _liveTickInterval;
        set
        {
            _liveTickInterval = value;
            ApplyTickInterval();
        }
    }

    /// <summary>How often a playing clock ticks: the smoothness of the animation. 200 ms by default.</summary>
    public TimeSpan PlayTickInterval { get; set; } = TimeSpan.FromMilliseconds(200);

    private void CreateMonthChips()
    {
        var names = CultureInfo.InvariantCulture.DateTimeFormat;

        for (int month = 1; month <= 12; month++)
        {
            var chip = new RadioButton
            {
                Content = names.GetAbbreviatedMonthName(month),
                Tag = month,
                Style = (Style)FindResource("MonthChip"),
                ToolTip = SeasonMarks.TryGetValue(month, out var mark) ? mark : names.GetMonthName(month),
            };

            System.Windows.Automation.AutomationProperties.SetName(chip, names.GetMonthName(month));

            chip.Checked += OnMonthChecked;

            _monthChips[month - 1] = chip;

            monthsPanel.Children.Add(chip);
        }
    }

    private void OnClockTimeChanged(object? sender, EventArgs e) => UpdateTimeControls();

    private void OnClockPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MapClock.IsLive) or nameof(MapClock.IsPlaying))
        {
            SyncModes();
            ApplyTickInterval();
        }
    }

    private void ApplyTickInterval()
    {
        if (_clock is null)
            return;

        if (_clock.IsPlaying)
            _clock.TickInterval = PlayTickInterval;
        else if (_clock.IsLive)
            _clock.TickInterval = LiveTickInterval;
    }

    private void SyncModes()
    {
        if (_clock is null)
            return;

        _updatingControls = true;

        liveToggle.IsChecked = _clock.IsLive;
        playToggle.IsChecked = _clock.IsPlaying;

        playIcon.Kind = _clock.IsPlaying ? PackIconMaterialKind.PauseCircleOutline : PackIconMaterialKind.PlayCircleOutline;
        playText.Text = _clock.IsPlaying ? "Pause" : "Play";

        _updatingControls = false;
    }

    private void UpdateTimeControls()
    {
        if (_clock is null)
            return;

        _updatingControls = true;

        var time = _clock.Time;

        datePicker.SelectedDate = time.Date;

        _monthChips[time.Month - 1].IsChecked = true;

        timeSlider.Value = time.TimeOfDay.TotalMinutes;

        timeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        dateText.Text = time.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);

        _updatingControls = false;
    }

    /// <summary>The date picker's day plus the slider's minutes, as a UTC instant.</summary>
    private DateTime ComposeTime()
    {
        var date = datePicker.SelectedDate ?? DateTime.UtcNow.Date;

        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc).AddMinutes(Math.Min(timeSlider.Value, 1440 - 1.0 / 60));
    }

    /// <summary>The same day and time of day in the chosen month (the day clamped to the month's length).</summary>
    private void OnMonthChecked(object sender, RoutedEventArgs e)
    {
        if (_updatingControls || _clock is null || sender is not RadioButton { Tag: int month })
            return;

        var time = _clock.Time;

        var day = Math.Min(time.Day, DateTime.DaysInMonth(time.Year, month));

        _clock.Time = new DateTime(time.Year, month, day, 0, 0, 0, DateTimeKind.Utc) + time.TimeOfDay;
    }

    private void OnDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_updatingControls && _clock is not null)
            _clock.Time = ComposeTime();
    }

    private void OnTimeOfDayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_updatingControls && _clock is not null)
            _clock.Time = ComposeTime();
    }

    private void OnNow(object sender, RoutedEventArgs e)
    {
        if (_clock is not null)
            _clock.Time = DateTime.UtcNow;
    }

    private void OnLiveChanged(object sender, RoutedEventArgs e)
    {
        if (!_updatingControls && _clock is not null)
            _clock.IsLive = liveToggle.IsChecked == true;
    }

    private void OnPlayChanged(object sender, RoutedEventArgs e)
    {
        if (!_updatingControls && _clock is not null)
            _clock.IsPlaying = playToggle.IsChecked == true;
    }

    private void OnSpeedChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_clock is not null)
            _clock.Speed = SelectedSpeed();
    }

    private double SelectedSpeed()
        => speedCombo?.SelectedItem is ComboBoxItem { Tag: string tag } ? double.Parse(tag, CultureInfo.InvariantCulture) : 600;
}

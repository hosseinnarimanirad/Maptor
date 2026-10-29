using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IRI.Maptor.Core.Astronomy.Solar;
using MahApps.Metro.IconPacks;
using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.DayNight;

/// <summary>
/// The Sun at one place: the light right now, its height through the local day, and the day's
/// events (sunrise, solar noon, sunset, day length, and the three twilights), all from
/// <see cref="Sun.GetSolarDay(Point, DateTime, TimeSpan)"/>.
/// </summary>
/// <remarks>
/// A place has no time zone the sample could know offline, so the panel offers three: the zone its
/// longitude falls in (whole hours, as at sea), this computer's zone, and UTC.
/// </remarks>
public partial class SunInfoPanel : UserControl
{
    private const int ChartStepMinutes = 10;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly string[] CompassPoints =
        { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };

    private Point? _place;

    private DateTime _utc;

    private bool _rebuildingZones;

    /// <summary>Sun elevation every <see cref="ChartStepMinutes"/> through the local day, for the chart.</summary>
    private readonly List<double> _elevations = new();

    private double _nowMinutes;

    public SunInfoPanel()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    public Point? Place => _place;

    /// <summary>Shows the Sun at <paramref name="place"/> (X = longitude, Y = latitude) at <paramref name="utc"/>.</summary>
    public void Show(Point place, DateTime utc)
    {
        _place = place;

        _utc = utc;

        RebuildZones();

        Refresh();
    }

    /// <summary>Follows the map clock; cheap enough to run on every tick.</summary>
    public void UpdateTime(DateTime utc)
    {
        _utc = utc;

        if (_place is not null)
            Refresh();
    }

    private TimeSpan SelectedOffset()
    {
        if (_place is null)
            return TimeSpan.Zero;

        return zoneCombo.SelectedIndex switch
        {
            1 => TimeZoneInfo.Local.GetUtcOffset(_utc),
            2 => TimeSpan.Zero,
            _ => LongitudeOffset(_place.X),
        };
    }

    /// <summary>The whole-hour zone a longitude lies in: 15° per hour, centred on the Greenwich meridian.</summary>
    private static TimeSpan LongitudeOffset(double longitude)
        => TimeSpan.FromHours(Math.Max(-12, Math.Min(12, Math.Round(longitude / 15.0))));

    private void RebuildZones()
    {
        var selected = zoneCombo.SelectedIndex < 0 ? 0 : zoneCombo.SelectedIndex;

        _rebuildingZones = true;

        zoneCombo.Items.Clear();
        zoneCombo.Items.Add($"Place, from longitude ({FormatOffset(LongitudeOffset(_place!.X))})");
        zoneCombo.Items.Add($"This computer ({FormatOffset(TimeZoneInfo.Local.GetUtcOffset(_utc))})");
        zoneCombo.Items.Add("UTC");
        zoneCombo.SelectedIndex = selected;

        _rebuildingZones = false;
    }

    private void Refresh()
    {
        if (_place is not Point place)
            return;

        var offset = SelectedOffset();

        var localNow = _utc + offset;

        var day = Sun.GetSolarDay(place, localNow.Date, offset);

        var now = Sun.GetHorizontal(place, _utc);

        placeText.Text = string.Format(Invariant, "{0:F4}° {1}   {2:F4}° {3}",
            Math.Abs(place.Y), place.Y >= 0 ? "N" : "S", Math.Abs(place.X), place.X >= 0 ? "E" : "W");

        ShowPhase(SolarPhases.FromElevation(now.Elevation));

        nowText.Text = string.Format(Invariant, "{0:HH:mm}  ·  Sun {1:F1}° {2} the horizon, toward {3:F0}° {4}",
            localNow, Math.Abs(now.Elevation), now.Elevation >= 0 ? "above" : "below", now.Azimuth, Compass(now.Azimuth));

        dayHeader.Text = localNow.ToString("dddd d MMMM yyyy", Invariant).ToUpperInvariant();

        sunriseText.Text = FormatTime(day.Sunrise, day.Date);
        sunriseNote.Text = day.SunriseAzimuth is double rise ? FormatAzimuth(rise) : string.Empty;

        noonText.Text = FormatTime(day.SolarNoon, day.Date);
        noonNote.Text = string.Format(Invariant, "{0:F1}° high", day.SolarNoonElevation);

        sunsetText.Text = FormatTime(day.Sunset, day.Date);
        sunsetNote.Text = day.SunsetAzimuth is double set ? FormatAzimuth(set) : string.Empty;

        dayLengthText.Text = day.DayLength is TimeSpan length ? FormatDuration(length) : "—";

        civilText.Text = FormatTwilight(day.CivilTwilight, day.Date);
        nauticalText.Text = FormatTwilight(day.NauticalTwilight, day.Date);
        astronomicalText.Text = FormatTwilight(day.AstronomicalTwilight, day.Date);

        polarBanner.Visibility = day.IsPolarDay || day.IsPolarNight ? Visibility.Visible : Visibility.Collapsed;
        polarText.Text = day.IsPolarDay
            ? "Midnight sun: the Sun stays above the horizon all day."
            : day.IsPolarNight ? "Polar night: the Sun does not rise today." : string.Empty;

        // The chart covers the local calendar day in the chosen zone
        var start = DateTime.SpecifyKind(localNow.Date - offset, DateTimeKind.Utc);

        _elevations.Clear();

        for (int minutes = 0; minutes <= 1440; minutes += ChartStepMinutes)
            _elevations.Add(Sun.GetHorizontal(place, start.AddMinutes(minutes)).Elevation);

        _nowMinutes = localNow.TimeOfDay.TotalMinutes;

        DrawChart();
    }

    private void ShowPhase(SolarPhase phase)
    {
        (string text, string background, string foreground, PackIconMaterialKind icon) = phase switch
        {
            SolarPhase.Day => ("Daylight", "#FEF3C7", "#92400E", PackIconMaterialKind.WeatherSunny),
            SolarPhase.CivilTwilight => ("Civil twilight", "#E0E7FF", "#3730A3", PackIconMaterialKind.WeatherSunset),
            SolarPhase.NauticalTwilight => ("Nautical twilight", "#C7D2FE", "#312E81", PackIconMaterialKind.WeatherNightPartlyCloudy),
            SolarPhase.AstronomicalTwilight => ("Astronomical twilight", "#3730A3", "#E0E7FF", PackIconMaterialKind.StarOutline),
            _ => ("Night", "#0B1A3A", "#E0E7FF", PackIconMaterialKind.WeatherNight),
        };

        phaseChip.Background = Brush(background);
        phaseText.Foreground = Brush(foreground);
        phaseIcon.Foreground = Brush(foreground);
        phaseIcon.Kind = icon;
        phaseText.Text = text;
    }

    private void DrawChart()
    {
        var width = chart.ActualWidth;

        var height = chart.ActualHeight;

        if (width <= 0 || height <= 0 || _elevations.Count == 0)
            return;

        // Fixed ±90° scale, so charts of different places and dates compare honestly
        double Y(double elevation) => height / 2 - elevation / 90.0 * (height / 2 - 4);

        double X(double minutes) => minutes / 1440.0 * width;

        var horizon = Y(0);

        Canvas.SetTop(nightShade, horizon);
        nightShade.Width = width;
        nightShade.Height = Math.Max(0, height - horizon);

        horizonLine.X1 = 0;
        horizonLine.X2 = width;
        horizonLine.Y1 = horizonLine.Y2 = horizon;

        var points = new PointCollection(_elevations.Count);

        for (int i = 0; i < _elevations.Count; i++)
            points.Add(new System.Windows.Point(X(i * ChartStepMinutes), Y(_elevations[i])));

        points.Freeze();

        elevationCurve.Points = points;

        var nowX = X(_nowMinutes);

        nowLine.X1 = nowLine.X2 = nowX;
        nowLine.Y1 = 0;
        nowLine.Y2 = height;

        // Interpolate the curve under the marker so the dot sits on it between samples
        var index = Math.Min(_elevations.Count - 2, (int)(_nowMinutes / ChartStepMinutes));
        var fraction = _nowMinutes / ChartStepMinutes - index;
        var nowElevation = _elevations[index] + (_elevations[index + 1] - _elevations[index]) * fraction;

        Canvas.SetLeft(nowDot, nowX - nowDot.Width / 2);
        Canvas.SetTop(nowDot, Y(nowElevation) - nowDot.Height / 2);
    }

    private void OnChartSizeChanged(object sender, SizeChangedEventArgs e) => DrawChart();

    private void OnZoneChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_rebuildingZones)
            Refresh();
    }

    private void OnClose(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>"05:49", with "+1"/"−1" when the event falls on the next or previous calendar day.</summary>
    private static string FormatTime(DateTimeOffset? time, DateTime date)
    {
        if (time is not DateTimeOffset value)
            return "—";

        var text = value.ToString("HH:mm", Invariant);

        var dayShift = (value.Date - date.Date).Days;

        return dayShift switch
        {
            0 => text,
            > 0 => text + " +" + dayShift,
            _ => text + " −" + (-dayShift),
        };
    }

    private static string FormatTwilight(AltitudeEvents events, DateTime date)
    {
        if (events.IsAlwaysAbove)
            return "lasts all night";

        if (events.IsAlwaysBelow)
            return "none";

        return FormatTime(events.Rising, date) + "  –  " + FormatTime(events.Setting, date);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var minutes = (int)Math.Round(duration.TotalMinutes);

        return string.Format(Invariant, "{0} h {1:00} min", minutes / 60, minutes % 60);
    }

    private static string FormatAzimuth(double azimuth) => string.Format(Invariant, "{0:F0}° {1}", azimuth, Compass(azimuth));

    private static string Compass(double azimuth) => CompassPoints[(int)Math.Round(azimuth / 22.5) % 16];

    private static string FormatOffset(TimeSpan offset)
    {
        if (offset == TimeSpan.Zero)
            return "UTC";

        var sign = offset < TimeSpan.Zero ? "−" : "+";

        var value = offset.Duration();

        return value.Minutes == 0
            ? $"UTC{sign}{value.Hours}"
            : string.Format(Invariant, "UTC{0}{1}:{2:00}", sign, value.Hours, value.Minutes);
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        brush.Freeze();

        return brush;
    }
}

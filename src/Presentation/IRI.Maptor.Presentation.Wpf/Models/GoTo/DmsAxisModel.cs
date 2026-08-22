using System;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Presentation.Wpf.Models.GoTo;

/// <summary>
/// One geodetic axis entered as unsigned degrees / minutes / seconds plus a hemisphere
/// letter — the way a GPS receiver or a survey sheet states it. The sign lives in the
/// hemisphere, never in the numbers, so a southern latitude reads "35° 41′ 23″ S" rather
/// than "-35° -41′ -23″" as the previous editor showed it.
/// </summary>
public class DmsAxisModel : Notifier
{
    private const int SecondDecimals = 3;

    private bool _suspendNotifications;

    public DmsAxisModel(bool isLatitude)
    {
        IsLatitude = isLatitude;

        HemisphereOptions = isLatitude ? new[] { "N", "S" } : new[] { "E", "W" };

        _hemisphere = HemisphereOptions[0];
    }

    public bool IsLatitude { get; }

    /// <summary>Largest whole degree the axis accepts (90 for latitude, 180 for longitude).</summary>
    public int MaxDegrees => IsLatitude ? 90 : 180;

    public string[] HemisphereOptions { get; }

    private double _degrees;
    public double Degrees
    {
        get => _degrees;
        set
        {
            if (_degrees == value)
                return;

            _degrees = value;
            RaisePropertyChanged();
            OnComponentChanged();
        }
    }

    private double _minutes;
    public double Minutes
    {
        get => _minutes;
        set
        {
            if (_minutes == value)
                return;

            _minutes = value;
            RaisePropertyChanged();
            OnComponentChanged();
        }
    }

    private double _seconds;
    public double Seconds
    {
        get => _seconds;
        set
        {
            if (_seconds == value)
                return;

            _seconds = value;
            RaisePropertyChanged();
            OnComponentChanged();
        }
    }

    private string _hemisphere;
    /// <summary>"N" / "S" for latitude, "E" / "W" for longitude.</summary>
    public string Hemisphere
    {
        get => _hemisphere;
        set
        {
            if (_hemisphere == value || string.IsNullOrEmpty(value))
                return;

            _hemisphere = value;
            RaisePropertyChanged();
            OnComponentChanged();
        }
    }

    public bool IsNegative => Hemisphere == "S" || Hemisphere == "W";

    /// <summary>
    /// Signed decimal degrees. Reading combines the components; writing decomposes the value
    /// and raises one <see cref="ValueChanged"/> at the end rather than one per component.
    /// </summary>
    public double Value
    {
        get
        {
            var magnitude = Math.Abs(Degrees) + Minutes / 60.0 + Seconds / 3600.0;

            return IsNegative ? -magnitude : magnitude;
        }
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return;

            DegreeHelper.ToDmsComponents(value, SecondDecimals, out var isNegative, out var degree, out var minute, out var second);

            _suspendNotifications = true;

            try
            {
                Degrees = degree;
                Minutes = minute;
                Seconds = second;
                Hemisphere = isNegative ? HemisphereOptions[1] : HemisphereOptions[0];
            }
            finally
            {
                _suspendNotifications = false;
            }

            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(IsNegative));
            RaisePropertyChanged(nameof(IsValid));
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// True when every component is inside its range and the combined angle is inside the
    /// axis range. Degrees may equal the maximum only when minutes and seconds are zero.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (Degrees < 0 || Minutes < 0 || Seconds < 0)
                return false;

            if (Minutes >= 60 || Seconds >= 60)
                return false;

            if (Degrees % 1 != 0 || Minutes % 1 != 0)
                return false;

            return Math.Abs(Value) <= MaxDegrees;
        }
    }

    public event EventHandler? ValueChanged;

    private void OnComponentChanged()
    {
        if (_suspendNotifications)
            return;

        RaisePropertyChanged(nameof(Value));
        RaisePropertyChanged(nameof(IsNegative));
        RaisePropertyChanged(nameof(IsValid));
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}

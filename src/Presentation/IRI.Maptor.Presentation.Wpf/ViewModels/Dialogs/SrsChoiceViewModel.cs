using System;
using System.Collections.ObjectModel;

using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Models.DxfOpenDialog;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

/// <summary>
/// The user's choice of a coordinate system for data that does not give one: WGS84, a UTM zone (north or south) or
/// Web Mercator, as an SRID (<see cref="EffectiveSrid"/>). The same options and rules as the import options dialog;
/// shown by <c>SrsChoiceView</c>.
/// </summary>
public class SrsChoiceViewModel : Notifier
{
    private SrsOption _selectedSrsOption;

    private int _utmZone = 39;

    private bool _isNorthHemisphere = true;

    public SrsChoiceViewModel()
    {
        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            new() { DisplayName = "UTM (user-defined zone)", IsUtm = true },
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        _selectedSrsOption = AvailableSrsOptions[0];
    }

    /// <summary>Raised when <see cref="EffectiveSrid"/> may have changed.</summary>
    public event EventHandler? Changed;

    public ObservableCollection<SrsOption> AvailableSrsOptions { get; }

    public SrsOption SelectedSrsOption
    {
        get => _selectedSrsOption;
        set
        {
            // WPF can push null (or the same item) back while the list changes: that is not a choice
            if (value is null || ReferenceEquals(value, _selectedSrsOption))
                return;

            _selectedSrsOption = value;
            RaisePropertyChanged();
            OnChanged();
        }
    }

    public int UtmZone
    {
        get => _utmZone;
        set { _utmZone = Math.Clamp(value, 1, 60); RaisePropertyChanged(); OnChanged(); }
    }

    public bool IsNorthHemisphere
    {
        get => _isNorthHemisphere;
        set { _isNorthHemisphere = value; RaisePropertyChanged(); OnChanged(); }
    }

    public bool IsUtmSelected => SelectedSrsOption.IsUtm;

    public int EffectiveSrid => SelectedSrsOption.IsUtm
        ? (_isNorthHemisphere ? SridHelper.GetUtmSrid(_utmZone) : SridHelper.GetUtmSouthSrid(_utmZone))
        : SelectedSrsOption.FixedSrid ?? 0;

    private void OnChanged()
    {
        RaisePropertyChanged(nameof(IsUtmSelected));
        RaisePropertyChanged(nameof(EffectiveSrid));

        Changed?.Invoke(this, EventArgs.Empty);
    }
}

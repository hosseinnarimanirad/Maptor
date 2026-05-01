using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.DxfOpenDialog;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;


public class ExportVectorDialogViewModel : DialogViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly SrsOption _utmOption;
    private string _filePath = string.Empty;
    private string _rawText = string.Empty;
    private int _utmZone = 39;
    private bool _utmHemisphereNorth = true;
    private bool _isLongitudeFirst = true;

    private DataSourceKind _selectedDataSourceKind;
    public DataSourceKind SelectedDataSourceKind
    {
        get { return _selectedDataSourceKind; }
        set
        {
            _selectedDataSourceKind = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowXYOrderOptions));
        }
    }

    private ExportFeatureMode _selectedExportMode;
    public ExportFeatureMode SelectedExportMode
    {
        get { return _selectedExportMode; }
        set
        {
            _selectedExportMode = value;
            RaisePropertyChanged();
        }
    }


    // Coordinate System
    private bool _useSourceSrs;
    public bool UseSourceSrs
    {
        get { return _useSourceSrs; }
        set
        {
            _useSourceSrs = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EnableProjection));
        }
    }

    public bool EnableProjection => !UseSourceSrs;

    public ObservableCollection<SrsOption> AvailableSrsOptions { get; }
     
    private SrsOption? _selectedSrsOption;
    public SrsOption? SelectedSrsOption
    {
        get => _selectedSrsOption;
        set
        {
            _selectedSrsOption = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsUtmSelected));
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public int EffectiveSelectedSrid
    {
        get
        {
            if (SelectedSrsOption == null)
                return 0;
            if (SelectedSrsOption.IsUtm)
                return _utmHemisphereNorth ? SridHelper.GetUtmSrid(_utmZone) : SridHelper.GetUtmSouthSrid(_utmZone);
            return SelectedSrsOption.FixedSrid ?? 0;
        }
    }

    // UTM
    public int UtmZone
    {
        get => _utmZone;
        set
        {
            _utmZone = Math.Clamp(value, 1, 60);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public bool UtmHemisphereNorth
    {
        get => _utmHemisphereNorth;
        set
        {
            _utmHemisphereNorth = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public bool IsUtmSelected => SelectedSrsOption?.IsUtm == true;

    public bool ShowXYOrderOptions => SelectedDataSourceKind == DataSourceKind.GeoJson;

    // Others
    public string FilePath
    {
        get => _filePath;
        set { _filePath = value ?? string.Empty; RaisePropertyChanged(); }
    }

    /// <summary>
    /// True = X,Y (longitude,latitude) order; False = Y,X.
    /// </summary>
    public bool IsLongitudeFirst
    {
        get => _isLongitudeFirst;
        set { _isLongitudeFirst = value; RaisePropertyChanged(); }
    }


    public ExportVectorDialogViewModel(IDialogService dialogService, int? initialSrid = null)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _utmOption = new SrsOption { DisplayName = "UTM (user-defined zone)", IsUtm = true };
        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            _utmOption,
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        SelectedSrsOption = AvailableSrsOptions[0];

        if (initialSrid.HasValue && initialSrid.Value > 0)
            ApplyInitialSrid(initialSrid.Value);

        BrowseCommand = new RelayCommand(_ => Browse());
        SaveCommand = new RelayCommand(_ => Open(), _ => CanOpen());
        CancelCommand = new RelayCommand(_ => Cancel());
    }
     
    public RelayCommand BrowseCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
     
    /// <summary>
    /// Set when user clicks Open. Contains the full result for import.
    /// </summary>
    public ExportVectorDialogResult? Result { get; private set; }

    private void ApplyInitialSrid(int srid)
    {
        if (srid >= 32601 && srid <= 32660)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = srid - 32600;
            UtmHemisphereNorth = true;
        }
        else if (srid >= 32701 && srid <= 32760)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = srid - 32700;
            UtmHemisphereNorth = false;
        }
        else
        {
            var match = AvailableSrsOptions.FirstOrDefault(o => !o.IsUtm && o.FixedSrid == srid);
            if (match != null)
                SelectedSrsOption = match;
        }
    }

    private void Browse()
    {
        
    }

    private bool CanOpen()
    {
        //if (string.IsNullOrWhiteSpace(RawText))
        //    return false;

        if (EffectiveSelectedSrid <= 0)
            return false;

        if (IsUtmSelected && (UtmZone < 1 || UtmZone > 60))
            return false;

        return true;
    }
     
    private void Open()
    {
        if (!CanOpen())
            return;
         
        Result = new ExportVectorDialogResult(
            string.IsNullOrEmpty(FilePath) ? null : FilePath, 
            EffectiveSelectedSrid,
            IsLongitudeFirst);

        DialogResult = true;

        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        DialogResult = null;
        RequestClose?.Invoke();
    }
}

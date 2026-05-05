using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IRI.Maptor.Jab.Common.Abstractions;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.DxfOpenDialog;
using IRI.Maptor.Jab.Common.ViewModels.Dialogs;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Common.ViewModels.LayerSettings;

public class LayerSettings_VectorExportViewModel : ViewModelBase
{
    //public Action? RequestExport;
    //private readonly IDialogService _dialogService;

    //private string _filePath = string.Empty;

    private DataSourceKind _selectedDataSourceKind;
    public DataSourceKind SelectedDataSourceKind
    {
        get { return _selectedDataSourceKind; }
        set
        {
            _selectedDataSourceKind = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowSrsOptions));
            RaisePropertyChanged(nameof(ShowXYOrderOptions));
        }
    }

    private ExportFeatureMode _selectedExportMode = ExportFeatureMode.All;
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
    public bool ShowSrsOptions => SelectedDataSourceKind == DataSourceKind.Shapefile || SelectedDataSourceKind == DataSourceKind.Dxf;

    private readonly SrsOption _utmOption;
    private readonly MapViewModelBase viewModel;
    private readonly VectorLayer layer;
    private bool _isProjectionEnabled;
    public bool IsProjectionEnabled
    {
        get { return _isProjectionEnabled; }
        set
        {
            _isProjectionEnabled = value;
            RaisePropertyChanged();
        }
    }

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
    private int _utmZone = 39;
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

    private bool _utmHemisphereNorth = true;
    public bool IsNorthHemisphere
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

    private bool _isLongitudeFirst;
    /// <summary>
    /// True = X,Y (longitude,latitude) order; False = Y,X.
    /// </summary>
    public bool IsLongitudeFirst
    {
        get => _isLongitudeFirst;
        set { _isLongitudeFirst = value; RaisePropertyChanged(); }
    }

    ///// <summary>
    ///// Set when user clicks Open. Contains the full result for import.
    ///// </summary>
    //public LayerSettings_ExportResult? Result { get; private set; }


    public LayerSettings_VectorExportViewModel(MapViewModelBase viewModel, VectorLayer layer, /*IDialogService dialogService,*/ int? initialSrid = null)
    {
        //_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _utmOption = new SrsOption { DisplayName = "UTM (user-defined zone)", IsUtm = true };
        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            _utmOption,
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        SelectedSrsOption = AvailableSrsOptions[0];

        SelectedDataSourceKind = DataSourceKind.Shapefile;

        if (initialSrid.HasValue && initialSrid.Value > 0)
            ApplyInitialSrid(initialSrid.Value);

        ExportCommand = new RelayCommand(_ => Export(), _ => CanExport());
        this.viewModel = viewModel;
        this.layer = layer;
    }

    public RelayCommand ExportCommand { get; }

    private void ApplyInitialSrid(int srid)
    {
        //if (srid >= 32601 && srid <= 32660)
        //{
        //    SelectedSrsOption = _utmOption;
        //    UtmZone = srid - 32600;
        //    IsNorthHemisphere = true;
        //}
        //else if (srid >= 32701 && srid <= 32760)
        //{
        //    SelectedSrsOption = _utmOption;
        //    UtmZone = srid - 32700;
        //    IsNorthHemisphere = false;
        //}

        var utmZoneInfo = SridHelper.GetUtmZone(srid);

        if (utmZoneInfo.isUtm)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = utmZoneInfo.zone!.Value;
            IsNorthHemisphere = utmZoneInfo.isNorthHemisphere!.Value;
        }
        else
        {
            var match = AvailableSrsOptions.FirstOrDefault(o => !o.IsUtm && o.FixedSrid == srid);
            if (match != null)
                SelectedSrsOption = match;
        }
    }

    private bool CanExport()
    {
        if (IsProjectionEnabled)
            return true;

        if (SelectedDataSourceKind < 0)
            return false;

        if (EffectiveSelectedSrid <= 0)
            return false;

        if (IsUtmSelected && (UtmZone < 1 || UtmZone > 60))
            return false;

        return true;
    }

    private async Task Export()
    {
        if (!CanExport())
            return;

        //this.RequestExport?.Invoke();

        //Result = new LayerSettings_ExportResult(
        //    string.IsNullOrEmpty(FilePath) ? null : FilePath,
        //    EffectiveSelectedSrid,
        //    IsLongitudeFirst);

        //DialogResult = true;

        //RequestClose?.Invoke();

        var fileName = await viewModel.DialogService.ShowSaveFileDialogAsync(SelectedDataSourceKind, null, layer.LayerName);

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        // prepare output srs
        var features = await layer.GetFeaturesAsync();

        if (features is null)
            return;

        var targetSrs = SelectedDataSourceKind switch
        {
            //DataSourceKind.GeoJson => SrsBases.GeodeticWgs84,
            //DataSourceKind.TopoJson => SrsBases.GeodeticWgs84,
            DataSourceKind.Shapefile => SrsBase.Create(this.EffectiveSelectedSrid),
            DataSourceKind.Dxf => SrsBase.Create(this.EffectiveSelectedSrid),
            _ => SrsBases.GeodeticWgs84
        };

        if (targetSrs is null)
            return;

        var targetFeatureSet = features.Project(targetSrs);

        targetFeatureSet.Export(fileName, SelectedDataSourceKind, targetSrs, IsLongitudeFirst);

        await viewModel.DialogService.ShowMessage_DoneSuccessfully();
    }

}
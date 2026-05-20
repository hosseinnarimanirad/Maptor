using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Jab.Common.Services;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Common.Models.DxfOpenDialog;
using IRI.Maptor.Sta.Common.Exceptions;
using System.IO;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

public class PointDisplay
{
    public int Index { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
}

public class DxfOpenDialogViewModel : DialogViewModelBase
{
    //private readonly IDialogService _dialogService;
    private readonly SrsOption _utmOption;


    private ObservableCollection<PointDisplay> _samplePoints = new();
    public ObservableCollection<PointDisplay> SamplePoints
    {
        get => _samplePoints;
        set
        {
            _samplePoints = value ?? new ObservableCollection<PointDisplay>();
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


    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set { _filePath = value ?? string.Empty; RaisePropertyChanged(); }
    }


    private bool _canUserChooseSrs = true;
    public bool CanUserChooseSrs
    {
        get { return _canUserChooseSrs; }
        set
        {
            _canUserChooseSrs = value;
            RaisePropertyChanged();
        }
    }


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


    private bool _isNorthHemisphere = true;
    public bool IsNorthHemisphere
    {
        get => _isNorthHemisphere;
        set
        {
            _isNorthHemisphere = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
        }
    }

    public bool IsUtmSelected => SelectedSrsOption?.IsUtm == true;

    public int EffectiveSelectedSrid
    {
        get
        {
            if (SelectedSrsOption == null)
                return 0;
            if (SelectedSrsOption.IsUtm)
                return _isNorthHemisphere ? SridHelper.GetUtmSrid(_utmZone) : SridHelper.GetUtmSouthSrid(_utmZone);
            return SelectedSrsOption.FixedSrid ?? 0;
        }
    }


    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; RaisePropertyChanged(); }
    }

    public DxfOpenDialogViewModel(IDialogService dialogService)
    {
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _utmOption = new SrsOption { DisplayName = "UTM (user-defined zone)", IsUtm = true };
        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            _utmOption,
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        SelectedSrsOption = AvailableSrsOptions[0];

        BrowseCommand = new RelayCommand(async _ => await BrowseAsync());
        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen());
        CancelCommand = new RelayCommand(_ => Cancel());
        RemoveFileCommand = new RelayCommand(_ => RemoveSelectedFile(), _ => !string.IsNullOrEmpty(FilePath));

    }


    private void ApplyInitialSrid(int srid)
    { 
        var utmZoneInfo = SridHelper.GetUtmZone(srid);

        if (utmZoneInfo.isUtm)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = utmZoneInfo.zone!.Value;
            IsNorthHemisphere = utmZoneInfo.isNorthHemisphere!.Value;
        }
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
        else
        {
            var match = AvailableSrsOptions.FirstOrDefault(o => !o.IsUtm && o.FixedSrid == srid);

            if (match != null)
                SelectedSrsOption = match;
        }
    }

    private async Task BrowseAsync()
    {
        var path = await DialogService.ShowOpenFileDialogAsync(DataSourceKind.Dxf/*"Drawing Exchange Format (DXF)|*.dxf"*/, null);

        if (string.IsNullOrEmpty(path))
            return;

        FilePath = path;

        await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
            return;

        IsLoading = true;

        try
        {
            var preview = await DxfReader.GetPreviewAsync(FilePath, 50);

            SamplePoints = new ObservableCollection<PointDisplay>(
                preview.SamplePoints.Select((p, i) => new PointDisplay { Index = i + 1, X = p.X, Y = p.Y }));

            if (preview.DetectedSrid > 0)
            {
                ApplyInitialSrid(preview.DetectedSrid);

                CanUserChooseSrs = false;
            }
            else
            {
                CanUserChooseSrs = true;
            }
        }
        catch (Exception ex)
        {
            await ShowExceptionMessageAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanOpen()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return false;

        if (EffectiveSelectedSrid <= 0)
            return false;

        if (IsUtmSelected && (UtmZone < 1 || UtmZone > 60))
            return false;

        return true;
    }

    private void RemoveSelectedFile()
    {
        this.FilePath = string.Empty;
        this.SamplePoints = new ObservableCollection<PointDisplay>();
    }

    private void Open()
    {
        if (!CanOpen())
            return;
        DialogResult = true;
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke();
    }


    private async Task ShowExceptionMessageAsync(Exception ex)
    {
        if (ex is DomainException domainException)
        {
            await this.DialogService.ShowErrorMessage(domainException);
        }
        else if (ex is IOException)
        {
            await this.DialogService.ShowErrorMessage(MaptorLockedFileException.Instance);
        }
        else if (ex is UnauthorizedAccessException)
        {
            await this.DialogService.ShowErrorMessage(MaptorLockedFileException.Instance);
        }
        else
        {
            await this.DialogService.ShowErrorMessage(new MaptorUnknownException(ex.Message));
        }
    }

    #region Commands

    public RelayCommand BrowseCommand { get; }

    public RelayCommand OpenCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand RemoveFileCommand { get; }

    #endregion

}

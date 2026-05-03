using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.DxfOpenDialog;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

public class PointDisplay
{
    public int Index { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
}

public class DxfOpenDialogViewModel : DialogViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly SrsOption _utmOption;
    private string _filePath = string.Empty;
    private SrsOption? _selectedSrsOption;
    private int _utmZone = 39;
    private bool _utmHemisphereNorth = true;
    private bool _isLoading;
    private ObservableCollection<PointDisplay> _samplePoints = new();

    public DxfOpenDialogViewModel(IDialogService dialogService, int? initialSrid = null)
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

        BrowseCommand = new RelayCommand(async _ => await BrowseAsync());
        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen());
        CancelCommand = new RelayCommand(_ => Cancel());
        RemoveFileCommand = new RelayCommand(_ => RemoveSelectedFile(), _ => !string.IsNullOrEmpty(FilePath));

    }

    public ObservableCollection<SrsOption> AvailableSrsOptions { get; }

    public string FilePath
    {
        get => _filePath;
        set { _filePath = value ?? string.Empty; RaisePropertyChanged(); }
    }

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

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<PointDisplay> SamplePoints
    {
        get => _samplePoints;
        set
        {
            _samplePoints = value ?? new ObservableCollection<PointDisplay>();
            RaisePropertyChanged();
        }
    }

    public RelayCommand BrowseCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand CancelCommand { get; }

    public RelayCommand RemoveFileCommand { get; }

    private void ApplyInitialSrid(int srid)
    {
        if (srid >= 32601 && srid <= 32660)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = srid - 32600;
            IsNorthHemisphere = true;
        }
        else if (srid >= 32701 && srid <= 32760)
        {
            SelectedSrsOption = _utmOption;
            UtmZone = srid - 32700;
            IsNorthHemisphere = false;
        }
        else
        {
            var match = AvailableSrsOptions.FirstOrDefault(o => !o.IsUtm && o.FixedSrid == srid);
            if (match != null)
                SelectedSrsOption = match;
        }
    }

    private async Task BrowseAsync()
    {
        var path = _dialogService.ShowOpenFileDialog("Drawing Exchange Format (DXF)|*.dxf", null);
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
                ApplyInitialSrid(preview.DetectedSrid);
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
}

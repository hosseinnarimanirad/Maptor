using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Wpf.Services;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Wpf.Models.DxfOpenDialog;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Dialogs;

public class CsvTsvOpenDialogViewModel : DialogViewModelBase
{
    //private readonly IDialogService _dialogService;
    private readonly SrsOption _utmOption;
    private string _filePath = string.Empty;
    private string _rawText = string.Empty;
    private SrsOption? _selectedSrsOption;
    private int _utmZone = 39;
    private bool _utmHemisphereNorth = true;
    private bool _isCsv = true;
    private bool _isLongitudeFirst = true;
    private bool _useFirstLineAsHeader = false;

    public CsvTsvOpenDialogViewModel(IDialogService dialogService, bool initialIsCsv = true, int? initialSrid = null)
    {
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _isCsv = initialIsCsv;

        _utmOption = new SrsOption { DisplayName = "UTM (user-defined zone)", IsUtm = true };

        AvailableSrsOptions = new ObservableCollection<SrsOption>
        {
            new() { FixedSrid = SridHelper.GeodeticWGS84, DisplayName = "WGS84 (EPSG:4326)", IsUtm = false },
            _utmOption,
            new() { FixedSrid = SridHelper.WebMercator, DisplayName = "Web Mercator (EPSG:3857)", IsUtm = false }
        };

        SelectedSrsOption = AvailableSrsOptions[0];

        //this.GeometryModes = [
        //    new(GeometryType.Point, "Point"),
        //    new(GeometryType.LineString,"Polyline"),
        //    new(GeometryType.Polygon,"Polygon")
        //    ];

        this.SelectedGeometryMode = GeometryType.Polygon; //GeometryModes[0];

        if (initialSrid.HasValue && initialSrid.Value > 0)
            ApplyInitialSrid(initialSrid.Value);

        BrowseCommand = new RelayCommand(_ => Browse());
        OpenCommand = new RelayCommand(_ => Open(), _ => CanOpen());
        CancelCommand = new RelayCommand(_ => Cancel());
        RemoveFileCommand = new RelayCommand(_ => RemoveSelectedFile(), _ => !string.IsNullOrEmpty(FilePath));
        //SelectGeometryModeCommand = new RelayCommand(type => SelectedGeometryMode = (GeometryType)type);
    }

    public ObservableCollection<SrsOption> AvailableSrsOptions { get; }

    //public List<GeometryModeModel> GeometryModes { get; }

    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value ?? string.Empty;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsFileSelected));
            RaisePropertyChanged(nameof(IsRawTextReadOnly));
        }
    }

    /// <summary>
    /// Raw text content: either pasted by user or loaded from file. Used for preview (top 50 lines) and import.
    /// </summary>
    public string RawText
    {
        get => _rawText;
        set
        {
            _rawText = value ?? string.Empty;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ValidationMessage));
        }
    }

    public bool IsFileSelected => !string.IsNullOrWhiteSpace(FilePath);

    public bool IsRawTextReadOnly => IsFileSelected;

    public SrsOption? SelectedSrsOption
    {
        get => _selectedSrsOption;
        set
        {
            _selectedSrsOption = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsUtmSelected));
            RaisePropertyChanged(nameof(EffectiveSelectedSrid));
            RaisePropertyChanged(nameof(ValidationMessage));
        }
    }

    private GeometryType _selectedGeometryMode;
    public GeometryType SelectedGeometryMode
    {
        get => _selectedGeometryMode;
        set
        {
            _selectedGeometryMode = value;
            RaisePropertyChanged();
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
            RaisePropertyChanged(nameof(ValidationMessage));
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
            RaisePropertyChanged(nameof(ValidationMessage));
        }
    }

    public bool IsCsv
    {
        get => _isCsv;
        set { _isCsv = value; RaisePropertyChanged(); }
    }

    /// <summary>
    /// True = X,Y (longitude,latitude) order; False = Y,X.
    /// </summary>
    public bool IsLongitudeFirst
    {
        get => _isLongitudeFirst;
        set { _isLongitudeFirst = value; RaisePropertyChanged(); }
    }

    public bool UseFirstLineAsHeader
    {
        get => _useFirstLineAsHeader;
        set { _useFirstLineAsHeader = value; RaisePropertyChanged(); }
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

    public RelayCommand BrowseCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand CancelCommand { get; }

    public RelayCommand RemoveFileCommand { get; }
    //public ICommand SelectGeometryModeCommand { get; }

    /// <summary>
    /// Set when user clicks Open. Contains the full result for import.
    /// </summary>
    public CsvTsvOptions? CsvTsvResult { get; private set; }

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

    private async Task Browse()
    {
        //var filter = IsCsv
        //    ? "CSV file (*.csv)|*.csv|Text file (*.txt)|*.txt"
        //    : "TSV file (*.tsv)|*.tsv|Text file (*.txt)|*.txt";
        var filter = IsCsv ? DataSourceKind.Csv : DataSourceKind.Tsv;

        var path = await DialogService.ShowOpenFileDialogAsync(filter, null);

        if (string.IsNullOrEmpty(path))
            return;

        FilePath = path;

        try
        {
            var content = File.ReadAllText(path);
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var previewLines = lines.Take(50);
            RawText = string.Join(Environment.NewLine, previewLines);
            if (lines.Length > 50)
                RawText += Environment.NewLine + $"... ({lines.Length - 50} more lines)";
        }
        catch
        {
            RawText = string.Empty;
        }
    }

    private bool CanOpen()
    {
        return ValidationMessage is null;
    }

    /// <summary>
    /// Why Open is disabled, or null when it is enabled. Shown in the dialog footer so the
    /// user is not left guessing at a greyed-out button.
    /// </summary>
    public string? ValidationMessage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RawText))
                return LocalizationManager.Instance["dialog_common_validationNoData"];

            if (EffectiveSelectedSrid <= 0)
                return LocalizationManager.Instance["dialog_common_validationNoSrs"];

            if (IsUtmSelected && (UtmZone < 1 || UtmZone > 60))
                return LocalizationManager.Instance["dialog_common_validationUtmZone"];

            return null;
        }
    }

    private void RemoveSelectedFile()
    {
        this.FilePath = string.Empty;
        this.RawText = string.Empty;
    }

    private void Open()
    {
        if (!CanOpen())
            return;

        var textToImport = RawText;

        if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
        {
            try
            {
                textToImport = File.ReadAllText(FilePath);
            }
            catch
            {
                var idx = RawText.IndexOf("... (", StringComparison.Ordinal);
                textToImport = idx >= 0 ? RawText[..idx].TrimEnd() : RawText;
            }
        }

        CsvTsvResult = new CsvTsvOptions(
            IsFileSelected,
            string.IsNullOrEmpty(FilePath) ? null : FilePath,
            textToImport,
            EffectiveSelectedSrid,
            IsCsv,
            IsLongitudeFirst,
            UseFirstLineAsHeader,
            SelectedGeometryMode);

        DialogResult = true;

        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        DialogResult = null;
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
}

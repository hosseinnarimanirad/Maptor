using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using IRI.Maptor.Jab.Common.Models.Print;
using IRI.Maptor.Jab.Common.Services;
using IRI.Maptor.Sta.Pdf;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

public class PrintToPdfDialogViewModel : DialogViewModelBase
{
    private readonly Func<Task<BitmapSource?>>? _mapThumbnailProvider;

    private string _mapTitle = string.Empty;
    public string MapTitle
    {
        get => _mapTitle;
        set { _mapTitle = value ?? string.Empty; RaisePropertyChanged(); }
    }

    private bool _includeDecorations = true;
    public bool IncludeDecorations
    {
        get => _includeDecorations;
        set { _includeDecorations = value; RaisePropertyChanged(); }
    }

    private bool _showScaleBar = true;
    public bool ShowScaleBar
    {
        get => _showScaleBar;
        set { _showScaleBar = value; RaisePropertyChanged(); }
    }

    private bool _showMaptorLogo = true;
    public bool ShowMaptorLogo
    {
        get => _showMaptorLogo;
        set { _showMaptorLogo = value; RaisePropertyChanged(); }
    }

    private bool _showCompanyLogo;
    public bool ShowCompanyLogo
    {
        get => _showCompanyLogo;
        set { _showCompanyLogo = value; RaisePropertyChanged(); }
    }

    private string _companyLogoPath = string.Empty;
    public string CompanyLogoPath
    {
        get => _companyLogoPath;
        set
        {
            _companyLogoPath = value ?? string.Empty;
            RaisePropertyChanged();
            ShowCompanyLogo = !string.IsNullOrEmpty(_companyLogoPath);
        }
    }

    private bool _showGraticule = true;
    public bool ShowGraticule
    {
        get => _showGraticule;
        set { _showGraticule = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<PdfPageSize> AvailablePageSizes { get; } = new() { PdfPageSize.A4, PdfPageSize.A3, PdfPageSize.Letter };

    private PdfPageSize _selectedPageSize = PdfPageSize.A4;
    public PdfPageSize SelectedPageSize
    {
        get => _selectedPageSize;
        set
        {
            _selectedPageSize = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PreviewPageHeight));
        }
    }

    private bool _isLandscape = true;
    public bool IsLandscape
    {
        get => _isLandscape;
        set
        {
            _isLandscape = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PreviewPageHeight));
        }
    }

    private BitmapSource? _mapThumbnail;
    public BitmapSource? MapThumbnail
    {
        get => _mapThumbnail;
        set { _mapThumbnail = value; RaisePropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; RaisePropertyChanged(); }
    }

    public const double PreviewPageWidth = 240;

    public double PreviewPageHeight
    {
        get
        {
            var (w, h) = SelectedPageSize switch
            {
                PdfPageSize.A3 => (842.0, 1191.0),
                PdfPageSize.Letter => (612.0, 792.0),
                _ => (595.0, 842.0),
            };

            var aspect = IsLandscape ? h / w : w / h;

            return PreviewPageWidth / aspect;
        }
    }

    public PrintToPdfDialogOptions? Result { get; private set; }

    public PrintToPdfDialogViewModel(
        IDialogService dialogService,
        Func<Task<BitmapSource?>>? mapThumbnailProvider = null,
        PrintToPdfDialogOptions? initialOptions = null)
    {
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _mapThumbnailProvider = mapThumbnailProvider;

        if (initialOptions != null)
        {
            MapTitle = initialOptions.MapTitle ?? string.Empty;
            IncludeDecorations = initialOptions.IncludeDecorations;
            ShowScaleBar = initialOptions.ShowScaleBar;
            ShowMaptorLogo = initialOptions.ShowMaptorLogo;
            CompanyLogoPath = initialOptions.CompanyLogoPath ?? string.Empty;
            ShowCompanyLogo = initialOptions.ShowCompanyLogo && !string.IsNullOrEmpty(CompanyLogoPath);
            ShowGraticule = initialOptions.ShowGraticule;
            SelectedPageSize = initialOptions.PageSize;
            IsLandscape = initialOptions.PageOrientation == PdfPageOrientation.Landscape;
        }

        BrowseLogoCommand = new RelayCommand(async _ => await BrowseLogoAsync());
        RemoveLogoCommand = new RelayCommand(_ => CompanyLogoPath = string.Empty, _ => !string.IsNullOrEmpty(CompanyLogoPath));
        PrintCommand = new RelayCommand(_ => Print());
        CancelCommand = new RelayCommand(_ => Cancel());

        _ = LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        if (_mapThumbnailProvider == null)
            return;

        IsLoading = true;

        try
        {
            MapThumbnail = await _mapThumbnailProvider();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PrintToPdfDialogViewModel thumbnail error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BrowseLogoAsync()
    {
        var path = await DialogService.ShowOpenFileDialogAsync("*.png|*.png", null);

        if (!string.IsNullOrEmpty(path))
            CompanyLogoPath = path;
    }

    private void Print()
    {
        Result = new PrintToPdfDialogOptions
        {
            MapTitle = string.IsNullOrWhiteSpace(MapTitle) ? null : MapTitle.Trim(),
            IncludeDecorations = IncludeDecorations,
            ShowScaleBar = ShowScaleBar,
            ShowMaptorLogo = ShowMaptorLogo,
            ShowCompanyLogo = ShowCompanyLogo && !string.IsNullOrEmpty(CompanyLogoPath),
            CompanyLogoPath = string.IsNullOrEmpty(CompanyLogoPath) ? null : CompanyLogoPath,
            ShowGraticule = ShowGraticule,
            PageSize = SelectedPageSize,
            PageOrientation = IsLandscape ? PdfPageOrientation.Landscape : PdfPageOrientation.Portrait,
        };

        DialogResult = true;
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        Result = null;
        DialogResult = false;
        RequestClose?.Invoke();
    }

    #region Commands

    public RelayCommand BrowseLogoCommand { get; }

    public RelayCommand RemoveLogoCommand { get; }

    public RelayCommand PrintCommand { get; }

    public RelayCommand CancelCommand { get; }

    #endregion
}
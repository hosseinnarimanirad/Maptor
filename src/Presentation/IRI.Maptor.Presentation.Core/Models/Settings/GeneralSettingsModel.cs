using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Core.Models;

public class GeneralSettingsModel : Notifier, IGeneralSettings
{
    protected readonly IGeneralSettings _settings;
     
    public LanguageType CurrentLanguage
    {
        get => _settings.CurrentLanguage;
        set
        {
            _settings.CurrentLanguage = value;
            RaisePropertyChanged();
        }
    }


    public List<LanguageType> AvailableLanguages
    {
        get => _settings.AvailableLanguages;
        set
        {
            _settings.AvailableLanguages = value;
            RaisePropertyChanged();
        }
    }


    public double LegendFontSize
    {
        get => _settings.LegendFontSize;
        set
        {
            _settings.LegendFontSize = value;
            RaisePropertyChanged();
        }
    }

    public bool Scalebar_ShowScalebar
    {
        get => _settings.Scalebar_ShowScalebar;
        set
        {
            _settings.Scalebar_ShowScalebar = value;
            RaisePropertyChanged();
        }
    }

    public bool Scalebar_ShowScaleValue
    {
        get => _settings.Scalebar_ShowScaleValue;
        set
        {
            _settings.Scalebar_ShowScaleValue = value;
            RaisePropertyChanged();
        }
    }

    public bool Scalebar_ShowZoomLevel
    {
        get => _settings.Scalebar_ShowZoomLevel;
        set
        {
            _settings.Scalebar_ShowZoomLevel = value;
            RaisePropertyChanged();
        }
    }

    public bool CoordinatePanel_ShowCoordinatePanel
    {
        get => _settings.CoordinatePanel_ShowCoordinatePanel;
        set
        {
            _settings.CoordinatePanel_ShowCoordinatePanel = value;
            RaisePropertyChanged();
        }
    }

    public bool CoordinatePanel_ShowMgrs
    {
        get => _settings.CoordinatePanel_ShowMgrs;
        set
        {
            _settings.CoordinatePanel_ShowMgrs = value;
            RaisePropertyChanged();
        }
    }

    public string MapGrids_SelectedKeys
    {
        get => _settings.MapGrids_SelectedKeys;
        set
        {
            _settings.MapGrids_SelectedKeys = value;
            RaisePropertyChanged();
        }
    }

    public bool Legend_ShowLegendTools
    {
        get => _settings.Legend_ShowLegendTools;
        set
        {
            _settings.Legend_ShowLegendTools = value;
            RaisePropertyChanged();
        }
    }

    public bool Legend_ShowLayerColors
    {
        get => _settings.Legend_ShowLayerColors;
        set
        {
            _settings.Legend_ShowLayerColors = value;
            RaisePropertyChanged();
        }
    }

    public bool FeatureTable_UseMultiRowTabs
    {
        get => _settings.FeatureTable_UseMultiRowTabs;
        set
        {
            _settings.FeatureTable_UseMultiRowTabs = value;
            RaisePropertyChanged();
        }
    }
     

    public MahAppsThemeColor? MahAppsTheme
    {
        get => _settings.MahAppsTheme;
        set
        {
            _settings.MahAppsTheme = value;
            RaisePropertyChanged();
        }
    }

    public ThemeMode? MahAppsThemeMode
    {
        get => _settings.MahAppsThemeMode;
        set
        {
            _settings.MahAppsThemeMode = value;
            RaisePropertyChanged();
        }
    }


    public GeneralSettingsModel(IGeneralSettings settings)
    {
        _settings = settings;
    }


    public IGeneralSettings GetData() => _settings;

}

using IRI.Maptor.Jab.Core.Data;
using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Core.Models;

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


    public GeneralSettingsModel(IGeneralSettings settings)
    {
        _settings = settings;
    }


    public IGeneralSettings GetData() => _settings;

}

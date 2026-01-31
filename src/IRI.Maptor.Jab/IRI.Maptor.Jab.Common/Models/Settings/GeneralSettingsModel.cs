using System;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Models.Settings;

public class GeneralSettingsModel : Notifier, IGeneralSettings
{
    protected readonly IGeneralSettings _settings;

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

    public string MahAppsTheme
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

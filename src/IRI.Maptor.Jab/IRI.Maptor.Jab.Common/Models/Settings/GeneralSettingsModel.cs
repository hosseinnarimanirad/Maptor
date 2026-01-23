using System;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Models.Settings;

public class GeneralSettingsModel : Notifier, IGeneralSettings
{
    private readonly IGeneralSettings _settings;

    public double LegendFontSize
    {
        get => _settings.LegendFontSize;
        set
        {
            _settings.LegendFontSize = value;
            RaisePropertyChanged();
        }
    }

    public GeneralSettingsModel(IGeneralSettings settings)
    {
        _settings = settings; 
    }
}

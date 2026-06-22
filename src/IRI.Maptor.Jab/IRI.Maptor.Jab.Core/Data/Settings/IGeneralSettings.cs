using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Core.Models;
using System.Collections.Generic;

namespace IRI.Maptor.Jab.Core.Data;

public interface IGeneralSettings
{
    //string? SelectedCultureName { get; set; }
    LanguageType CurrentLanguage { get; set; }

    List<LanguageType> AvailableLanguages { get; set; }

    double LegendFontSize { get; set; }


    bool Scalebar_ShowScalebar { get; set; }

    bool Scalebar_ShowScaleValue { get; set; }

    bool Scalebar_ShowZoomLevel { get; set; }

    bool CoordinatePanel_ShowCoordinatePanel { get; set; }

    bool Legend_ShowLegendTools { get; set; }

    bool Legend_ShowLayerColors { get; set; }

    bool FeatureTable_UseMultiRowTabs { get; set; } 

    MahAppsThemeColor? MahAppsTheme { get; set; }

}

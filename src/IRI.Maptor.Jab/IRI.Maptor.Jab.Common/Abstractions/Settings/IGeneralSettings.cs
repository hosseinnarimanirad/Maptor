using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common.Models.Themes;
using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Abstractions;

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

    MahAppsThemeColor? MahAppsTheme { get; set; }

}

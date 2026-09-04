using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Models;
using System.Collections.Generic;

namespace IRI.Maptor.Presentation.Core.Data;

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

    /// <summary>
    /// Whether the coordinate panel shows the MGRS reference beside the coordinates. Off by
    /// default: MGRS is a military grid, not something most maps need on screen.
    /// </summary>
    bool CoordinatePanel_ShowMgrs { get; set; }

    /// <summary>
    /// Which map grids were switched on, as their keys joined by commas — <c>geodetic,utm</c>.
    /// Empty means none, which is the default.
    /// </summary>
    /// <remarks>
    /// Keys rather than indices, so a grid added to or removed from the catalogue later does not
    /// silently restore a different one. A key that no longer exists is ignored on restore.
    /// </remarks>
    string MapGrids_SelectedKeys { get; set; }

    bool Legend_ShowLegendTools { get; set; }

    bool Legend_ShowLayerColors { get; set; }

    bool FeatureTable_UseMultiRowTabs { get; set; } 

    MahAppsThemeColor? MahAppsTheme { get; set; }

    ThemeMode? MahAppsThemeMode { get; set; }

}

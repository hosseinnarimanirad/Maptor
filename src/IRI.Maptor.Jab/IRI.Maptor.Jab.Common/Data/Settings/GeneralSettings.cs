using System;
using System.Collections.Generic;
using IRI.Maptor.Jab.Common.Data.Settings;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common.Models.Themes;

namespace IRI.Maptor.Jab.Common.Data;

public class GeneralSettings : IGeneralSettings
{
    //public string? SelectedCultureName { get; set; } = "fa-IR";
    public LanguageType CurrentLanguage { get; set; } = LanguageType.fa_IR;

    public List<LanguageType> AvailableLanguages { get; set; } = [LanguageType.en_US, LanguageType.fa_IR];

    public double LegendFontSize { get; set; } = 10;

    public bool Scalebar_ShowScalebar { get; set; } = true;

    public bool Scalebar_ShowScaleValue { get; set; } = true;

    public bool Scalebar_ShowZoomLevel { get; set; } = true;

    public bool CoordinatePanel_ShowCoordinatePanel { get; set; } = true;

    public bool Legend_ShowLegendTools { get; set; } = false;

    public bool Legend_ShowLayerColors { get; set; } = false;

    public MahAppsThemeColor? MahAppsTheme { get; set; } = MahAppsThemeColor.Amber;

    public static GeneralSettings Default => new GeneralSettings();
}

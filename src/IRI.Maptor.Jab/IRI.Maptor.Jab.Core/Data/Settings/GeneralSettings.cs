
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Core.Data;

public class GeneralSettings : IGeneralSettings
{
    public LanguageType CurrentLanguage { get; set; } = LanguageType.fa_IR;

    public List<LanguageType> AvailableLanguages { get; set; } = [LanguageType.en_US, LanguageType.fa_IR];

    public double LegendFontSize { get; set; } = 10;

    public bool Scalebar_ShowScalebar { get; set; } = true;

    public bool Scalebar_ShowScaleValue { get; set; } = true;

    public bool Scalebar_ShowZoomLevel { get; set; } = true;

    public bool CoordinatePanel_ShowCoordinatePanel { get; set; } = true;

    public bool Legend_ShowLegendTools { get; set; } = false;

    public bool Legend_ShowLayerColors { get; set; } = false;

    public bool FeatureTable_UseMultiRowTabs { get; set; } = true;

    public MahAppsThemeColor? MahAppsTheme { get; set; } = MahAppsThemeColor.Amber;

    public static GeneralSettings Default => new GeneralSettings();
}
